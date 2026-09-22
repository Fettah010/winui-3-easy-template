using System;
using System.Collections.Generic;

namespace DevTemWinUi3.Services;

/// <summary>
/// Validated-options layer over the bespoke JSON store (Advancement C4).
/// Every persisted preference gets three things: a default (declared at
/// the <c>SettingsService</c> property), validation (normalize on
/// read + write so stored garbage can never stick), and a schema version
/// (migrations below). Deliberately thin: no
/// <c>Microsoft.Extensions.Options</c> (C3 kept the host out). Never
/// throws; a corrupt store reads as defaults, never as an exception.
/// </summary>
public static class SettingsSchema
{
    /// <summary>Current live-store schema version. Bump with each migration.</summary>
    public const int CurrentVersion = 1;

    internal const string KeySchemaVersion = "SettingsSchemaVersion";
    internal const string KeyChannel = "UpdateChannel";

    private static readonly Dictionary<int, Action> _migrations = new()
    {
        // v1: stamp the version; rewrite raw legacy "dev" channels to
        // "beta" (reads already normalize, but the stored raw value
        // would otherwise live forever).
        [1] = () =>
        {
            try
            {
                var store = LocalSettingsStore.Shared;
                if (string.Equals(store.Get(KeyChannel, string.Empty), "dev", StringComparison.OrdinalIgnoreCase))
                    store.Set(KeyChannel, ChannelResolver.Beta);
            }
            catch { }
        },
    };

    /// <summary>
    /// Runs every pending migration in version order, then stamps the
    /// current version. Stores from the future (higher version) are left
    /// untouched — never migrate down. Idempotent. Never throws.
    /// </summary>
    public static void EnsureMigrated()
    {
        try
        {
            var store = LocalSettingsStore.Shared;
            int stored;
            try { stored = store.Get(KeySchemaVersion, 0); }
            catch { stored = 0; }
            if (stored >= CurrentVersion)
                return;
            for (int version = stored + 1; version <= CurrentVersion; version++)
            {
                try
                {
                    if (_migrations.TryGetValue(version, out var migrate))
                        migrate();
                }
                catch { }
            }
            try { store.Set(KeySchemaVersion, CurrentVersion); }
            catch { }
        }
        catch { }
    }

    /// <summary>
    /// Normalizes any value to a real theme (unknown honors system).
    /// Applied on read + write so legacy garbage self-heals.
    /// </summary>
    public static string NormalizeTheme(string? theme) => theme switch
    {
        "Light" => "Light",
        "Dark" => "Dark",
        _ => "System",
    };
}
