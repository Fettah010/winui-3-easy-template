using System.Threading;

namespace DevTemWinUi3.Services.Helpers;

/// <summary>
/// Explicit repaint signal for mutation flows (pain-log #25): after
/// merge/rotate/delete, page/count/zoom often do not change, so
/// property-change-driven repaints never fire and the old bitmap stays.
/// Bump <see cref="Next"/> on every content transition and have the view
/// watch <see cref="Current"/> — change notification is not change
/// detection. Thread-safe; wraps without going negative.
/// </summary>
public sealed class ChangeEpoch
{
    private long _current;

    /// <summary>Current generation. Views watch this.</summary>
    public long Current => Interlocked.Read(ref _current);

    /// <summary>Bumps the generation and returns the new value.</summary>
    public long Next() => Interlocked.Increment(ref _current);
}
