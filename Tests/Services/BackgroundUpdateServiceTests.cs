using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class BackgroundUpdateServiceTests
{
    private string _storePath = string.Empty;

    [TestInitialize]
    public void Init()
    {
        _storePath = Path.Combine(Path.GetTempPath(), System.Guid.NewGuid() + ".json");
        LocalSettingsStore.SetTestPath(_storePath);
    }

    [TestCleanup]
    public void Cleanup()
    {
        LocalSettingsStore.SetTestPath(null);
        try { File.Delete(_storePath); } catch { }
    }

    [TestMethod]
    public async Task CheckForUpdates_Unpackaged_ReturnsQuietly()
    {
        // Test runs are never installed: the guard must short-circuit
        // before touching the feed (no window needed either).
        await BackgroundUpdateService.Current.CheckForUpdatesAsync(null);
    }

    [TestMethod]
    public async Task PeriodicLoop_PreCancelledToken_ReturnsPromptly()
    {
        // P1-4: a cancelled loop is an expected shutdown, never an error.
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await BackgroundUpdateService.Current.RunPeriodicChecksAsync(
            null, System.TimeSpan.FromMilliseconds(20), cts.Token);
    }

    [TestMethod]
    public async Task PeriodicLoop_CancelMidRun_StopsLooping()
    {
        // P1-4: cancelling mid-run stops the timer (window-close path).
        using var cts = new CancellationTokenSource();
        var loop = BackgroundUpdateService.Current.RunPeriodicChecksAsync(
            null, System.TimeSpan.FromMilliseconds(20), cts.Token);
        await Task.Delay(120);
        cts.Cancel();
        await loop;
    }

    [TestMethod]
    public void IsMeteredConnection_DoesNotThrow()
    {
        // P1-4: environment-dependent, so only the never-throw contract
        // is pinned (true/false both acceptable headless).
        _ = BackgroundUpdateService.IsMeteredConnection();
    }

    [TestMethod]
    public async Task CheckForUpdates_AskModeOff_Unpackaged_ReturnsQuietly()
    {
        // Ask-mode (auto-install off) must also short-circuit quietly for
        // unpackaged runs: no dialog service is initialized headless, and
        // the guard runs before any popup path.
        bool previous = SettingsService.Current.AutoInstallUpdates;
        try
        {
            SettingsService.Current.AutoInstallUpdates = false;
            await BackgroundUpdateService.Current.CheckForUpdatesAsync(null);
        }
        finally
        {
            try { SettingsService.Current.AutoInstallUpdates = previous; } catch { }
        }
    }
}
