using System;
using System.IO;
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
}
