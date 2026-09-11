using System;
using Windows.Storage;

namespace DevTemWinUi3.Services;

/// <summary>
/// Detects first run and tracks shown versions for What's New dialog.
/// </summary>
public sealed class FirstRunService
{
    private const string KeyLastShownVersion = "LastShownVersion";
    private const string KeyHasRunBefore = "HasRunBefore";

    public static FirstRunService Current { get; } = new();

    private ApplicationDataContainer? _localSettings;
    private bool _initialized;

    private bool TryInit()
    {
        if (_initialized) return _localSettings != null;
        _initialized = true;
        try
        {
            _localSettings = ApplicationData.Current.LocalSettings;
        }
        catch
        {
            // Unpackaged or pre-runtime context — gracefully degrade
        }
        return _localSettings != null;
    }

    private FirstRunService()
    {
    }

    /// <summary>
    /// Whether this is the first time the app has run.
    /// </summary>
    public bool IsFirstRun
    {
        get
        {
            if (!TryInit()) return false;
            try
            {
                if (_localSettings!.Values.TryGetValue(KeyHasRunBefore, out var obj) && obj is bool b)
                    return !b;
                return true;
            }
            catch { return false; }
        }
    }

    /// <summary>
    /// The last version that was shown to the user.
    /// </summary>
    public string LastShownVersion
    {
        get
        {
            if (!TryInit()) return string.Empty;
            try
            {
                if (_localSettings!.Values.TryGetValue(KeyLastShownVersion, out var obj) && obj is string s)
                    return s;
            }
            catch { }
            return string.Empty;
        }
    }

    /// <summary>
    /// Whether the app has been updated since last run.
    /// </summary>
    public bool HasBeenUpdated
    {
        get
        {
            var current = AppInfo.Current.Version;
            var last = LastShownVersion;
            return !string.IsNullOrEmpty(last) && current != last;
        }
    }

    /// <summary>
    /// Marks the current version as shown.
    /// </summary>
    public void MarkAsShown()
    {
        if (!TryInit()) return;
        try
        {
            _localSettings!.Values[KeyHasRunBefore] = true;
            _localSettings.Values[KeyLastShownVersion] = AppInfo.Current.Version;
        }
        catch { }
    }

    /// <summary>
    /// Gets the changelog text for the current version.
    /// </summary>
    public string GetChangelog()
    {
        return $@"What's New in v{AppInfo.Current.Version}

• Settings page with theme selector
• Auto-update improvements
• Desktop shortcut support
• Bug fixes and performance improvements";
    }
}
