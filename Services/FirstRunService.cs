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
    /// Release notes shown in the what's-new dialog. Capped so a long
    /// release cannot flood the modal; the full changelog lives one click
    /// away (releases page, resolved from product config at runtime).
    /// Pure apart from store/loc reads; never throws.
    /// </summary>
    public const int MaxChangelogLength = 1200;

    /// <summary>
    /// Gets the changelog text for the current version. Keep in sync with
    /// CHANGELOG.md highlights — this is what updating users actually read.
    /// At most once per version: the caller marks the version shown, so a
    /// second launch finds <see cref="HasBeenUpdated"/> false.
    /// </summary>
    public string GetChangelog()
    {
        try
        {
            string body = @"• Updates never block launch: the app opens first, then asks
• New animated update popup with progress + restart options
• Diagnostics Clear and live views fixed
• Same features, smoother and more honest";
            if (body.Length > MaxChangelogLength)
                body = body.Substring(0, MaxChangelogLength).TrimEnd() + "…";
            string repo = AppMetadata.RepoUrl.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(repo))
                return body;
            string link = LocalizationService.Current.GetString(
                "WhatsNewFullChangelog", repo + "/releases");
            if (string.IsNullOrWhiteSpace(link))
                return body;
            return body + "\n\n" + link;
        }
        catch
        {
            return string.Empty;
        }
    }
}
