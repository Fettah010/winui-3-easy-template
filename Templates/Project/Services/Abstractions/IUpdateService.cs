using System;
using System.Threading.Tasks;

namespace DevTemWinUi3.Services;

/// <summary>
/// Update-check seam: the ViewModel programs against this, never Velopack
/// types, so the whole flow is unit-testable with a fake. The real
/// <see cref="UpdateService"/> holds the pending update internally.
/// Unconditional (compiles in every template combo); only the registration
/// is feature-gated, so consumers must tolerate <c>null</c>.
/// </summary>
public interface IUpdateService
{
    bool IsInstalled { get; }

    bool HasPendingUpdate { get; }

    /// <summary>
    /// Version of a downloaded-but-unapplied update from a previous
    /// session, if any (null when nothing waits). Lets startup offer a
    /// visible restart prompt instead of applying silently before the
    /// window appears. Backends without cross-session state return null.
    /// Never throws.
    /// </summary>
    string? PendingRestartVersion { get; }

    void SetChannel(string channel);

    Task<UpdateCheckResult> CheckAsync();

    Task DownloadPendingUpdateAsync(Action<int>? progress = null);

    void ApplyPendingUpdateAndRestart();
}
