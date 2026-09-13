using System;

namespace DevTemWinUi3.Services;

/// <summary>
/// Channel routing for updates. Only <c>stable</c> and <c>beta</c> exist (the
/// legacy <c>dev</c> value never had a feed and maps to <c>beta</c>).
/// Fresh installs default to the build's own channel: beta builds track beta,
/// everything else tracks stable. A persisted choice always wins.
/// </summary>
public static class ChannelResolver
{
    public const string Stable = "stable";
    public const string Beta = "beta";

    /// <summary>Legacy value without a feed; kept only for migration.</summary>
    private const string LegacyDev = "dev";

    /// <summary>
    /// Whether the given informational version (e.g. "0.0.2-beta") is a beta build.
    /// </summary>
    public static bool IsBetaVersion(string? informationalVersion) =>
        informationalVersion?.Contains(Beta, StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>Normalizes any stored value to a real channel.</summary>
    public static string Normalize(string? channel) =>
        string.Equals(channel, Beta, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(channel, LegacyDev, StringComparison.OrdinalIgnoreCase)
            ? Beta
            : Stable;

    /// <summary>
    /// Resolves the effective channel: persisted choice first, otherwise the
    /// default for the current build.
    /// </summary>
    public static string ResolveDefaultChannel(string? persistedChannel, string? informationalVersion) =>
        string.IsNullOrEmpty(persistedChannel)
            ? (IsBetaVersion(informationalVersion) ? Beta : Stable)
            : Normalize(persistedChannel);
}
