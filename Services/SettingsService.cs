using System;

namespace DevTemWinUi3.Services;

/// <summary>
/// User settings backed by <see cref="LocalSettingsStore"/> (JSON file).
/// Previously used ApplicationData.LocalSettings, which never persisted for
/// unpackaged apps — every setting silently reverted to its default.
/// </summary>
public sealed class SettingsService
{
    private const string KeyTheme = "AppTheme";
    private const string KeyChannel = "UpdateChannel";
    private const string KeyLastCheckTime = "LastUpdateCheckTime";
    private const string KeyPendingVersion = "PendingUpdateVersion";
    private const string KeyMinimizeToTray = "MinimizeToTray";
    private const string KeyAutoCheck = "AutoCheckOnStartup";

    public static SettingsService Current { get; } = new();

    private SettingsService()
    {
    }

    private static LocalSettingsStore Store => LocalSettingsStore.Shared;

    /// <summary>
    /// App theme: "System", "Light", or "Dark". Default is "System".
    /// </summary>
    public string Theme
    {
        get => Store.Get(KeyTheme, "System");
        set => Store.Set(KeyTheme, value);
    }

    /// <summary>
    /// Update channel: "stable", "beta", or "dev". Default is "stable".
    /// </summary>
    public string Channel
    {
        get => Store.Get(KeyChannel, "stable");
        set => Store.Set(KeyChannel, value);
    }

    /// <summary>
    /// Timestamp of the last auto-update check (UTC). Null if never checked.
    /// </summary>
    public DateTimeOffset? LastCheckTime
    {
        get
        {
            long ticks = Store.Get(KeyLastCheckTime, 0L);
            return ticks == 0 ? null : new DateTimeOffset(ticks, TimeSpan.Zero);
        }
        set
        {
            if (value.HasValue)
                Store.Set(KeyLastCheckTime, value.Value.UtcTicks);
            else
                Store.Remove(KeyLastCheckTime);
        }
    }

    /// <summary>
    /// Version string of a downloaded-but-not-installed update. Null when no
    /// pending update is stored.
    /// </summary>
    public string? PendingVersion
    {
        get => Store.Get<string?>(KeyPendingVersion, null);
        set
        {
            if (value is not null)
                Store.Set(KeyPendingVersion, value);
            else
                Store.Remove(KeyPendingVersion);
        }
    }

    /// <summary>
    /// Whether the app minimizes to the system tray on close. Default is true.
    /// </summary>
    public bool MinimizeToTray
    {
        get => Store.Get(KeyMinimizeToTray, true);
        set => Store.Set(KeyMinimizeToTray, value);
    }

    /// <summary>
    /// Whether the app checks for updates automatically on startup.
    /// Default is true.
    /// </summary>
    public bool AutoCheck
    {
        get => Store.Get(KeyAutoCheck, true);
        set => Store.Set(KeyAutoCheck, value);
    }
}
