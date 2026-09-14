using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevTemWinUi3.Services;
using Microsoft.UI.Xaml;

namespace DevTemWinUi3.ViewModels;

public partial class SettingsPageViewModel : ObservableObject
{
    private readonly IUpdateService? _updates;
    private readonly IFilePickerService _pickers;

    /// <summary>
    /// UI thread captured at construction (the page resolves this VM on the
    /// UI thread): progress callbacks arrive on background threads and must
    /// be marshalled in. Null in unit tests — updates apply directly.
    /// </summary>
    private readonly SynchronizationContext? _uiThread = SynchronizationContext.Current;

    private bool _checking;
    private bool _installing;

    [ObservableProperty]
    private int _selectedThemeIndex;

    [ObservableProperty]
    private int _selectedChannelIndex;

    [ObservableProperty]
    private bool _autoCheckOnStartup;

    [ObservableProperty]
    private bool _minimizeToTray;

    [ObservableProperty]
    private bool _autoStart;

    [ObservableProperty]
    private string _appVersion = string.Empty;

    /// <summary>Update-flow state, bound by <c>SettingsPage.xaml</c>.</summary>
    [ObservableProperty]
    private bool _isCheckUpdatesEnabled = true;

    [ObservableProperty]
    private string _checkUpdatesButtonText = string.Empty;

    [ObservableProperty]
    private Visibility _updateCardVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private string _updateCardHeader = string.Empty;

    [ObservableProperty]
    private string _updateCardDescription = string.Empty;

    [ObservableProperty]
    private string _updateStatusMessage = string.Empty;

    [ObservableProperty]
    private Visibility _downloadProgressVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private int _downloadProgress;

    [ObservableProperty]
    private Visibility _installButtonVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private bool _isInstallEnabled = true;

    [ObservableProperty]
    private string _installButtonText = string.Empty;

    /// <summary>
    /// MSIX-packaged apps cannot use registry autostart (virtualized store):
    /// the toggle binds IsEnabled to this and shows a note instead.
    /// Unpackaged runs (including Velopack installs) manage HKCU Run directly.
    /// </summary>
    public bool AutoStartAvailable => !AppInfo.IsPackaged;

    /// <summary>
    /// Guards change handlers while the constructor loads persisted values,
    /// so loading never writes back or triggers side effects.
    /// </summary>
    private bool _loaded;

    public SettingsPageViewModel(IUpdateService? updates = null, IFilePickerService? pickers = null)
    {
        // Null-tolerant by design: the updates feature may be compiled out
        // (template combos) and tests construct the VM directly. Update
        // methods no-op without a service; pickers fall back to real ones.
        _updates = updates;
        _pickers = pickers ?? new FilePickerService();
        RefreshFromServices();
        RefreshUpdateLabels();
        _loaded = true;
    }

    /// <summary>
    /// Re-reads every bound value from the services (used after reset/import
    /// changed preferences behind the ViewModel's back). Safe to call any
    /// time; never throws.
    /// </summary>
    public void RefreshFromServices()
    {
        try
        {
            AppVersion = AppInfo.Current.Version;

            var theme = SettingsService.Current.Theme;
            SelectedThemeIndex = theme switch
            {
                "Light" => 1,
                "Dark" => 2,
                _ => 0
            };
        }
        catch { SelectedThemeIndex = 0; }

        try
        {
            SelectedChannelIndex = SettingsService.Current.Channel == ChannelResolver.Beta ? 1 : 0;
        }
        catch { SelectedChannelIndex = 0; }

        try { AutoCheckOnStartup = SettingsService.Current.AutoCheck; }
        catch { AutoCheckOnStartup = true; }

        try { MinimizeToTray = SettingsService.Current.MinimizeToTray; }
        catch { MinimizeToTray = true; }

        try { AutoStart = AutoStartService.Current.IsEnabled(); }
        catch { }
    }

    /// <summary>Resets all preferences to defaults and refreshes the UI.</summary>
    [RelayCommand]
    private void ResetSettings()
    {
        SettingsBackupService.ResetAll();
        RefreshFromServices();
        var loc = LocalizationService.Current;
        NotificationService.Current.Success(
            loc.GetString("SettingsTitle"), loc.GetString("SettingsBackupResetDone"));
    }

    /// <summary>Exports preferences to a JSON file. Returns false on failure.</summary>
    public bool ExportTo(string path) => SettingsBackupService.ExportToFile(path);

    /// <summary>
    /// Exports preferences to a user-picked JSON file. Returns false on failure.
    /// </summary>
    [RelayCommand]
    private async Task ExportSettingsAsync()
    {
        try
        {
            var path = await _pickers.PickSaveFileAsync("devtem-settings");
            if (path is null)
                return;

            var loc = LocalizationService.Current;
            if (ExportTo(path))
                NotificationService.Current.Success(
                    loc.GetString("SettingsTitle"), loc.GetString("SettingsBackupExportDone"));
            else
                NotificationService.Current.Error(
                    loc.GetString("SettingsTitle"), loc.GetString("SettingsBackupImportFailed"));
        }
        catch { }
    }

