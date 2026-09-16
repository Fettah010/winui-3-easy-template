using System;
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

    private bool _loaded;

    [ObservableProperty]
    private int _selectedThemeIndex;

    [ObservableProperty]
    private int _selectedChannelIndex;

    [ObservableProperty]
    private bool _autoCheckOnStartup;

    /// <summary>
    /// Whether detected updates download and prepare automatically
    /// (the popup only asks for restart). Off = the popup asks before
    /// anything downloads. Bound to the auto-install toggle.
    /// </summary>
    [ObservableProperty]
    private bool _autoInstallUpdates;

    [ObservableProperty]
    private bool _minimizeToTray;

    [ObservableProperty]
    private bool _autoStart;

    [ObservableProperty]
    private string _appVersion = string.Empty;

    /// <summary>
    /// Update cards, bound by <c>SettingsPage.xaml</c>. The interactive
    /// flow lives in the update popup (<see cref="Services.UpdateDialogService"/>):
    /// Settings only shows resting state (version) plus the external-mode
    /// status card with its owner action.
    /// </summary>
    [ObservableProperty]
    private Visibility _updateCardVisibility = Visibility.Visible;

    /// <summary>
    /// Engine-owned cards (channel + check-now). Collapsed when updates are
    /// externally managed (Store / AppInstaller) — the status card carries
    /// the handler text and action instead.
    /// </summary>
    [ObservableProperty]
    private Visibility _updateCheckCardVisibility = Visibility.Visible;

    [ObservableProperty]
    private string _updateCardHeader = string.Empty;

    [ObservableProperty]
    private string _updateCardDescription = string.Empty;

    [ObservableProperty]
    private string _updateStatusMessage = string.Empty;

    [ObservableProperty]
    private Visibility _installButtonVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private bool _isInstallEnabled = true;

    [ObservableProperty]
    private string _installButtonText = string.Empty;

    /// <summary>
    /// Both distributions support the toggle: unpackaged runs manage
    /// HKCU Run directly; packaged runs drive the manifest StartupTask
    /// (Windows may show a consent prompt on enable).
    /// </summary>
    public bool AutoStartAvailable => true;

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

        try { AutoInstallUpdates = SettingsService.Current.AutoInstallUpdates; }
        catch { AutoInstallUpdates = true; }

        try { MinimizeToTray = SettingsService.Current.MinimizeToTray; }
        catch { MinimizeToTray = true; }

        try
        {
            if (AppInfo.IsPackaged)
                _ = LoadPackagedAutoStartAsync();
            else
                AutoStart = AutoStartService.Current.IsEnabled();
        }
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
    /// Re-reads the resting update labels from the current language.
    /// Called once at construction and by the page on every language
    /// change. The interactive flow lives in the update popup — Settings
    /// only shows resting state (installed version).
    /// </summary>
    public void RefreshUpdateLabels()
    {
        var loc = LocalizationService.Current;
        if (AppFeatures.IsExternallyManaged)
        {
            // Externally owned (Store / AppInstaller, or any packaged run:
            // the install dir is read-only there): no channel, no check
            // button — the status card carries the handler, the version,
            // and the action (both entry points route to the owner).
            UpdateCheckCardVisibility = Visibility.Collapsed;
            UpdateCardVisibility = Visibility.Visible;
            UpdateCardHeader = loc.GetString("SettingsUpdates");
            UpdateCardDescription = ExternalHandlerText(loc);
            UpdateStatusMessage = AppInfo.Current.VersionDisplay;
            InstallButtonText = AppFeatures.UpdateMode == "store"
                ? loc.GetString("SettingsOpenStore")
                : loc.GetString("SettingsOpenWindowsSettings");
            InstallButtonVisibility = Visibility.Visible;
            IsInstallEnabled = true;
            return;
        }
        UpdateCheckCardVisibility = Visibility.Visible;
        UpdateCardVisibility = Visibility.Visible;
        UpdateCardHeader = loc.GetString("SettingsCheckHeader");
        UpdateCardDescription = loc.GetString("SettingsCheckDesc");
        UpdateStatusMessage = AppInfo.Current.VersionDisplay;
        InstallButtonVisibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Opens the external update owner: the Store listing (store mode),
    /// Windows Apps settings where the feed can be managed (appinstaller
    /// mode, or any packaged run whose scaffold engine cannot apply).
    /// Never throws.
    /// </summary>
    public async Task OpenExternalUpdateSourceAsync()
    {
        try
        {
            string? uri = AppFeatures.UpdateMode switch
            {
                "store" => AppInfo.PackageFamilyName is string pfn
                    ? "ms-windows-store://pdp/?PFN=" + Uri.EscapeDataString(pfn)
                    : "ms-windows-store://home",
                "appinstaller" => "ms-settings:appsfeatures",
                _ => AppInfo.IsPackaged ? "ms-settings:appsfeatures" : null,
            };
            if (uri is null)
                return;
            _ = await Windows.System.Launcher.LaunchUriAsync(new Uri(uri));
        }
        catch { }
    }

    /// <summary>
    /// Handler line for the slim status surface: Store text in store mode,
    /// packaged-dual text for engine scaffolds running packaged, feed text
    /// otherwise. Pure lookup (headless-testable).
    /// </summary>
    internal static string ExternalHandlerText(LocalizationService loc)
    {
        if (AppFeatures.UpdateMode == "store")
            return loc.GetString("SettingsUpdatesExternalStore");
        if (AppInfo.IsPackaged)
            return loc.GetString("SettingsUpdatesExternalPackaged");
        return loc.GetString("SettingsUpdatesExternalAppInstaller");
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

    partial void OnAutoInstallUpdatesChanged(bool value)
    {
        if (!_loaded) return;
        try { SettingsService.Current.AutoInstallUpdates = value; } catch { }
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
        if (AppInfo.IsPackaged)
        {
            _ = ApplyPackagedAutoStartAsync(value);
            return;
        }
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

    /// <summary>Loads the StartupTask state into the toggle. Never throws.</summary>
    private async Task LoadPackagedAutoStartAsync()
    {
        try
        {
            AutoStart = await AutoStartService.Current.IsPackagedStartupEnabledAsync();
        }
        catch { }
    }

    /// <summary>
    /// Applies the toggle to the StartupTask, reverting on mismatch so the
    /// UI never lies (e.g. DisabledByUser can only change in Settings).
    /// Never throws.
    /// </summary>
    private async Task ApplyPackagedAutoStartAsync(bool value)
    {
        try
        {
            bool actual = await AutoStartService.Current.SetPackagedStartupEnabledAsync(value);
            if (actual != value)
                AutoStart = actual;
        }
        catch
        {
            await LoadPackagedAutoStartAsync();
        }
    }
}
