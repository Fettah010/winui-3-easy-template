namespace DevTemWinUi3.Services;

/// <summary>
/// Detects first run and tracks shown versions for What's New dialog.
/// Backed by <see cref="LocalSettingsStore"/> (JSON file).
/// </summary>
public sealed class FirstRunService
{
    private const string KeyLastShownVersion = "LastShownVersion";
    private const string KeyHasRunBefore = "HasRunBefore";

    public static FirstRunService Current { get; } = new();

    private FirstRunService()
    {
    }

    private static LocalSettingsStore Store => LocalSettingsStore.Shared;

    /// <summary>
    /// Whether this is the first time the app has run.
    /// </summary>
    public bool IsFirstRun => !Store.Get(KeyHasRunBefore, false);

    /// <summary>
    /// The last version that was shown to the user.
    /// </summary>
    public string LastShownVersion => Store.Get(KeyLastShownVersion, string.Empty);

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
        Store.Set(KeyHasRunBefore, true);
        Store.Set(KeyLastShownVersion, AppInfo.Current.Version);
    }

    /// <summary>
    /// Gets the changelog text for the current version. Keep in sync with
    /// CHANGELOG.md highlights — this is what updating users actually read.
    /// </summary>
    public string GetChangelog()
    {
        return $@"What's New in v{AppInfo.Current.Version}

• Restart and update prompts fully translated
• Same features, cleaner and faster internals
• Bug fixes and performance improvements";
    }
}
