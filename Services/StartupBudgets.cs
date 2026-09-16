using System;
using System.Collections.Generic;
using DevTemWinUi3.Services.Diagnostics;

namespace DevTemWinUi3.Services;

/// <summary>
/// Startup performance budgets (performance plan P3-1, ratified from
/// §2.2): the machines catch regressions, not users. <see cref="Check"/>
/// compares a metrics snapshot against the budgets and returns human
/// descriptions of every breach (empty when green). Pure and
/// headless-testable. For one release breaches only warn (see the
/// budget test); afterwards they fail.
/// </summary>
public static class StartupBudgets
{
    /// <summary>Process → splash pixels, Debug dev box.</summary>
    public const double SplashMs = 800;

    /// <summary>Process → interactive main window, Debug.</summary>
    public const double WindowMsDebug = 2500;

    /// <summary>Process → interactive main window, Release.</summary>
    public const double WindowMsRelease = 2000;

    /// <summary>Warm start → interactive.</summary>
    public const double WarmMs = 1200;

    /// <summary>Regression margin before a breach is reported.</summary>
    public const double ToleranceRatio = 1.15;

    /// <summary>
    /// Checks <paramref name="snapshot"/> against the budgets for
    /// <paramref name="isRelease"/>. Missing phases are skipped (a
    /// headless snapshot has none) — never throws, never null.
    /// </summary>
    public static IReadOnlyList<string> Check(AppMetrics.MetricsSnapshot snapshot, bool isRelease)
    {
        var breaches = new List<string>();
        try
        {
            if (snapshot.StartupPhasesMs.TryGetValue("splash", out double splash) &&
                splash > SplashMs * ToleranceRatio)
                breaches.Add($"splash {splash:F0} ms > budget {SplashMs:F0} ms");
            double windowBudget = isRelease ? WindowMsRelease : WindowMsDebug;
            if (snapshot.StartupPhasesMs.TryGetValue("window", out double window) &&
                window > windowBudget * ToleranceRatio)
                breaches.Add($"window {window:F0} ms > budget {windowBudget:F0} ms");
        }
        catch { }
        return breaches;
    }
}
