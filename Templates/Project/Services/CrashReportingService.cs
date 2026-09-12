using System;
using Sentry;

namespace DevTemWinUi3.Services;

/// <summary>
/// Sentry crash reporting, gated by <see cref="AppMetadata.SentryDsn"/>.
/// Empty DSN (the default) disables it entirely: <see cref="Initialize"/>
/// returns without touching the network and every other entry point is a
/// safe no-op, so unconfigured apps and unit tests never notice it.
/// </summary>
public sealed class CrashReportingService
{
    public static CrashReportingService Current { get; } = new();

    private CrashReportingService() { }

    private readonly object _lock = new();
    private IDisposable? _sentry;

    /// <summary>
    /// Whether reports are actually flowing (DSN set and init succeeded).
    /// </summary>
    public bool IsEnabled
    {
        get { lock (_lock) { return _sentry is not null; } }
    }

    /// <summary>
    /// Starts Sentry when a DSN is configured. Idempotent and never throws.
    /// Call once at startup (Program.Run).
    /// </summary>
    public void Initialize()
    {
        var dsn = AppMetadata.SentryDsn;
        if (string.IsNullOrWhiteSpace(dsn))
        {
            LoggingService.Log.Information("Crash reporting disabled: no Sentry DSN configured");
            return;
        }

        lock (_lock)
        {
            if (_sentry is not null)
                return;
            try
            {
                _sentry = SentrySdk.Init(o =>
                {
                    o.Dsn = dsn;
                    o.Release = AppInfo.Current.Version;
                    o.Environment = AppInfo.Current.IsBetaBuild ? "beta" : "production";
                    o.SendDefaultPii = false;
                });
                LoggingService.Log.Information("Crash reporting enabled (Sentry, {Environment})",
                    AppInfo.Current.IsBetaBuild ? "beta" : "production");
            }
            catch (Exception ex)
            {
                _sentry = null;
                LoggingService.Log.Error(ex, "Crash reporting failed to initialize");
            }
        }
    }

    /// <summary>
    /// Reports an exception with an optional context tag. Never throws;
    /// no-op (apart from a debug log) when disabled.
    /// </summary>
    public void CaptureException(Exception? ex, string? context = null)
    {
        if (ex is null)
            return;
        try
        {
            if (!IsEnabled)
                return;
            SentrySdk.CaptureException(ex, scope =>
            {
                if (!string.IsNullOrEmpty(context))
                    scope.SetTag("context", context);
            });
        }
        catch (Exception captureEx)
        {
            LoggingService.Log.Error(captureEx, "Crash report capture failed");
        }
    }

    /// <summary>
    /// Flushes and shuts Sentry down. Never throws. Called on clean exit so
    /// queued reports go out; crash paths rely on best-effort delivery.
    /// </summary>
    public void Shutdown()
    {
        lock (_lock)
        {
            try
            {
                _sentry?.Dispose();
            }
            catch { }
            _sentry = null;
        }
    }
}
