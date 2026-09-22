using System;
using System.Threading;
using System.Threading.Tasks;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

/// <summary>
/// Advancement C5: registry mechanics, single-flight, per-task
/// cancellation, offline gate, and the update-check default. Fake tasks
/// only — the real update check stays covered by
/// <c>BackgroundUpdateServiceTests</c> (it short-circuits unpackaged).
/// </summary>
[TestClass]
public class BackgroundTaskRunnerTests
{
    private sealed class FakeTask : IBackgroundTask
    {
        public string Name { get; set; } = "fake";
        public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(30);
        public bool RequiresNetwork { get; set; }
        public int Runs;
        public TaskCompletionSource? Gate;
        public CancellationToken SeenToken;

        public async Task RunAsync(BackgroundTaskContext context, CancellationToken cancellationToken)
        {
            Runs++;
            SeenToken = cancellationToken;
            if (Gate is not null)
                await Gate.Task;
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    [TestInitialize]
    public void Init() => BackgroundTaskRunner.ResetForTests();

    [TestCleanup]
    public void Cleanup() => BackgroundTaskRunner.ResetForTests();

    [TestMethod]
    public async Task RunDue_ExecutesOnceThenWaitsForInterval()
    {
        var task = new FakeTask();
        BackgroundTaskRunner.Register(task);
        var ctx = new BackgroundTaskContext(null);

        Assert.IsTrue(await BackgroundTaskRunner.RunTaskAsync("fake", ctx, CancellationToken.None));
        Assert.AreEqual(1, task.Runs);

        // Not due yet: direct run still works (explicit), due-sweep skips.
        await BackgroundTaskRunner.RunDueAsync(ctx, CancellationToken.None);
        Assert.AreEqual(1, task.Runs);
    }

    [TestMethod]
    public async Task RunDue_UnknownTask_ReturnsFalse()
    {
        Assert.IsFalse(await BackgroundTaskRunner.RunTaskAsync(
            "nope", new BackgroundTaskContext(null), CancellationToken.None));
    }

    [TestMethod]
    public async Task SingleFlight_OverlappingRun_Skipped()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var task = new FakeTask { Gate = gate };
        BackgroundTaskRunner.Register(task);
        var ctx = new BackgroundTaskContext(null);

        var first = BackgroundTaskRunner.RunTaskAsync("fake", ctx, CancellationToken.None);
        await Task.Delay(50);
        Assert.IsFalse(await BackgroundTaskRunner.RunTaskAsync("fake", ctx, CancellationToken.None));
        gate.SetResult();
        Assert.IsTrue(await first);
        Assert.AreEqual(1, task.Runs);
    }

    [TestMethod]
    public async Task CancelTask_CancelsInflightRun()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var task = new FakeTask { Gate = gate };
        BackgroundTaskRunner.Register(task);
        var ctx = new BackgroundTaskContext(null);

        var run = BackgroundTaskRunner.RunTaskAsync("fake", ctx, CancellationToken.None);
        await Task.Delay(50);
        BackgroundTaskRunner.CancelTask("fake");
        gate.SetResult();
        Assert.IsFalse(await run);
        Assert.IsTrue(task.SeenToken.IsCancellationRequested);
    }

    [TestMethod]
    public async Task OfflineGate_SkipsNetworkTasks()
    {
        var task = new FakeTask { RequiresNetwork = true };
        BackgroundTaskRunner.Register(task);
        var ctx = new BackgroundTaskContext(null);
        BackgroundTaskRunner.NetworkProbeOverride = () => false;
        try
        {
            Assert.IsFalse(await BackgroundTaskRunner.RunTaskAsync("fake", ctx, CancellationToken.None));
            Assert.AreEqual(0, task.Runs);
        }
        finally
        {
            BackgroundTaskRunner.NetworkProbeOverride = null;
        }
    }

    [TestMethod]
    public async Task OnlineGate_RunsNetworkTasks()
    {
        var task = new FakeTask { RequiresNetwork = true };
        BackgroundTaskRunner.Register(task);
        BackgroundTaskRunner.NetworkProbeOverride = () => true;
        try
        {
            Assert.IsTrue(await BackgroundTaskRunner.RunTaskAsync(
                "fake", new BackgroundTaskContext(null), CancellationToken.None));
            Assert.AreEqual(1, task.Runs);
        }
        finally
        {
            BackgroundTaskRunner.NetworkProbeOverride = null;
        }
    }

    [TestMethod]
    public void Unregister_RemovesTask()
    {
        BackgroundTaskRunner.Register(new FakeTask { Name = "gone" });
        Assert.IsTrue(BackgroundTaskRunner.IsRegistered("gone"));
        BackgroundTaskRunner.Unregister("gone");
        Assert.IsFalse(BackgroundTaskRunner.IsRegistered("gone"));
    }

    [TestMethod]
    public void EnsureDefaults_RegistersUpdateCheck()
    {
        BackgroundTaskRunner.EnsureDefaultsRegistered();

        Assert.IsTrue(BackgroundTaskRunner.IsRegistered("update-check"));
        CollectionAssert.Contains(BackgroundTaskRunner.RegisteredNames(), "update-check");
    }
}
