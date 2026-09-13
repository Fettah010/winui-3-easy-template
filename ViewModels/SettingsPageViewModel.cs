using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevTemWinUi3.Services;

namespace DevTemWinUi3.ViewModels;

public partial class SettingsPageViewModel : ObservableObject
{
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

    public SettingsPageViewModel()
    {
        RefreshFromServices();
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

        try { AutoStart = SystemTrayService.IsAutoStartEnabled(); }
        catch { }
    }

    /// <summary>Resets all preferences to defaults and refreshes the UI.</summary>
    [RelayCommand]
    private void ResetSettings()
    {
        SettingsBackupService.ResetAll();
        RefreshFromServices();
    }

    /// <summary>Exports preferences to a JSON file. Returns false on failure.</summary>
    public bool ExportTo(string path) => SettingsBackupService.ExportToFile(path);

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
        try { UpdateService.Current.SetChannel(channel); } catch { }
    }

    partial void OnAutoCheckOnStartupChanged(bool value)
    {
        if (!_loaded) return;
        try { SettingsService.Current.AutoCheck = value; } catch { }
    }

    partial void OnMinimizeToTrayChanged(bool value)
    {
        if (!_loaded) return;
        try { SettingsService.Current.MinimizeToTray = value; } catch { }
        try { SystemTrayService.Current.UpdateSettings(); } catch { }
    }

    partial void OnAutoStartChanged(bool value)
    {
        if (!_loaded) return;
        // MSIX-packaged runs cannot use registry autostart (virtualized):
        // the toggle is disabled there (see AutoStartAvailable).
        if (!AutoStartAvailable) return;
        try
        {
            SystemTrayService.SetAutoStart(value);
            // Re-read: if the registry write didn't stick, revert the toggle
            // so the UI never lies about the real state.
            bool actual = SystemTrayService.IsAutoStartEnabled();
            if (actual != value)
                AutoStart = actual;
        }
        catch
        {
            try { AutoStart = SystemTrayService.IsAutoStartEnabled(); } catch { }
        }
    }
}
