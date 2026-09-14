using System;
using DevTemWinUi3.Services.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class AppMetricsTests
{
    private static string Unique(string prefix) =>
        prefix + "-" + Guid.NewGuid().ToString("N")[..8];

    [TestMethod]
    public void RecordStartupPhase_AppearsInSnapshot()
    {
        string phase = Unique("phase");
        AppMetrics.RecordStartupPhase(phase, 12.5);
        Assert.AreEqual(12.5, AppMetrics.GetSnapshot().StartupPhasesMs[phase]);
    }

    [TestMethod]
    public void RecordStartupPhase_BlankName_IsIgnored()
    {
        AppMetrics.RecordStartupPhase("  ", 1);
        Assert.IsFalse(AppMetrics.GetSnapshot().StartupPhasesMs.ContainsKey("  "));
    }

    [TestMethod]
    public void RecordNavigation_CountsPerTag()
    {
        string tag = Unique("page");
        AppMetrics.RecordNavigation(tag);
        AppMetrics.RecordNavigation(tag);
        Assert.AreEqual(2L, AppMetrics.GetSnapshot().NavigationCounts[tag]);
    }

    [TestMethod]
    public void RecordUpdateCheck_UpdatesCountAndLast()
    {
        long before = AppMetrics.GetSnapshot().UpdateCheckCount;
        AppMetrics.RecordUpdateCheck(42);
        var after = AppMetrics.GetSnapshot();
        Assert.AreEqual(before + 1, after.UpdateCheckCount);
        Assert.AreEqual(42.0, after.LastUpdateCheckMs);
    }

    [TestMethod]
    public void RecordException_IncrementsCount()
    {
        long before = AppMetrics.GetSnapshot().ExceptionCount;
        AppMetrics.RecordException();
        Assert.AreEqual(before + 1, AppMetrics.GetSnapshot().ExceptionCount);
    }
}
