namespace DevTemWinUi3.Services;

/// <summary>
/// Single source of truth for the app's identity. When reusing this template,
/// change the values here AND run <c>Scripts/init-template.ps1</c>, which
/// rewrites the text/XAML surfaces (names, URLs) that cannot read code.
/// Everything process-wide unique (mutex, registry, settings folder) derives
/// from here so two apps built from this template can never collide.
/// </summary>
public static class AppMetadata
{
    /// <summary>Display name: titles, toasts, installer.</summary>
    public const string AppName = "DevTem-WinUI 3";

    /// <summary>Identifier-safe name: mutexes, registry, folders.</summary>
    public const string SafeName = "DevTemWinUi3";

    /// <summary>Pack author / company.</summary>
    public const string Company = "Fettah";

    /// <summary>
    /// GitHub repo hosting releases and links. Resolved from
    /// <c>ProductConfiguration.RepoUrl</c> (rewritten at scaffold/rebrand
    /// time) with <c>DEVTEM_REPO_URL</c> override support, so XAML surfaces
    /// never hardcode the template repo. Falls back to the template default
    /// when configuration is empty.
    /// </summary>
    public static string RepoUrl
    {
        get
        {
            try
            {
                string configured = Configuration.DeploymentConfiguration.RepoUrl;
                if (!string.IsNullOrWhiteSpace(configured))
                    return configured;
            }
            catch { }
            return "https://github.com/Fettah010/winui-3-easy-template";
        }
    }

    /// <summary>License display name (from ProductConfiguration).</summary>
    public static string LicenseName
    {
        get
        {
            try
            {
                string configured = Configuration.DeploymentConfiguration.LicenseName;
                if (!string.IsNullOrWhiteSpace(configured))
                    return configured;
            }
            catch { }
            return "MIT License";
        }
    }

    /// <summary>License URL (from ProductConfiguration, derived when empty).</summary>
    public static string LicenseUrl
    {
        get
        {
            try { return Configuration.DeploymentConfiguration.LicenseUrl; } catch { }
            return "https://github.com/Fettah010/winui-3-easy-template/blob/main/LICENSE";
        }
    }

    /// <summary>HTTP user agent.</summary>
    public static string UserAgent => $"{SafeName}/{AppInfo.Current.Version}";

    /// <summary>Single-instance mutex/event name prefix.</summary>
    public static string SingleInstanceMutexName => $"Global\\{SafeName}_SingleInstance_Mutex";

    /// <summary>Single-instance activation event name.</summary>
    public static string SingleInstanceEventName => $"Global\\{SafeName}_Activate_Event";

    /// <summary>HKCU\...\Run value name for auto-start.</summary>
    public const string AutoStartRegistryName = "DevTemWinUi3";

    /// <summary>%LocalAppData% folder for settings.</summary>
    public const string AppDataFolder = "DevTemWinUi3";

    /// <summary>Tray tooltip.</summary>
    public static string TrayTooltip => AppName;

    // NOTE: spelled WITH "://" on purpose — both rename engines key on the
    // "devtem://" literal (dotnet-new `scheme` symbol, init-template.ps1),
    // so renamed apps get their own scheme with no extra rules.
    private const string ProtocolPrefix = "devtem://";

    /// <summary>Deep-link URI scheme without "://" (e.g. "devtem").</summary>
    public static string ProtocolScheme => ProtocolPrefix.TrimEnd('/', ':');

    /// <summary>
    /// Sentry DSN for crash reporting. Empty (default) disables it entirely:
    /// <see cref="CrashReportingService"/> becomes a no-op and no Sentry
    /// package traffic ever happens. Paste a DSN to enable.
    /// </summary>
    public const string SentryDsn = Configuration.ProductConfiguration.SentryDsn;

    /// <summary>Optional Sentry release override; empty uses the assembly version.</summary>
    public const string SentryRelease = Configuration.ProductConfiguration.SentryRelease;

    /// <summary>Optional Sentry environment override; empty derives beta/production.</summary>
    public const string SentryEnvironment = Configuration.ProductConfiguration.SentryEnvironment;
}