    /// <summary>
    /// Imports preferences from a user-picked JSON file, refreshing the UI
    /// and re-applying the theme when valid. Returns false on failure.
    /// </summary>
    [RelayCommand]
    private async Task ImportSettingsAsync()
    {
        try
        {
            var path = await _pickers.PickOpenFileAsync();
            if (path is null)
                return;

            var loc = LocalizationService.Current;
            if (ImportFrom(path))
            {
                ThemeService.Current.ApplyToMainWindow();
                NotificationService.Current.Success(
                    loc.GetString("SettingsTitle"), loc.GetString("SettingsBackupImportDone"));
            }
            else
            {
                NotificationService.Current.Error(
                    loc.GetString("SettingsTitle"), loc.GetString("SettingsBackupImportFailed"));
            }
        }
        catch { }
    }

    /// <summary>
    /// Imports preferences from a JSON file, refreshing the UI when valid.
    /// Returns false when the file is missing or unparseable.
    /// </summary>
    public bool ImportFrom(string path)
    {
        bool ok = SettingsBackupService.ImportFromFile(path);
        if (ok)
            RefreshFromServices();
        return ok;
    }

    /// <summary>
    /// Re-reads the resting (non-busy) update labels from the current
    /// language. Called once at construction and by the page on every
    /// language change — busy states are owned by the running operation and
    /// must never be overwritten here.
    /// </summary>
    public void RefreshUpdateLabels()
    {
        var loc = LocalizationService.Current;
        if (!_checking)
            CheckUpdatesButtonText = loc.GetString("SettingsCheckNow");
        UpdateCardHeader = loc.GetString("SettingsCheckHeader");
        if (UpdateCardVisibility == Visibility.Collapsed)
            UpdateCardDescription = loc.GetString("SettingsStatusIdle");
        if (InstallButtonVisibility == Visibility.Visible && IsInstallEnabled)
            InstallButtonText = loc.GetString("SettingsInstall");
    }

