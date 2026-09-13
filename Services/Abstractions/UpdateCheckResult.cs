namespace DevTemWinUi3.Services;

/// <summary>
/// Outcome of an update check. Carries the version as a plain string so
/// consumers (ViewModels, tests) never touch Velopack types.
/// </summary>
public sealed record UpdateCheckResult(bool HasUpdate, string? Version);
