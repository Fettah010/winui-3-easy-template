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
    private string _appVersion = string.Empty;

    public SettingsPageViewModel()
    {
        AppVersion = AppInfo.Current.Version;

        // Load persisted theme
        var theme = SettingsService.Current.Theme;
        SelectedThemeIndex = theme switch
        {
            "Light" => 1,
            "Dark" => 2,
            _ => 0
        };

        // Load persisted channel
        var channel = SettingsService.Current.Channel;
        SelectedChannelIndex = channel switch
        {
            "beta" => 1,
            "dev" => 2,
            _ => 0
        };

        AutoCheckOnStartup = true; // Default, could be persisted too
    }

    partial void OnSelectedThemeIndexChanged(int value)
    {
        var theme = value switch
        {
            1 => "Light",
            2 => "Dark",
            _ => "System"
        };
        SettingsService.Current.Theme = theme;

        // Apply theme to the app
        if (App.Current is App app && app.m_window is MainWindow mainWindow)
        {
            var rootElement = mainWindow.Content as FrameworkElement;
            if (rootElement is not null)
            {
                rootElement.RequestedTheme = value switch
                {
                    1 => ElementTheme.Light,
                    2 => ElementTheme.Dark,
                    _ => ElementTheme.Default
                };
            }
        }
    }

    partial void OnSelectedChannelIndexChanged(int value)
    {
        var channel = value switch
        {
            1 => "beta",
            2 => "dev",
            _ => "stable"
        };
        SettingsService.Current.Channel = channel;
        UpdateService.Current.SetChannel(channel);
    }
}
