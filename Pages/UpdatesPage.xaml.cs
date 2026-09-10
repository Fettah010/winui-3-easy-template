using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DevTemWinUi3.Services;
using Velopack;

namespace DevTemWinUi3.Pages;

public sealed partial class UpdatesPage : Page
{
    public UpdateService UpdateService { get; } = UpdateService.Current;

    private static readonly string[] Channels = { "stable", "beta", "dev" };

    private UpdateInfo? _pendingUpdate;

    public UpdatesPage()
    {
        this.InitializeComponent();
        CurrentVersionText.Text = $"Version {UpdateService.CurrentVersion}";
        UpdateService.SetChannel(Channels[Math.Clamp(ChannelSegment.SelectedIndex, 0, Channels.Length - 1)]);
    }

    private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        _pendingUpdate = null;
        CheckProgress.IsActive = true;
        CheckUpdateButton.IsEnabled = false;
        UpToDateInfoBar.IsOpen = false;
        ErrorInfoBar.IsOpen = false;
        NotInstalledInfoBar.IsOpen = false;
        UpdateAvailablePanel.Visibility = Visibility.Collapsed;

        if (!UpdateService.IsInstalled)
        {
            NotInstalledInfoBar.Message = UpdateService.NotInstalledMessage;
            NotInstalledInfoBar.IsOpen = true;
            CheckProgress.IsActive = false;
            CheckUpdateButton.IsEnabled = true;
            return;
        }

        try
        {
            LoggingService.Log.Information("Manual update check requested");
            var update = await UpdateService.CheckForUpdatesAsync();
            if (update is null)
            {
                LoggingService.Log.Information("No update available");
                UpToDateInfoBar.Message = UpdateService.NoUpdateMessage;
                UpToDateInfoBar.IsOpen = true;
            }
            else
            {
                _pendingUpdate = update;
                UpdateVersionTagText.Text = $"v{update.TargetFullRelease.Version}";
                UpdateNotesText.Text = string.IsNullOrWhiteSpace(update.TargetFullRelease.NotesMarkdown)
                    ? $"Version {update.TargetFullRelease.Version} is ready to install."
                    : update.TargetFullRelease.NotesMarkdown;
                UpdateAvailablePanel.Visibility = Visibility.Visible;
                InstallUpdateButton.IsEnabled = true;
                LoggingService.Log.Information("Update {Version} available", update.TargetFullRelease.Version);
            }
        }
        catch (Exception ex)
        {
            LoggingService.Log.Error(ex, "Update check failed");
            ErrorInfoBar.Message = ex.Message;
            ErrorInfoBar.IsOpen = true;
        }
        finally
        {
            CheckProgress.IsActive = false;
            CheckUpdateButton.IsEnabled = true;
        }
    }

    private async void InstallUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_pendingUpdate is null)
        {
            CheckUpdateButton_Click(sender, e);
            return;
        }

        InstallUpdateButton.IsEnabled = false;
        DownloadProgress.IsActive = true;
        DownloadProgress.IsIndeterminate = true;

        try
        {
            await UpdateService.DownloadUpdatesAsync(_pendingUpdate, p =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (p > 0)
                    {
                        DownloadProgress.IsIndeterminate = false;
                        DownloadProgress.Value = p;
                    }
                });
            });

            DownloadProgress.IsActive = false;
            LoggingService.Log.Information("Update downloaded - applying and restarting");
            UpdateService.ApplyUpdatesAndRestart(_pendingUpdate);
        }
        catch (Exception ex)
        {
            LoggingService.Log.Error(ex, "Update install failed");
            ErrorInfoBar.Message = ex.Message;
            ErrorInfoBar.IsOpen = true;
            DownloadProgress.IsActive = false;
            InstallUpdateButton.IsEnabled = true;
        }
    }
}