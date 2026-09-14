using System;
using System.Collections.Generic;
using DevTemWinUi3.Services.Configuration;
using DevTemWinUi3.Services.Diagnostics;

namespace DevTemWinUi3.Services;

/// <summary>
/// Crash-backend contract. Implemented by <see cref="SentryCrashReporter"/>
/// (scaffolded only with the crash feature); the veneer below programs
/// against this so call sites never touch SDK types.
/// </summary>
internal interface ICrashReporter : IDisposable
{
    void Capture(Exception ex, string? context);

    void Breadcrumb(string message, string category);
}

/// <summary>
/// Crash-reporting seam: SDK-free veneer, so every call site compiles for
/// every scaffold and degrades to a no-op when the crash feature is off.
/// The Sentry SDK lives in <see cref="SentryCrashReporter"/> (scaffolded
/// only with the crash feature). Gated by the opt-in setting plus
/// <see cref="AppMetadata.SentryDsn"/>: empty DSN (the default) disables
/// it entirely, so unconfigured apps and unit tests never notice it.
/// </summary>
public sealed class CrashReportingService
{
    public static CrashReportingService Current { get; } = new();
    private CrashReportingService() { }

    private readonly object _lock = new();
#pragma warning disable CA1859 // Seam by design: the interface decouples call sites from the (scaffold-optional) Sentry backend.
    private ICrashReporter? _reporter;
#pragma warning restore CA1859

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
                if (_reporter is null)
                    return false;
                try { return SettingsService.Current.CrashReportsEnabled; }
                catch { return false; }
            }
        }
    }

    /// <summary>
    /// Starts reporting when the user opted in and a DSN is configured.
    /// Idempotent and never throws. Call once at startup (Program.Run);
    /// call again after the opt-in toggle flips on.
    /// </summary>
    public void Initialize()
    {
        try
        {
            if (!SettingsService.Current.CrashReportsEnabled)
            {
                AppLog.Information("Crash reporting disabled: user opted out");
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
            AppLog.Information("Crash reporting disabled: no Sentry DSN configured");
            return;
        }

        lock (_lock)
        {
            if (_reporter is not null)
                return;
            try
            {
                string release = string.IsNullOrWhiteSpace(DeploymentConfiguration.SentryRelease)
                    ? AppInfo.Current.Version
                    : DeploymentConfiguration.SentryRelease;
                string environment = string.IsNullOrWhiteSpace(DeploymentConfiguration.SentryEnvironment)
                    ? (AppInfo.Current.IsBetaBuild ? "beta" : "production")
                    : DeploymentConfiguration.SentryEnvironment;
#if (crash)
                _reporter = SentryCrashReporter.TryCreate(dsn, release, environment);
#endif
                if (_reporter is null)
                    return;
                AppLog.Information("Crash reporting enabled (Sentry, {Environment})", environment);
            }
            catch (Exception ex)
            {
                _reporter = null;
                AppLog.Error(ex, "Crash reporting failed to initialize");
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
            _reporter?.Capture(ex, context);
        }
        catch (Exception captureEx)
        {
            AppLog.Error(captureEx, "Crash report capture failed");
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
            Current._reporter?.Breadcrumb(message, category ?? "app");
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
    /// Flushes and shuts reporting down. Never throws. Called on clean exit
    /// so queued reports go out; crash paths rely on best-effort delivery.
    /// </summary>
    public void Shutdown()
    {
        lock (_lock)
        {
            try
            {
                _reporter?.Dispose();
            }
            catch { }
            _reporter = null;
        }
    }
}
