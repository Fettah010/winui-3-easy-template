using System;

namespace DevTemWinUi3.Services;

/// <summary>
/// Navigation request raised by the system-tray context menu.
/// Carries the target route plus whether the Settings page should
/// automatically run an update check once it opens.
/// </summary>
public sealed record TrayNavigationRequest(string Target, bool AutoCheckUpdates = false)
{
    /// <summary>
    /// Navigation parameter passed to the Settings page to trigger
    /// an automatic update check in <c>OnNavigatedTo</c>.
    /// </summary>
    public const string CheckUpdatesParameter = "check-updates";

    /// <summary>
    /// Determines whether the given navigation parameter should trigger
    /// an automatic update check. Accepts the <see cref="CheckUpdatesParameter"/>
    /// string (case-insensitive), <c>true</c>, or a <see cref="TrayNavigationRequest"/>
    /// with <see cref="AutoCheckUpdates"/> set.
    /// </summary>
    public static bool ShouldAutoCheck(object? parameter) =>
        parameter switch
        {
            TrayNavigationRequest req => req.AutoCheckUpdates,
            bool b => b,
            string s => string.Equals(s, CheckUpdatesParameter, StringComparison.OrdinalIgnoreCase),
            _ => false,
        };

    /// <summary>
    /// Builds the navigation parameter for the given request, or null
    /// when no auto-check is needed.
    /// </summary>
    public object? ToParameter() =>
        AutoCheckUpdates ? CheckUpdatesParameter : null;
}
