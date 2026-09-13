using System;
using System.IO;
using Microsoft.UI.Xaml;

namespace DevTemWinUi3.Services;

/// <summary>
/// Resolves the theme-appropriate window/tray icon.
/// <c>app-light.ico</c> / <c>app-dark.ico</c> are preferred when present;
/// <c>app.ico</c> is always the fallback so older trees (or a fresh
/// <c>set-app-icon.ps1</c> run that only wrote <c>app.ico</c>) keep working.
/// Pure file-name logic is unit-testable; existence checks stay at the edge.
/// </summary>
public static class AppIconService
{
    public const string DefaultIconFileName = "app.ico";
    public const string LightIconFileName = "app-light.ico";
    public const string DarkIconFileName = "app-dark.ico";

    /// <summary>Preferred file name for the given theme.</summary>
    public static string ResolveIconFileName(bool isDark) =>
        isDark ? DarkIconFileName : LightIconFileName;

    /// <summary>Whether an actual theme value counts as dark.</summary>
    public static bool IsDark(ElementTheme actualTheme) => actualTheme == ElementTheme.Dark;

    /// <summary>
    /// Full path to the theme icon under <c>&lt;baseDir&gt;/Assets</c>,
    /// falling back to <c>app.ico</c> when the variant is absent.
    /// Never throws; returns the fallback path on any error.
    /// </summary>
    public static string ResolveIconPath(string baseDirectory, bool isDark)
    {
        try
        {
            var assets = Path.Combine(baseDirectory, "Assets");
            var preferred = Path.Combine(assets, ResolveIconFileName(isDark));
            if (File.Exists(preferred))
                return preferred;
            return Path.Combine(assets, DefaultIconFileName);
        }
        catch
        {
            try
            {
                return Path.Combine(baseDirectory, "Assets", DefaultIconFileName);
            }
            catch
            {
                return DefaultIconFileName;
            }
        }
    }
}
