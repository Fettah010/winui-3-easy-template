namespace DevTemWinUi3.Services.Configuration;

/// <summary>
/// Generated product and deployment defaults. Keep secrets out of this file;
/// local or CI-only values belong in environment variables.
/// </summary>
public static class ProductConfiguration
{
    public const string PrimaryColor = "#2D6CDF";
    public const string AccentColor = "#2D6CDF";
    public const string SupportUrl = "";
    public const string PrivacyUrl = "";
    public const string UpdateFeedUrl = "";
    public const string SentryDsn = "";
    public const string SentryEnvironment = "";
    public const string SentryRelease = "";
}

/// <summary>
/// Resolves deployment overrides without requiring a configuration package.
/// Environment values are developer/CI-owned and never committed.
/// </summary>
public static class DeploymentConfiguration
{
    public static string SupportUrl => Read("DEVTEM_SUPPORT_URL", ProductConfiguration.SupportUrl);
    public static string PrivacyUrl => Read("DEVTEM_PRIVACY_URL", ProductConfiguration.PrivacyUrl);
    public static string UpdateFeedUrl => Read("DEVTEM_UPDATE_FEED_URL", ProductConfiguration.UpdateFeedUrl);
    public static string SentryDsn => Read("DEVTEM_SENTRY_DSN", ProductConfiguration.SentryDsn);
    public static string SentryEnvironment => Read("DEVTEM_SENTRY_ENVIRONMENT", ProductConfiguration.SentryEnvironment);
    public static string SentryRelease => Read("DEVTEM_SENTRY_RELEASE", ProductConfiguration.SentryRelease);

    private static string Read(string name, string fallback)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
