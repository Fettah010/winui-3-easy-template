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
        AppVersion = AppInfo.Current.Version;

        try
        {
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

#if (tray)
        AutoStart = SystemTrayService.IsAutoStartEnabled();
#endif

        _loaded = true;
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
#if (updates)
        // Only Stable (0) and Beta (1) exist; anything else falls back to stable.
        var channel = value == 1 ? ChannelResolver.Beta : ChannelResolver.Stable;
        try { SettingsService.Current.Channel = channel; } catch { }
        try { UpdateService.Current.SetChannel(channel); } catch { }
#endif
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
#if (tray)
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
#endif
    }
}
