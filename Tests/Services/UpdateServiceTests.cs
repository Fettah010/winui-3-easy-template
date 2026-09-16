using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class UpdateServiceTests
{
    private string _storePath = string.Empty;

    [TestInitialize]
    public void Init()
    {
        _storePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        LocalSettingsStore.SetTestPath(_storePath);
    }

    [TestCleanup]
    public void Cleanup()
    {
        LocalSettingsStore.SetTestPath(null);
        try { File.Delete(_storePath); } catch { }
    }

    [TestMethod]
    public void PeriodicCheckInterval_IsSane()
    {
        // The trayed-app re-check cadence: positive, and bounded so a typo
        // can neither DDoS the feed nor silently disable the feature.
        Assert.IsTrue(UpdateService.PeriodicCheckInterval > TimeSpan.Zero);
        Assert.IsTrue(UpdateService.PeriodicCheckInterval >= TimeSpan.FromHours(1));
        Assert.IsTrue(UpdateService.PeriodicCheckInterval <= TimeSpan.FromHours(24));
    }

    [TestMethod]
    public void IsInstalled_IsFalse_ForUnpackagedTestRuns()
    {
        // Guards the startup + periodic paths: must never throw headless,
        // and test runs are never Velopack-installed.
        Assert.IsFalse(UpdateService.Current.IsInstalled);
    }

    [TestMethod]
    public void PendingRestartVersion_IsNull_ForUnpackagedTestRuns()
    {
        // Nothing can wait on disk without a Velopack install layout;
        // must never throw headless (startup reads this every launch).
        Assert.IsNull(UpdateService.Current.PendingRestartVersion);
    }

    [TestMethod]
    public async Task EnsureInitializedAsync_CompletesHeadless()
    {
        // P0-3: lazy async init never throws unpackaged and leaves the
        // safe defaults in place.
        await UpdateService.Current.EnsureInitializedAsync();
        Assert.IsFalse(UpdateService.Current.IsInstalled);
        Assert.IsNull(UpdateService.Current.PendingRestartVersion);
    }

    [TestMethod]
    public void SyncSurface_NeverBlocks_AUiThread()
    {
        // P0-3: every synchronous property returns without waiting, even
        // when called on a single-threaded (UI-like) sync context that a
        // blocking Wait() would deadlock.
        var previous = SynchronizationContext.Current;
        var ui = new SingleThreadedContext();
        SynchronizationContext.SetSynchronizationContext(ui);
        try
        {
            bool installed = false;
            string? version = null;
            bool pending = true;
            string? restart = "x";
            string? current = null;
            ui.Run(() =>
            {
                installed = UpdateService.Current.IsInstalled;
                version = UpdateService.Current.CurrentVersion;
                pending = UpdateService.Current.HasPendingUpdate;
                restart = UpdateService.Current.PendingRestartVersion;
                current = UpdateService.Current.CurrentVersion;
            });
            Assert.IsFalse(installed);
            Assert.IsFalse(pending);
            Assert.IsNull(restart);
            Assert.IsFalse(string.IsNullOrWhiteSpace(version));
            Assert.IsFalse(string.IsNullOrWhiteSpace(current));
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
            ui.Complete();
        }
    }

    /// <summary>Minimal single-threaded context standing in for the UI thread.</summary>
    private sealed class SingleThreadedContext : SynchronizationContext, IDisposable
    {
        private readonly System.Collections.Concurrent.BlockingCollection<(SendOrPostCallback, object?)> _queue = new();

        public override void Post(SendOrPostCallback d, object? state) =>
            _queue.Add((d, state));

        public void Run(Action body)
        {
            var done = new ManualResetEventSlim(false);
            Exception? error = null;
            _queue.Add(((_ => { try { body(); } catch (Exception ex) { error = ex; } finally { done.Set(); } }), null));
            while (!done.IsSet)
            {
                if (_queue.TryTake(out var work, 5000))
                    work.Item1(work.Item2);
            }
            if (error is not null)
                throw error;
        }

        public void Complete() => _queue.CompleteAdding();

        public void Dispose()
        {
            _queue.Dispose();
            GC.SuppressFinalize(this);
        }

        public override SynchronizationContext CreateCopy() => this;
    }
}
