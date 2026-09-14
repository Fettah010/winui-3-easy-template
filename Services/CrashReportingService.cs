using System;
using System.Collections.Generic;
using DevTemWinUi3.Services.Diagnostics;
using Sentry;
using DevTemWinUi3.Services.Configuration;

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
    /// Whether reports are actually flowing (user opted in, DSN set, and
    /// init succeeded).
    /// </summary>
    public bool IsEnabled
    {
        get
        {
            lock (_lock)
            {
                if (_sentry is null)
                    return false;
                try { return SettingsService.Current.CrashReportsEnabled; }
                catch { return false; }
            }
        }
    }

    /// <summary>
    /// Starts Sentry when the user opted in and a DSN is configured.
    /// Idempotent and never throws. Call once at startup (Program.Run);
    /// call again after the opt-in toggle flips on.
    /// </summary>
    public void Initialize()
    {
        try
        {
            if (!SettingsService.Current.CrashReportsEnabled)
            {
                LoggingService.Log.Information("Crash reporting disabled: user opted out");
                return;
            }
        }
        catch
        {
            return;
        }
        var dsn = DeploymentConfiguration.SentryDsn;
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
                    o.Release = string.IsNullOrWhiteSpace(DeploymentConfiguration.SentryRelease)
                        ? AppInfo.Current.Version
                        : DeploymentConfiguration.SentryRelease;
                    o.Environment = string.IsNullOrWhiteSpace(DeploymentConfiguration.SentryEnvironment)
                        ? (AppInfo.Current.IsBetaBuild ? "beta" : "production")
                        : DeploymentConfiguration.SentryEnvironment;
                    o.SendDefaultPii = false;
                });
                LoggingService.Log.Information("Crash reporting enabled (Sentry, {Environment})",
                    string.IsNullOrWhiteSpace(DeploymentConfiguration.SentryEnvironment)
                        ? (AppInfo.Current.IsBetaBuild ? "beta" : "production")
                        : DeploymentConfiguration.SentryEnvironment);
            }
            catch (Exception ex)
            {
                _sentry = null;
                LoggingService.Log.Error(ex, "Crash reporting failed to initialize");
            }
        }
    }

    /// <summary>
    /// Reports an exception with an optional context tag, attaching the app
    /// status snapshot for actionability. Never throws; no-op (apart from a
    /// debug log) when disabled. Exceptions are counted in
    /// <see cref="AppMetrics"/> regardless of reporting state.
    /// </summary>
    public void CaptureException(Exception? ex, string? context = null)
    {
        if (ex is null)
            return;
        try { AppMetrics.RecordException(); } catch { }
        try
        {
            if (!IsEnabled)
                return;
            SentrySdk.CaptureException(ex, scope =>
            {
                if (!string.IsNullOrEmpty(context))
                    scope.SetTag("context", context);
                try { scope.Contexts["app-status"] = BuildStatusContext(); } catch { }
            });
        }
        catch (Exception captureEx)
        {
            LoggingService.Log.Error(captureEx, "Crash report capture failed");
        }
    }

    /// <summary>
    /// Leaves a breadcrumb at a key transition (navigation, update check,
    /// settings change, language switch). Static for terse call sites;
    /// never throws; no-op when disabled.
    /// </summary>
    public static void AddBreadcrumb(string message, string? category = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(message) || !Current.IsEnabled)
                return;
            SentrySdk.AddBreadcrumb(message, category ?? "app");
        }
        catch { }
    }

    /// <summary>
    /// App status attached to every report (version, channel, theme,
    /// language, log level). Pure and headless-testable.
    /// </summary>
    internal static Dictionary<string, string> BuildStatusContext()
    {
        var context = new Dictionary<string, string>(StringComparer.Ordinal);
        try
        {
            var status = DiagnosticsService.GetStatus();
            context["version"] = status.AppVersion;
            context["channel"] = status.Channel;
            context["theme"] = status.Theme;
            context["language"] = status.Language;
            context["logLevel"] = status.LogLevel;
        }
        catch { }
        return context;
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
