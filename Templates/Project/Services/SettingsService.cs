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
    private const string KeyVerboseLogging = "VerboseLogging";
    private const string KeyCrashReports = "CrashReportsEnabled";
    private const string KeyChannelMigratedFor = "ChannelMigratedFor";
    private const string KeyFastLaunch = "FastLaunch";
    private const string KeyDownloadOnMetered = "DownloadOnMetered";

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
    /// Update channel: "stable" or "beta". Fresh installs default to the
    /// build's own channel (beta builds track beta); a saved choice always
    /// wins. Legacy "dev" values normalize to "beta".
    /// </summary>
    public string Channel
    {
        get => ChannelResolver.Normalize(Store.Get(KeyChannel, AppInfo.Current.DefaultChannel));
        set => Store.Set(KeyChannel, ChannelResolver.Normalize(value));
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
    /// One-time per-version channel migration, called at startup. A beta build
    /// holding a non-beta channel (e.g. upgraded from a stable default) would
    /// otherwise never see beta updates, so it is moved to beta once. Fresh
    /// installs need nothing (the default is already build-aware), and a user
    /// choice made after migration is left alone until the next version.
    /// </summary>
    public void EnsureChannelForCurrentBuild()
    {
        try
        {
            var version = AppInfo.Current.Version;
            if (Store.Get(KeyChannelMigratedFor, string.Empty) == version)
                return;
            Store.Set(KeyChannelMigratedFor, version);

            if (!AppInfo.Current.IsBetaBuild)
                return;
            if (Channel != ChannelResolver.Beta)
                Channel = ChannelResolver.Beta;
        }
        catch { }
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

    /// <summary>
    /// Whether extra debug detail is logged (Serilog Verbose level).
    /// Off by default in release builds; on in Debug builds so the dev
    /// loop captures everything without a toggle round-trip.
    /// </summary>
    public bool VerboseLogging
    {
        get => Store.Get(KeyVerboseLogging,
#if DEBUG
            true
#else
            false
#endif
        );
        set => Store.Set(KeyVerboseLogging, value);
    }

    /// <summary>
    /// Whether the app may send crash reports (Sentry). Explicit opt-in,
    /// off by default; a configured DSN alone never enables sending.
    /// </summary>
    public bool CrashReportsEnabled
    {
        get => Store.Get(KeyCrashReports, false);
        set => Store.Set(KeyCrashReports, value);
    }

    /// <summary>
    /// Opt-in fast launch (performance plan P0-1): entrance/transition
    /// animations collapse to ~1 ms. Off by default; the visual language
    /// is unchanged otherwise. <c>DEVTEM_FAST_LAUNCH=1</c> overrides this
    /// without persisting.
    /// </summary>
    public bool FastLaunch
    {
        get => Store.Get(KeyFastLaunch, false);
        set => Store.Set(KeyFastLaunch, value);
    }

    /// <summary>
    /// Whether background update downloads may run on metered networks.
    /// Off by default (checks still run; only the download is skipped).
    /// </summary>
    public bool DownloadOnMetered
    {
        get => Store.Get(KeyDownloadOnMetered, false);
        set => Store.Set(KeyDownloadOnMetered, value);
    }

    /// <summary>
    /// Removes every user preference (theme, channel, tray, auto-check,
    /// verbose logging, crash-report consent, fast launch, metered
    /// downloads, pending-update state) so the
    /// next read returns defaults. The per-version channel-migration marker
    /// is kept: it is bookkeeping, not a preference. Language lives in
    /// <see cref="LocalizationService"/> and is reset separately
    /// (<see cref="SettingsBackupService.ResetAll"/>).
    /// </summary>
    public void ResetToDefaults()
    {
        try
        {
            Store.Remove(KeyTheme);
            Store.Remove(KeyChannel);
            Store.Remove(KeyMinimizeToTray);
            Store.Remove(KeyAutoCheck);
            Store.Remove(KeyVerboseLogging);
            Store.Remove(KeyCrashReports);
            Store.Remove(KeyFastLaunch);
            Store.Remove(KeyDownloadOnMetered);
            Store.Remove(KeyLastCheckTime);
            Store.Remove(KeyPendingVersion);
        }
        catch { }
    }
}
