using System.Collections.Generic;
using DevTemWinUi3.Services;
using DevTemWinUi3.Services.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class StartupBudgetsTests
{
    private static AppMetrics.MetricsSnapshot SnapshotWith(double splashMs, double windowMs) =>
        new(
            new Dictionary<string, double> { ["splash"] = splashMs, ["window"] = windowMs },
            new Dictionary<string, long>(),
            new Dictionary<string, double>(),
            0, null, 0);

    [TestMethod]
    public void Check_GreenSnapshot_NoBreaches()
    {
        var breaches = StartupBudgets.Check(SnapshotWith(400, 1500), isRelease: false);
        Assert.IsEmpty(breaches);
    }

    [TestMethod]
    public void Check_SlowWindow_ReportsBreach()
    {
        // P3-1: a window phase 2x the Debug budget breaches (15% tolerance).
        var breaches = StartupBudgets.Check(SnapshotWith(400, 6000), isRelease: false);
        Assert.HasCount(1, breaches);
        Assert.Contains("window", breaches[0]);
    }

    [TestMethod]
    public void Check_SlowSplash_ReportsBreach()
    {
        var breaches = StartupBudgets.Check(SnapshotWith(2000, 1500), isRelease: false);
        Assert.HasCount(1, breaches);
        Assert.Contains("splash", breaches[0]);
    }

    [TestMethod]
    public void Check_ReleaseBudget_IsTighter()
    {
        // 2600 ms passes Debug (2500 + 15%) but breaches Release (2000 + 15%).
        var debug = StartupBudgets.Check(SnapshotWith(400, 2600), isRelease: false);
        var release = StartupBudgets.Check(SnapshotWith(400, 2600), isRelease: true);
        Assert.IsEmpty(debug);
        Assert.IsNotEmpty(release);
    }

    [TestMethod]
    public void Check_MissingPhases_Skipped()
    {
        var empty = new AppMetrics.MetricsSnapshot(
            new Dictionary<string, double>(),
            new Dictionary<string, long>(),
            new Dictionary<string, double>(),
            0, null, 0);
        Assert.IsEmpty(StartupBudgets.Check(empty, isRelease: false));
    }

    [TestMethod]
    public void Check_LiveSnapshot_NeverThrows()
    {
        // P3-1 warn-first wiring: whatever the process recorded, checking
        // it is safe (empty headless snapshots simply have no phases).
        var breaches = StartupBudgets.Check(AppMetrics.GetSnapshot(), isRelease: false);
        Assert.IsNotNull(breaches);
    }

    [TestMethod]
    public void Budgets_AreSaneAndOrdered()
    {
        // Anti-mute guard (Phase D1): budgets live in code, so pinning
        // their shape stops a quiet raise-to-green. Mute = new baseline +
        // reason in DECISIONS, never a constant edit.
        Assert.IsGreaterThan(0, StartupBudgets.SplashMs);
        Assert.IsGreaterThan(0, StartupBudgets.WindowMsDebug);
        Assert.IsGreaterThan(0, StartupBudgets.WindowMsRelease);
        Assert.IsLessThanOrEqualTo(StartupBudgets.WindowMsDebug, StartupBudgets.WindowMsRelease);
        Assert.IsGreaterThan(1, StartupBudgets.ToleranceRatio);
    }

    [TestMethod]
    public void Report_ReturnsBreachesWithoutThrowing()
    {
        var breaches = StartupBudgets.Report(SnapshotWith(400, 6000), isRelease: false);
        Assert.HasCount(1, breaches);
        Assert.IsEmpty(StartupBudgets.Report(SnapshotWith(400, 1500), isRelease: false));
    }
}
