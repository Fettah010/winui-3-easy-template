using System;
using System.Diagnostics;

namespace DevTemWinUi3.Services.Diagnostics;

/// <summary>
/// Startup tracing: a reused singleton <see cref="ActivitySource"/> for the
/// launch path (splash → services → window). With no listener attached,
/// <c>StartActivity</c> returns null and every call is a near-zero-cost
/// no-op; attach a listener (tests, profilers, future OTLP export) to get
/// real spans. Timings are also pushed to <see cref="AppMetrics"/> so the
/// diagnostics page works with no listener at all. Never throws.
/// </summary>
public static class AppTrace
{
    public static readonly ActivitySource Source = new("DevTem.Startup");

    public static Activity? StartStartup() => StartPhase("startup");

    public static Activity? StartPhase(string phase)
    {
        try
        {
            var activity = Source.StartActivity(phase, ActivityKind.Internal);
            if (activity is null)
                return null;
            try { activity.SetTag("component", "startup"); } catch { }
            return activity;
        }
        catch
        {
            return null;
        }
    }
}