    /// <summary>
    /// Runs the update check and publishes the outcome to the bound status
    /// card. Safe to call from any thread; re-entrant calls while a check
    /// is running return immediately. Cancellation (page left) resets the
    /// button quietly — never an error toast.
    /// </summary>
    public async Task CheckForUpdatesAsync(CancellationToken ct = default)
    {
        if (_updates is null || _checking)
            return;

        var loc = LocalizationService.Current;

        if (!_updates.IsInstalled)
        {
            // Unpackaged run: explain via the animated in-app toast (modern
            // WinUI style) instead of the inline status card.
            AppLog.Information("Update check: app is not installed, showing toast");
            NotificationService.Current.Info(loc.GetString("NotifUpdates"), loc.GetString("SettingsNotInstalled"));
            return;
        }

        _checking = true;
        IsCheckUpdatesEnabled = false;
        CheckUpdatesButtonText = loc.GetString("SettingsChecking");

        try
        {
            ct.ThrowIfCancellationRequested();
            var result = await _updates.CheckAsync();
            ct.ThrowIfCancellationRequested();

            UpdateCardVisibility = Visibility.Visible;
            UpdateCardHeader = loc.GetString("SettingsCheckHeader");
            UpdateCardDescription = loc.GetString("SettingsCheckHeader");
            DownloadProgressVisibility = Visibility.Collapsed;
            DownloadProgress = 0;

            if (!result.HasUpdate)
            {
                UpdateStatusMessage = loc.GetString("SettingsNoUpdate");
                InstallButtonVisibility = Visibility.Collapsed;
                NotificationService.Current.Success(loc.GetString("NotifUpdates"), loc.GetString("SettingsNoUpdate"));
            }
            else
            {
                string version = result.Version ?? string.Empty;
                UpdateStatusMessage = loc.GetString("UpdateAvailableVersion", version);
                InstallButtonVisibility = Visibility.Visible;
                IsInstallEnabled = true;
                InstallButtonText = loc.GetString("SettingsInstall");
                NotificationService.Current.Info(
                    loc.GetString("NotifUpdates"),
                    loc.GetString("UpdateAvailablePrompt", version));
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            UpdateCardVisibility = Visibility.Visible;
            UpdateCardHeader = loc.GetString("SettingsCheckHeader");
            UpdateCardDescription = loc.GetString("SettingsCheckHeader");
            UpdateStatusMessage = $"{loc.GetString("SettingsCheckFailed")}: {ex.Message}";
            InstallButtonVisibility = Visibility.Collapsed;
            NotificationService.Current.Error(loc.GetString("SettingsCheckFailed"), ex.Message);
        }
        finally
        {
            _checking = false;
            IsCheckUpdatesEnabled = true;
            CheckUpdatesButtonText = loc.GetString("SettingsCheckNow");
        }
    }

    /// <summary>
    /// Downloads the pending update with determinate progress, then applies
    /// it (restart). No-op when nothing is pending or an install is already
    /// running. Progress arrives on a background thread and is marshalled
    /// to the UI thread captured at construction.
    /// </summary>
    public async Task InstallPendingUpdateAsync(CancellationToken ct = default)
    {
        if (_updates is null || _installing || !_updates.HasPendingUpdate)
            return;

        var loc = LocalizationService.Current;
        _installing = true;
        IsInstallEnabled = false;
        InstallButtonText = loc.GetString("SettingsDownloading");
        IsCheckUpdatesEnabled = false;

        // Smooth determinate progress while the delta/full package
        // downloads; the callback runs on a background thread.
        DownloadProgressVisibility = Visibility.Visible;
        DownloadProgress = 0;
        UpdateStatusMessage = loc.GetString("SettingsDownloadingProgress", 0);

        try
        {
            await _updates.DownloadPendingUpdateAsync(percent =>
                SetOnUiThread(() =>
                {
                    DownloadProgress = percent;
                    UpdateStatusMessage = loc.GetString("SettingsDownloadingProgress", percent);
                }));
            ct.ThrowIfCancellationRequested();

            UpdateStatusMessage = loc.GetString("SettingsInstalling");
            DownloadProgressVisibility = Visibility.Collapsed;
            InstallButtonVisibility = Visibility.Collapsed;
            NotificationService.Current.Success(
                loc.GetString("NotifUpdates"), loc.GetString("UpdateDownloadedRestart"));

            await Task.Delay(300, ct);
            _updates.ApplyPendingUpdateAndRestart();
        }
        catch (OperationCanceledException)
        {
            ResetInstallState();
        }
        catch (Exception ex)
        {
            UpdateStatusMessage = loc.GetString("InstallFailedDetail", ex.Message);
            DownloadProgressVisibility = Visibility.Collapsed;
            IsInstallEnabled = true;
            InstallButtonText = loc.GetString("SettingsInstall");
            IsCheckUpdatesEnabled = true;
            NotificationService.Current.Error(loc.GetString("SettingsCheckFailed"), ex.Message);
        }
        finally
        {
            _installing = false;
        }
    }

    private void ResetInstallState()
    {
        var loc = LocalizationService.Current;
        DownloadProgressVisibility = Visibility.Collapsed;
        DownloadProgress = 0;
        IsInstallEnabled = true;
        InstallButtonText = loc.GetString("SettingsInstall");
        IsCheckUpdatesEnabled = true;
    }

    private void SetOnUiThread(Action update)
    {
        try
        {
            if (_uiThread is null)
                update();
            else
                _uiThread.Post(_ => update(), null);
        }
        catch { }
    }

    partial void OnSelectedThemeIndexChanged(int value)
    {
        // Guard against programmatic resets (e.g. ComboBox display refresh):
        // only 0-2 are real themes, anything else must not write settings.
        if (value is < 0 or > 2)
            return;

        var theme = value switch
        {
            1 => "Light",
            2 => "Dark",
            _ => "System"
        };

        ThemeService.Current.SetTheme(theme);
        ThemeService.Current.ApplyToMainWindow();
    }

    partial void OnSelectedChannelIndexChanged(int value)
    {
        // Only Stable (0) and Beta (1) exist; anything else falls back to stable.
        var channel = value == 1 ? ChannelResolver.Beta : ChannelResolver.Stable;
        try { SettingsService.Current.Channel = channel; } catch { }
        try { _updates?.SetChannel(channel); } catch { }
    }

    partial void OnAutoCheckOnStartupChanged(bool value)
    {
        if (!_loaded) return;
        try { SettingsService.Current.AutoCheck = value; } catch { }
    }

    partial void OnMinimizeToTrayChanged(bool value)
    {
        if (!_loaded) return;
#if (tray)
        try { SettingsService.Current.MinimizeToTray = value; } catch { }
        try { SystemTrayService.Current.UpdateSettings(); } catch { }
#endif
    }

    partial void OnAutoStartChanged(bool value)
    {
        if (!_loaded) return;
        // MSIX-packaged runs cannot use registry autostart (virtualized):
        // the toggle is disabled there (see AutoStartAvailable).
        if (!AutoStartAvailable) return;
        try
        {
            AutoStartService.Current.SetEnabled(value);
            // Re-read: if the registry write didn't stick, revert the toggle
            // so the UI never lies about the real state.
            bool actual = AutoStartService.Current.IsEnabled();
            if (actual != value)
                AutoStart = actual;
        }
        catch
        {
            try { AutoStart = AutoStartService.Current.IsEnabled(); } catch { }
        }
    }
}
