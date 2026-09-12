using System.Reflection;

namespace DevTemWinUi3.Services;

public sealed class AppInfo
{
    public static AppInfo Current { get; } = new();

    public string Version =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

    public string VersionDisplay => $"Version {Version}";

    /// <summary>
    /// Full informational version (may carry a "-beta" suffix for beta builds).
    /// </summary>
    public string InformationalVersion =>
        Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? Version;

    /// <summary>Whether this build is a beta release.</summary>
    public bool IsBetaBuild => ChannelResolver.IsBetaVersion(InformationalVersion);

    /// <summary>Update channel fresh installs of this build should track.</summary>
    public string DefaultChannel => IsBetaBuild ? ChannelResolver.Beta : ChannelResolver.Stable;
}
