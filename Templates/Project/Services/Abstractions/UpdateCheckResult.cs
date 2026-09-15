namespace DevTemWinUi3.Services;

/// <summary>
/// Outcome of an update check. Carries the version as a plain string so
/// consumers (ViewModels, tests) never touch Velopack types.
/// <c>ReleaseNotes</c> carries the upstream notes verbatim (Velopack
/// <c>NotesMarkdown</c>, GitHub release body for basic) and is null when
/// the backend has none — the UI collapses the notes section then.
/// </summary>
public sealed record UpdateCheckResult(bool HasUpdate, string? Version, string? ReleaseNotes = null);
