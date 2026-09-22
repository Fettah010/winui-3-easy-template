using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DevTemWinUi3.Services.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevTemWinUi3.Services;

/// <summary>
/// Deferred post-launch background work: database init, the update check,
/// and the periodic re-check loop. Owned here so <c>App</c> stays launch
/// orchestration (splash → window → protocol). Every path is never-throw
/// guarded; unpackaged runs log one line and skip.
/// </summary>
public sealed class BackgroundUpdateService
{
    public static BackgroundUpdateService Current { get; } = new();

    private BackgroundUpdateService() { }

#if (updates == 'velopack')
    private static UpdateService Updater => UpdateService.Current;
    internal static TimeSpan CheckInterval => UpdateService.PeriodicCheckInterval;
#endif
#if (updates == 'basic')
    private static BasicGithubUpdateService Updater => BasicGithubUpdateService.Current;
    internal static TimeSpan CheckInterval => BasicGithubUpdateService.PeriodicCheckInterval;
#endif
    /// <summary>
    /// Re-checks for updates on the backend's periodic interval
    /// while the app stays running. <see cref="CheckForUpdatesAsync"/> itself
    /// honors the auto-check setting and the installed-app guard, so this loop
    /// is a no-op for opted-out or unpackaged runs (one log line per tick).
    /// P1-4: the loop's lifetime is tied to the window (its
    /// <c>Closed</c> cancels the wait) plus an optional caller token, so a
    /// closing window never leaves the timer — or the process — alive.
    /// </summary>
    public Task RunPeriodicChecksAsync(Window? mainWindow) =>
        RunPeriodicChecksAsync(mainWindow, CheckInterval, CancellationToken.None);

    /// <summary>
    /// Cancellation-aware loop entry (callers that own a lifetime pass
    /// their token). Never throws.
    /// </summary>
    public Task RunPeriodicChecksAsync(Window? mainWindow, CancellationToken cancellationToken) =>
        RunPeriodicChecksAsync(mainWindow, CheckInterval, cancellationToken);

    internal async Task RunPeriodicChecksAsync(
        Window? mainWindow, TimeSpan interval, CancellationToken cancellationToken)
    {
        BackgroundTaskRunner.EnsureDefaultsRegistered();
        if (interval <= TimeSpan.Zero)
            interval = CheckInterval;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        void OnClosed(object? sender, WindowEventArgs e)
        {
            try { linked.Cancel(); } catch { }
        }
        if (mainWindow is not null)
        {
            try { mainWindow.Closed += OnClosed; } catch { }
        }
        try
        {
            AppLog.Information(
                "Periodic update checks scheduled every {Interval}", interval);
            await BackgroundTaskRunner.RunPeriodicAsync(
                new BackgroundTaskContext(mainWindow), interval, linked.Token).ConfigureAwait(false);
            AppLog.Information("Periodic update checks stopped");
        }
        catch (OperationCanceledException)
        {
            // Window closed (or caller cancelled): expected shutdown, not an error.
            AppLog.Information("Periodic update checks stopped");
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Periodic update check loop ended");
        }
        finally
        {
            if (mainWindow is not null)
            {
                try { mainWindow.Closed -= OnClosed; } catch { }
            }
            try { linked.Cancel(); } catch { }
        }
    }

    public async Task CheckForUpdatesAsync(Window? mainWindow)
    {
        var watch = Stopwatch.StartNew();
        try { CrashReportingService.AddBreadcrumb("Update check started", "updates"); } catch { }
        try
        {
            await CheckForUpdatesCoreAsync(mainWindow);
        }
        finally
        {
            try
            {
                watch.Stop();
                AppMetrics.RecordUpdateCheck(watch.Elapsed.TotalMilliseconds);
                CrashReportingService.AddBreadcrumb("Update check finished", "updates");
            }
            catch { }
        }
    }

    private async Task CheckForUpdatesCoreAsync(Window? mainWindow)
    {
        var svc = Updater;

        // P0-3: the manager now initializes off-thread; warm it before any
        // synchronous read below (IsInstalled degrades to false until then).
        try { await svc.EnsureInitializedAsync().ConfigureAwait(false); } catch { }

        // A previous session's prepared update waits on disk (auto-apply
        // is off so launch never stalls): offer it visibly instead of a
        // fresh check. Any choice ends this tick (no double prompt). The
        // window is always up by now — this runs past the first frame.
        string? prepared = null;
        try { prepared = svc.PendingRestartVersion; } catch { }
        if (!string.IsNullOrWhiteSpace(prepared))
        {
            AppLog.Information(
                "Auto-update: v{Version} prepared last session, prompting",
                prepared);
            if (mainWindow is null)
                return;
            await UpdateDialogService.ShowReadyAsync(prepared, null).ConfigureAwait(false);
            return;
        }

        if (!svc.IsInstalled)
        {
            AppLog.Information("Auto-update check skipped: app is not installed");
            return;
        }

        try
        {
            if (!SettingsService.Current.AutoCheck)
            {
                AppLog.Information("Auto-update check skipped: disabled in settings");
                return;
            }
        }
        catch { }

        try
        {
            var result = await svc.CheckAsync().ConfigureAwait(false);
            if (!result.HasUpdate)
            {
                AppLog.Information("Auto-update: already on the latest version");
                return;
            }

            // Ask-mode: detection opens the popup and nothing downloads
            // until Install is pressed (SettingsService.AutoInstallUpdates).
            bool autoInstall = true;
            try { autoInstall = SettingsService.Current.AutoInstallUpdates; } catch { }
            if (!autoInstall)
            {
                AppLog.Information(
                    "Auto-update: v{Version} available, asking (auto-install off)",
                    result.Version);
                if (mainWindow is null)
                    return;
                await UpdateDialogService.ShowAvailableAsync(result).ConfigureAwait(false);
                return;
            }

            // P1-4: the check is cheap; the download is not. On metered
            // networks it waits for an unmetered connection unless the
            // user explicitly allows metered downloads.
            bool allowMetered = false;
            try { allowMetered = SettingsService.Current.DownloadOnMetered; } catch { }
            if (!allowMetered && IsMeteredConnection())
            {
                AppLog.Information(
                    "Auto-update: v{Version} available, download deferred (metered network)",
                    result.Version);
                return;
            }

            AppLog.Information(
                "Auto-update: v{Version} available, downloading in the background",
                result.Version);
            await svc.DownloadPendingUpdateAsync().ConfigureAwait(false);

            if (mainWindow is null)
                return;
            await UpdateDialogService.ShowReadyAsync(result.Version, result.ReleaseNotes)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Auto-update check failed");
        }
    }

    /// <summary>
    /// Whether the active connection is metered (capped/roaming/over-data-
    /// limit). Best-effort: false when unreadable. Never throws.
    /// </summary>
    internal static bool IsMeteredConnection()
    {
        try
        {
            var profile = Windows.Networking.Connectivity.NetworkInformation
                .GetInternetConnectionProfile();
            if (profile is null)
                return false;
            try
            {
                var cost = profile.GetConnectionCost();
                return cost.NetworkCostType
                    != Windows.Networking.Connectivity.NetworkCostType.Unrestricted;
            }
            catch
            {
                return false;
            }
        }
        catch
        {
            return false;
        }
    }

}
