using System;
using Windows.Storage;

namespace DevTemWinUi3.Services;

/// <summary>
/// Persists user settings via Windows.Storage.ApplicationData (roaming).
/// Stores theme preference, update channel, and last update-check metadata.
/// </summary>
public sealed class SettingsService
{
    private static ApplicationDataContainer? _localSettings;

    private const string KeyTheme = "AppTheme";
    private const string KeyChannel = "UpdateChannel";
    private const string KeyLastCheckTime = "LastUpdateCheckTime";
    private const string KeyPendingVersion = "PendingUpdateVersion";

    public static SettingsService Current { get; } = new();

    private SettingsService()
    {
    }

    private static ApplicationDataContainer LocalSettings
    {
        get
        {
            if (_localSettings is null)
                _localSettings = ApplicationData.Current.LocalSettings;
            return _localSettings;
        }
    }

    /// <summary>
    /// App theme: "System", "Light", or "Dark". Default is "System".
    /// </summary>
    public string Theme
    {
        get => ReadString(KeyTheme, "System");
        set => WriteString(KeyTheme, value);
    }

    /// <summary>
    /// Update channel: "stable", "beta", or "dev". Default is "stable".
    /// </summary>
    public string Channel
    {
        get => ReadString(KeyChannel, "stable");
        set => WriteString(KeyChannel, value);
    }

    /// <summary>
    /// Timestamp of the last auto-update check (UTC). Null if never checked.
    /// </summary>
    public DateTimeOffset? LastCheckTime
    {
        get
        {
            try
            {
                if (LocalSettings.Values.TryGetValue(KeyLastCheckTime, out var obj) && obj is long ticks)
                    return new DateTimeOffset(ticks, TimeSpan.Zero);
            }
            catch { }
            return null;
        }
        set
        {
            try
            {
                if (value.HasValue)
                    LocalSettings.Values[KeyLastCheckTime] = value.Value.UtcTicks;
                else
                    LocalSettings.Values.Remove(KeyLastCheckTime);
            }
            catch { }
        }
    }

    /// <summary>
    /// Version string of a downloaded-but-not-installed update. Null when no
    /// pending update is stored.
    /// </summary>
    public string? PendingVersion
    {
        get => ReadString(KeyPendingVersion, null);
        set
        {
            if (value is not null)
                WriteString(KeyPendingVersion, value);
            else
            {
                try { LocalSettings.Values.Remove(KeyPendingVersion); } catch { }
            }
        }
    }

    private string ReadString(string key, string? defaultValue)
    {
        try
        {
            if (LocalSettings.Values.TryGetValue(key, out var obj) && obj is string s)
                return s;
        }
        catch { }
        return defaultValue ?? string.Empty;
    }

    private void WriteString(string key, string value)
    {
        try { LocalSettings.Values[key] = value; } catch { }
    }
}
