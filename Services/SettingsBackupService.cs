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
/// unreadable files outright. Imports are capped at 1 MB (settings are
/// ~1 KB) and carry a schema <c>version</c>: unknown future MAJOR versions
/// are refused, unknown keys are ignored. Never throws.
/// </summary>
public static class SettingsBackupService
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    /// <summary>Backup schema version written by <see cref="Capture"/>.</summary>
    internal const int SchemaVersion = 1;

    /// <summary>
    /// Largest import honored. Settings snapshots are ~1 KB; anything near
    /// this cap is not a settings file (a multi-GB file would OOM the parse).
    /// </summary>
    internal const long MaxImportBytes = 1024 * 1024;

    /// <summary>Resets preferences + language to defaults.</summary>
    public static void ResetAll()
    {
        try { SettingsService.Current.ResetToDefaults(); } catch { }
        try { LocalizationService.Current.ResetLanguage(); } catch { }
        try { CrashReportingService.AddBreadcrumb("Settings reset", "settings"); } catch { }
        try { AppLog.Information("Settings reset to defaults"); } catch { }
    }

    /// <summary>Current user preferences as a JSON-serializable snapshot.</summary>
    public static Dictionary<string, object?> Capture()
    {
        var snapshot = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var settings = SettingsService.Current;
            snapshot["version"] = SchemaVersion;
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
            try { CrashReportingService.AddBreadcrumb("Settings exported", "settings"); } catch { }
            AppLog.Information("Settings exported to {Path}", path);
            return true;
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Settings export failed");
            return false;
        }
    }

    /// <summary>
    /// Reads preferences from <paramref name="path"/> and applies the valid
    /// ones. Unknown keys are ignored; invalid values fall back to defaults.
    /// Returns false when the file is missing, oversized, unparseable, or
    /// from an unknown future schema MAJOR (nothing applied).
    /// </summary>
    public static bool ImportFromFile(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return false;
            if (new FileInfo(path).Length > MaxImportBytes)
            {
                AppLog.Warning("Settings import refused: file exceeds {Max} bytes", MaxImportBytes);
                return false;
            }

            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (data is null)
                return false;
            if (!CheckSchemaVersion(data))
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

            AppLog.Information("Settings imported from {Path}", path);
            try { CrashReportingService.AddBreadcrumb("Settings imported", "settings"); } catch { }
            return true;
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Settings import failed");
            return false;
        }
    }

    /// <summary>
    /// Schema gate: absent version means a pre-versioned export (accepted);
    /// a higher MAJOR is refused (forward-incompatible); same or lower
    /// applies tolerant-per-key as before. Pure apart from the warning log.
    /// </summary>
    internal static bool CheckSchemaVersion(Dictionary<string, JsonElement> data)
    {
        try
        {
            if (!data.TryGetValue("version", out var element))
                return true;
            int major = 0;
            bool parsed = element.ValueKind == JsonValueKind.Number
                ? element.TryGetInt32(out major)
                : element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out major);
            if (!parsed)
                return false;

            if (major > SchemaVersion)
            {
                AppLog.Warning("Settings import refused: schema v{Version} is newer than v{Supported}", major, SchemaVersion);
                return false;
            }

            return major >= 1;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryGetBool(Dictionary<string, JsonElement> data, string key, out bool value)    {
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
