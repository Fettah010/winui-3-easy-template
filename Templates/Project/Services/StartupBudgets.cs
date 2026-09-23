using System;
using System.Collections.Generic;
using DevTemWinUi3.Services.Diagnostics;

namespace DevTemWinUi3.Services;

/// <summary>
/// Startup performance budgets (performance plan P3-1, ratified from
/// §2.2): the machines catch regressions, not users. <see cref="Check"/>
/// compares a metrics snapshot against the budgets and returns human
/// descriptions of every breach (empty when green). Pure and
/// headless-testable. Armed (Phase D1): <see cref="Report"/> logs every
/// breach as an error at launch, and CI fails publish-weight jumps —
/// a breach gets triaged, never muted (mute = new baseline + reason in
/// DECISIONS).
/// </summary>
public static class StartupBudgets
{
    /// <summary>
    /// Process → splash pixels, Debug dev box. Re-baselined 800 → 1600 on
    /// the first measured cold flame (1451ms; warm ~300ms — cold is
    /// loader/JIT-dominated, not app code). Guards regressions, not physics.
    /// </summary>
    public const double SplashMs = 1600;

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

    /// <summary>
    /// Armed reporting (Phase D1): checks and logs every breach as an
    /// error (error-level = fails loudly in logs, Diagnostics, and
    /// bundles — the first breach gets triaged, not muted). Called once
    /// at launch after the window phase records. Never throws.
    /// </summary>
    public static IReadOnlyList<string> Report(AppMetrics.MetricsSnapshot snapshot, bool isRelease)
    {
        var breaches = Check(snapshot, isRelease);
        foreach (string breach in breaches)
        {
            try { AppLog.Error("Startup budget breach: {Breach}", breach); } catch { }
        }
        return breaches;
    }
}
