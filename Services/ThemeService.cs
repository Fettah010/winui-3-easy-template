using Microsoft.UI.Xaml;

namespace DevTemWinUi3.Services;

/// <summary>
/// Owns the app theme ("System"/"Light"/"Dark"): mapping, persistence, and
/// application to windows. Replaces the copy-pasted theme switches that used
/// to live in MainWindow, SplashScreen, and the settings ViewModel.
/// </summary>
public sealed class ThemeService
{
    public static ThemeService Current { get; } = new();

    private ThemeService() { }

    /// <summary>Pure mapping, unit-testable.</summary>
    public static ElementTheme ToElementTheme(string? theme) => theme switch
    {
        "Light" => ElementTheme.Light,
        "Dark" => ElementTheme.Dark,
        _ => ElementTheme.Default,
    };

    /// <summary>Normalizes any value to a real theme (unknown honors system).</summary>
    public static string Normalize(string? theme) => theme switch
    {
        "Light" => "Light",
        "Dark" => "Dark",
        _ => "System",
    };

    /// <summary>Persisted theme choice.</summary>
    public string CurrentTheme => SettingsService.Current.Theme;

    /// <summary>Persist a validated theme choice.</summary>
    public void SetTheme(string theme)
    {
        try { SettingsService.Current.Theme = Normalize(theme); } catch { }
    }

    /// <summary>Apply the persisted theme to a window root.</summary>
    public void ApplyTo(FrameworkElement root)
    {
        try { root.RequestedTheme = ToElementTheme(CurrentTheme); } catch { }
    }

    /// <summary>Apply the persisted theme to the main window (if present).</summary>
    public void ApplyToMainWindow()
    {
        try
        {
            if (App.Current is App app &&
                app.m_window is MainWindow mainWindow &&
                mainWindow.Content is FrameworkElement root)
            {
                ApplyTo(root);
            }
        }
        catch { }
    }
}
