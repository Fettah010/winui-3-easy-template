using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;

namespace DevTemWinUi3.Services.Diagnostics;

/// <summary>
/// In-process app metrics (three-pillars instrumentation, desktop-sized).
/// A singleton <see cref="Meter"/> records navigations, update-check
/// timings, startup phases, and exceptions (OTLP-ready for whoever wires an
/// exporter), while a small last-value store feeds the diagnostics page.
/// Everything is never-throw and safe to call headless. Never excluded by
/// template feature flags: reference freely from any file.
/// </summary>
public static class AppMetrics
{
    private static readonly Meter s_meter = new("DevTem.Diagnostics");
    private static readonly Counter<long> s_navigations =
        s_meter.CreateCounter<long>("diagnostics.navigations");
    private static readonly Counter<long> s_exceptions =
        s_meter.CreateCounter<long>("diagnostics.exceptions");
    private static readonly Histogram<double> s_updateCheckMs =
        s_meter.CreateHistogram<double>("diagnostics.update_check_ms");
    private static readonly Histogram<double> s_startupPhaseMs =
        s_meter.CreateHistogram<double>("diagnostics.startup_phase_ms");
    private static readonly Histogram<double> s_navigationMs =
        s_meter.CreateHistogram<double>("diagnostics.navigation_ms");

    private static readonly ConcurrentDictionary<string, double> s_startupPhases = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, long> s_navigationCounts = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, double> s_navigationTimings = new(StringComparer.Ordinal);
    private static long s_updateCheckCount;
    private static long s_exceptionCount;
    private static readonly object s_updateLock = new();
    private static double s_lastUpdateCheckMs;
    private static bool s_hasUpdateCheck;

    /// <summary>Point-in-time metrics for the diagnostics page.</summary>
    public sealed record MetricsSnapshot(
        IReadOnlyDictionary<string, double> StartupPhasesMs,
        IReadOnlyDictionary<string, long> NavigationCounts,
        IReadOnlyDictionary<string, double> NavigationTimingsMs,
        long UpdateCheckCount,
        double? LastUpdateCheckMs,
        long ExceptionCount);

    public static void RecordStartupPhase(string phase, double elapsedMs)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(phase))
                return;
            s_startupPhases[phase] = elapsedMs;
            s_startupPhaseMs.Record(elapsedMs, new TagList { { "phase", phase } });
        }
        catch { }
    }

    public static void RecordNavigation(string tag)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tag))
                return;
            s_navigationCounts.AddOrUpdate(tag, 1, (_, n) => n + 1);
            s_navigations.Add(1, new TagList { { "page", tag } });
        }
        catch { }
    }

    /// <summary>
    /// Records a navigation with its elapsed time (P2-3 page-construction
    /// budget): counts like <see cref="RecordNavigation(string)"/> plus a
    /// last-value timing per page for the diagnostics page (nav p95
    /// narrative) and a histogram for exporters.
    /// </summary>
    public static void RecordNavigation(string tag, double elapsedMs)
    {
        try
        {
            RecordNavigation(tag);
            if (string.IsNullOrWhiteSpace(tag) || double.IsNaN(elapsedMs) || double.IsInfinity(elapsedMs))
                return;
            s_navigationTimings[tag] = Math.Max(0, elapsedMs);
            s_navigationMs.Record(Math.Max(0, elapsedMs), new TagList { { "page", tag } });
        }
        catch { }
    }

    public static void RecordUpdateCheck(double elapsedMs)
    {
        try
        {
            Interlocked.Increment(ref s_updateCheckCount);
            lock (s_updateLock)
            {
                s_lastUpdateCheckMs = elapsedMs;
                s_hasUpdateCheck = true;
            }
            s_updateCheckMs.Record(elapsedMs);
        }
        catch { }
    }

    public static void RecordException()
    {
        try
        {
            Interlocked.Increment(ref s_exceptionCount);
            s_exceptions.Add(1);
        }
        catch { }
    }

    public static MetricsSnapshot GetSnapshot()
    {
        try
        {
            double? last = null;
            lock (s_updateLock)
            {
                if (s_hasUpdateCheck)
                    last = s_lastUpdateCheckMs;
            }
            return new MetricsSnapshot(
                new Dictionary<string, double>(s_startupPhases, StringComparer.Ordinal),
                new Dictionary<string, long>(s_navigationCounts, StringComparer.Ordinal),
                new Dictionary<string, double>(s_navigationTimings, StringComparer.Ordinal),
                Interlocked.Read(ref s_updateCheckCount),
                last,
                Interlocked.Read(ref s_exceptionCount));
        }
        catch
        {
            return new MetricsSnapshot(
                new Dictionary<string, double>(),
                new Dictionary<string, long>(),
                new Dictionary<string, double>(),
                0, null, 0);
        }
    }
}
