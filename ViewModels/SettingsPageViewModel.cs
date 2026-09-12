using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevTemWinUi3.Services;
using Microsoft.UI.Xaml;

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
            var channel = SettingsService.Current.Channel;
            SelectedChannelIndex = channel switch
            {
                "beta" => 1,
                "dev" => 2,
                _ => 0
            };
        }
        catch { SelectedChannelIndex = 0; }

        try { AutoCheckOnStartup = SettingsService.Current.AutoCheck; }
        catch { AutoCheckOnStartup = true; }

        try { MinimizeToTray = SettingsService.Current.MinimizeToTray; }
        catch { MinimizeToTray = true; }

        AutoStart = SystemTrayService.IsAutoStartEnabled();

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

        try { SettingsService.Current.Theme = theme; } catch { }

        try
        {
            if (App.Current is App app && app.m_window is MainWindow mainWindow)
            {
                var rootElement = mainWindow.Content as FrameworkElement;
                rootElement?.DispatcherQueue.TryEnqueue(() =>
                {
                    if (rootElement is not null)
                    {
                        rootElement.RequestedTheme = value switch
                        {
                            1 => ElementTheme.Light,
                            2 => ElementTheme.Dark,
                            _ => ElementTheme.Default
                        };
                    }
                });
            }
        }
        catch { }
    }

    partial void OnSelectedChannelIndexChanged(int value)
    {
        var channel = value switch
        {
            1 => "beta",
            2 => "dev",
            _ => "stable"
        };
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
