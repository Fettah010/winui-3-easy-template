using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevTemWinUi3.Services;
using Microsoft.UI.Dispatching;
using Velopack;

namespace DevTemWinUi3.ViewModels;

public partial class UpdatesPageViewModel : ObservableObject
{
    private readonly UpdateService _updateService = UpdateService.Current;
    private readonly DispatcherQueue _dispatcherQueue;

    [ObservableProperty]
    private string _currentVersion = string.Empty;

    [ObservableProperty]
    private bool _isInstalled;

    [ObservableProperty]
    private bool _isCheckInProgress;

    [ObservableProperty]
    private bool _isDownloadInProgress;

    [ObservableProperty]
    private double _downloadProgress;

    [ObservableProperty]
    private bool _isDownloadIndeterminate;

    [ObservableProperty]
    private UpdateInfo? _pendingUpdate;

    [ObservableProperty]
    private bool _isUpdateAvailable;

    [ObservableProperty]
    private string _updateVersionTag = string.Empty;

    [ObservableProperty]
    private string _updateNotes = string.Empty;

    [ObservableProperty]
    private bool _isUpToDate;

    [ObservableProperty]
    private string _upToDateMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isNotInstalled;

    [ObservableProperty]
    private string _notInstalledMessage = string.Empty;

    [ObservableProperty]
    private int _selectedChannelIndex;

    public UpdatesPageViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        CurrentVersion = $"Version {_updateService.CurrentVersion}";
        IsInstalled = _updateService.IsInstalled;
        NotInstalledMessage = UpdateService.NotInstalledMessage;

        // Load persisted channel
        var persistedChannel = SettingsService.Current.Channel;
        var channels = new[] { "stable", "beta", "dev" };
        var channelIndex = Array.IndexOf(channels, persistedChannel);
        SelectedChannelIndex = channelIndex >= 0 ? channelIndex : 0;
        _updateService.SetChannel(channels[SelectedChannelIndex]);
    }

    partial void OnSelectedChannelIndexChanged(int value)
    {
        var channels = new[] { "stable", "beta", "dev" };
        if (value >= 0 && value < channels.Length)
        {
            _updateService.SetChannel(channels[value]);
        }
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        PendingUpdate = null;
        IsUpdateAvailable = false;
        IsUpToDate = false;
        HasError = false;
        IsNotInstalled = false;

        if (!IsInstalled)
        {
            IsNotInstalled = true;
            return;
        }

        IsCheckInProgress = true;

        try
        {
            LoggingService.Log.Information("Manual update check requested");
            var update = await _updateService.CheckForUpdatesAsync();
            if (update is null)
            {
                LoggingService.Log.Information("No update available");
                IsUpToDate = true;
                UpToDateMessage = UpdateService.NoUpdateMessage;
            }
            else
            {
                PendingUpdate = update;
                UpdateVersionTag = $"v{update.TargetFullRelease.Version}";
                UpdateNotes = string.IsNullOrWhiteSpace(update.TargetFullRelease.NotesMarkdown)
                    ? $"Version {update.TargetFullRelease.Version} is ready to install."
                    : update.TargetFullRelease.NotesMarkdown;
                IsUpdateAvailable = true;
                LoggingService.Log.Information("Update {Version} available", update.TargetFullRelease.Version);
            }
        }
        catch (Exception ex)
        {
            LoggingService.Log.Error(ex, "Update check failed");
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsCheckInProgress = false;
        }
    }

    [RelayCommand]
    private async Task DownloadAndInstallAsync()
    {
        if (PendingUpdate is null)
        {
            await CheckForUpdatesAsync();
            return;
        }

        IsDownloadInProgress = true;
        IsDownloadIndeterminate = true;
        DownloadProgress = 0;

        try
        {
            await _updateService.DownloadUpdatesAsync(PendingUpdate, progress =>
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    if (progress > 0)
                    {
                        IsDownloadIndeterminate = false;
                        DownloadProgress = progress;
                    }
                });
            });

            IsDownloadInProgress = false;
            LoggingService.Log.Information("Update downloaded - applying and restarting");
            _updateService.ApplyUpdatesAndRestart(PendingUpdate);
        }
        catch (Exception ex)
        {
            LoggingService.Log.Error(ex, "Update install failed");
            HasError = true;
            ErrorMessage = ex.Message;
            IsDownloadInProgress = false;
        }
    }
}
