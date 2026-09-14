using System;
using Sentry;

namespace DevTemWinUi3.Services;

/// <summary>
/// Sentry SDK owner behind <see cref="CrashReportingService"/>: created
/// only when the user opted in and a DSN is configured. Scaffolded only
/// with the crash feature; without it the service veneer is a no-op and
/// this file (plus the Sentry package) is absent. Never throws.
/// </summary>
internal sealed class SentryCrashReporter : ICrashReporter
{
    internal static SentryCrashReporter? TryCreate(string dsn, string release, string environment)
    {
        try
        {
            var handle = SentrySdk.Init(o =>
            {
                o.Dsn = dsn;
                o.Release = release;
                o.Environment = environment;
                o.SendDefaultPii = false;
            });
            return new SentryCrashReporter(handle);
        }
        catch
        {
            return null;
        }
    }

    private readonly IDisposable _handle;

    private SentryCrashReporter(IDisposable handle)
    {
        _handle = handle;
    }

    public void Capture(Exception ex, string? context)
    {
        try
        {
            SentrySdk.CaptureException(ex, scope =>
            {
                if (!string.IsNullOrEmpty(context))
                    scope.SetTag("context", context);
                try { scope.Contexts["app-status"] = CrashReportingService.BuildStatusContext(); } catch { }
            });
        }
        catch { }
    }

    public void Breadcrumb(string message, string category)
    {
        try
        {
            SentrySdk.AddBreadcrumb(message, category);
        }
        catch { }
    }

    public void Dispose()
    {
        try
        {
            _handle.Dispose();
        }
        catch { }
    }
}
