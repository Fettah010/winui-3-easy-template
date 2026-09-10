using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyWinUIApp.Services;
using Velopack;

namespace MyWinUIApp.Pages;

public sealed partial class UpdatesPage : Page
{
    public UpdateService UpdateService { get; } = UpdateService.Current;

    private static readonly string[] Channels = { "stable", "beta", "dev" };

    public UpdatesPage()
    {
        this.InitializeComponent();
        CurrentVersionText.Text = $"Version {UpdateService.CurrentVersion}";
        UpdateService.SetChannel(Channels[Math.Clamp(ChannelSegment.SelectedIndex, 0, Channels.Length - 1)]);
    }

    private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        CheckProgress.IsActive = true;
        CheckUpdateButton.IsEnabled = false;
        UpToDateInfoBar.IsOpen = false;
        ErrorInfoBar.IsOpen = false;
        UpdateAvailablePanel.Visibility = Visibility.Collapsed;

        try
        {
            var update = await UpdateService.CheckForUpdatesAsync();
            if (update is null)
            {
                UpToDateInfoBar.Message = UpdateService.NoUpdateMessage;
                UpToDateInfoBar.IsOpen = true;
            }
            else
            {
                UpdateVersionTagText.Text = $"v{update.TargetFullRelease.Version}";
                UpdateNotesText.Text = string.IsNullOrWhiteSpace(update.TargetFullRelease.NotesMarkdown)
                    ? $"Version {update.TargetFullRelease.Version} is ready to install."
                    : update.TargetFullRelease.NotesMarkdown;
                UpdateAvailablePanel.Visibility = Visibility.Visible;
                InstallUpdateButton.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
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
        InstallUpdateButton.IsEnabled = false;
        DownloadProgress.IsActive = true;
        DownloadProgress.IsIndeterminate = true;

        try
        {
            var update = await UpdateService.CheckForUpdatesAsync();
            if (update is null)
            {
                UpToDateInfoBar.Message = UpdateService.NoUpdateMessage;
                UpToDateInfoBar.IsOpen = true;
                return;
            }

            await UpdateService.DownloadUpdatesAsync(update, p =>
            {
                var progress = DispatcherQueue;
                progress.TryEnqueue(() =>
                {
                    if (p > 0)
                    {
                        DownloadProgress.IsIndeterminate = false;
                        DownloadProgress.Value = p;
                    }
                });
            });

            DownloadProgress.IsActive = false;
            UpdateService.ApplyUpdatesAndRestart(update);
        }
        catch (Exception ex)
        {
            ErrorInfoBar.Message = ex.Message;
            ErrorInfoBar.IsOpen = true;
            DownloadProgress.IsActive = false;
            InstallUpdateButton.IsEnabled = true;
        }
    }
}