using System;
using System.Threading.Tasks;
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

#if (updates)
    /// <summary>
    /// Re-checks for updates on <see cref="UpdateService.PeriodicCheckInterval"/>
    /// while the app stays running. <see cref="CheckForUpdatesAsync"/> itself
    /// honors the auto-check setting and the installed-app guard, so this loop
    /// is a no-op for opted-out or unpackaged runs (one log line per tick).
    /// </summary>
    public async Task RunPeriodicChecksAsync(Window? mainWindow)
    {
        try
        {
            LoggingService.Log.Information(
                "Periodic update checks scheduled every {Interval}", UpdateService.PeriodicCheckInterval);
            using var timer = new PeriodicTimer(UpdateService.PeriodicCheckInterval);
            while (await timer.WaitForNextTickAsync())
            {
                await CheckForUpdatesAsync(mainWindow);
            }
        }
        catch (Exception ex)
        {
            LoggingService.Log.Error(ex, "Periodic update check loop ended");
        }
    }

    public async Task CheckForUpdatesAsync(Window? mainWindow)
    {
        var svc = UpdateService.Current;

        if (!svc.IsInstalled)
        {
            LoggingService.Log.Information("Auto-update check skipped: app is not installed");
            return;
        }

        try
        {
            if (!SettingsService.Current.AutoCheck)
            {
                LoggingService.Log.Information("Auto-update check skipped: disabled in settings");
                return;
            }
        }
        catch { }

        try
        {
            var result = await svc.CheckAsync();
            if (!result.HasUpdate)
            {
                LoggingService.Log.Information("Auto-update: already on the latest version");
                return;
            }

            LoggingService.Log.Information(
                "Auto-update: v{Version} available, downloading in the background",
                result.Version);
            await svc.DownloadPendingUpdateAsync();

            if (mainWindow is null)
                return;
            mainWindow.DispatcherQueue.TryEnqueue(() => PromptRestart(result.Version, mainWindow));
        }
        catch (Exception ex)
        {
            LoggingService.Log.Error(ex, "Auto-update check failed");
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
                UpdateService.Current.ApplyPendingUpdateAndRestart();
        }
        catch (Exception ex)
        {
            LoggingService.Log.Error(ex, "Restart prompt failed");
        }
    }
#endif
}
