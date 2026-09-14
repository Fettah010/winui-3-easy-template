using System;
using System.Diagnostics;
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
    private static TimeSpan CheckInterval => UpdateService.PeriodicCheckInterval;
#endif
#if (updates == 'basic')
    private static BasicGithubUpdateService Updater => BasicGithubUpdateService.Current;
    private static TimeSpan CheckInterval => BasicGithubUpdateService.PeriodicCheckInterval;
#endif
    /// <summary>
    /// Re-checks for updates on the backend's periodic interval
    /// while the app stays running. <see cref="CheckForUpdatesAsync"/> itself
    /// honors the auto-check setting and the installed-app guard, so this loop
    /// is a no-op for opted-out or unpackaged runs (one log line per tick).
    /// </summary>
    public async Task RunPeriodicChecksAsync(Window? mainWindow)
    {
        try
        {
            AppLog.Information(
                "Periodic update checks scheduled every {Interval}", CheckInterval);
            using var timer = new PeriodicTimer(CheckInterval);
            while (await timer.WaitForNextTickAsync())
            {
                await CheckForUpdatesAsync(mainWindow);
            }
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Periodic update check loop ended");
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
            var result = await svc.CheckAsync();
            if (!result.HasUpdate)
            {
                AppLog.Information("Auto-update: already on the latest version");
                return;
            }

            AppLog.Information(
                "Auto-update: v{Version} available, downloading in the background",
                result.Version);
            await svc.DownloadPendingUpdateAsync();

            if (mainWindow is null)
                return;
            mainWindow.DispatcherQueue.TryEnqueue(() => PromptRestart(result.Version, mainWindow));
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Auto-update check failed");
        }
    }

    private async void PromptRestart(string? version, Window? window)
    {
        try
        {
            if (window?.Content is null)
                return;

            var loc = LocalizationService.Current;
            var dialog = new ContentDialog
            {
                XamlRoot = window.Content.XamlRoot,
                Title = loc.GetString("UpdateRestartTitle", version ?? string.Empty),
                Content = loc.GetString("UpdateRestartBody"),
                PrimaryButtonText = loc.GetString("UpdateRestartNow"),
                CloseButtonText = loc.GetString("UpdateRestartLater"),
                DefaultButton = ContentDialogButton.Primary
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                Updater.ApplyPendingUpdateAndRestart();
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Restart prompt failed");
        }
    }
}
