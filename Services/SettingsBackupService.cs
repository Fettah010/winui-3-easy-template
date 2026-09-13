using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DevTemWinUi3.Services;

/// <summary>
/// Reset / JSON export / JSON import for user preferences (theme, channel,
/// tray, auto-check, language). Coordinates <see cref="SettingsService"/> and
/// <see cref="LocalizationService"/> so callers have one entry point.
/// Export files are plain JSON (<c>.json</c>); import tolerates missing keys
/// (keeps current values) and normalizes out-of-range ones, but rejects
/// unreadable files outright. Never throws.
/// </summary>
public static class SettingsBackupService
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    /// <summary>Resets preferences + language to defaults.</summary>
    public static void ResetAll()
    {
        try { SettingsService.Current.ResetToDefaults(); } catch { }
        try { LocalizationService.Current.ResetLanguage(); } catch { }
        try { LoggingService.Log.Information("Settings reset to defaults"); } catch { }
    }

    /// <summary>Current user preferences as a JSON-serializable snapshot.</summary>
    public static Dictionary<string, object?> Capture()
    {
        var snapshot = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var settings = SettingsService.Current;
            snapshot["theme"] = settings.Theme;
            snapshot["channel"] = settings.Channel;
            snapshot["minimizeToTray"] = settings.MinimizeToTray;
            snapshot["autoCheck"] = settings.AutoCheck;
            snapshot["language"] = LocalizationService.Current.CurrentLanguage;
        }
        catch { }
        return snapshot;
    }

    /// <summary>
    /// Writes the current preferences to <paramref name="path"/>.
    /// Returns false when the file cannot be written.
    /// </summary>
    public static bool ExportToFile(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(Capture(), s_jsonOptions);
            File.WriteAllText(path, json);
            LoggingService.Log.Information("Settings exported to {Path}", path);
            return true;
        }
        catch (Exception ex)
        {
            LoggingService.Log.Error(ex, "Settings export failed");
            return false;
        }
    }

    /// <summary>
    /// Reads preferences from <paramref name="path"/> and applies the valid
    /// ones. Unknown keys are ignored; invalid values fall back to defaults.
    /// Returns false when the file is missing or unparseable (nothing applied).
    /// </summary>
    public static bool ImportFromFile(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return false;
            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (data is null)
                return false;

            var settings = SettingsService.Current;
            if (data.TryGetValue("theme", out var theme) && theme.ValueKind == JsonValueKind.String)
                settings.Theme = ThemeService.Normalize(theme.GetString());

            if (data.TryGetValue("channel", out var channel) && channel.ValueKind == JsonValueKind.String)
                settings.Channel = ChannelResolver.Normalize(channel.GetString());

            if (TryGetBool(data, "minimizeToTray", out bool tray))
                settings.MinimizeToTray = tray;

            if (TryGetBool(data, "autoCheck", out bool autoCheck))
                settings.AutoCheck = autoCheck;

            if (data.TryGetValue("language", out var language) && language.ValueKind == JsonValueKind.String)
            {
                var tag = language.GetString();
                if (!string.IsNullOrEmpty(tag) &&
                    LocalizationService.AvailableLanguages.Any(l => l.Tag == tag))
                    LocalizationService.Current.SetLanguage(tag);
            }

            LoggingService.Log.Information("Settings imported from {Path}", path);
            return true;
        }
        catch (Exception ex)
        {
            LoggingService.Log.Error(ex, "Settings import failed");
            return false;
        }
    }

    private static bool TryGetBool(Dictionary<string, JsonElement> data, string key, out bool value)
    {
        value = false;
        try
        {
            if (!data.TryGetValue(key, out var element))
                return false;
            if (element.ValueKind == JsonValueKind.True)
            {
                value = true;
                return true;
            }
            if (element.ValueKind == JsonValueKind.False)
            {
                value = false;
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }
}
