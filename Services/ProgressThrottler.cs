using System;

namespace DevTemWinUi3.Services;

/// <summary>
/// Coalesces download-progress callbacks before they reach the UI thread
/// (performance plan P1-2): Velopack can deliver percent updates far faster
/// than a <c>ProgressBar</c> can render them. A report is forwarded when it
/// advances by at least <c>minDeltaPercent</c>, when
/// <c>minInterval</c> has passed since the last forward, or when it
/// completes (100). Pure and headless-testable; both update view models
/// share one instance per download.
/// </summary>
public sealed class ProgressThrottler
{
    private readonly int _minDeltaPercent;
    private readonly TimeSpan _minInterval;
    private readonly object _gate = new();
    private int _lastReported = -1;
    private DateTimeOffset _lastTime = DateTimeOffset.MinValue;

    public ProgressThrottler(int minDeltaPercent = 2, TimeSpan? minInterval = null)
    {
        _minDeltaPercent = minDeltaPercent <= 0 ? 2 : minDeltaPercent;
        _minInterval = minInterval ?? TimeSpan.FromMilliseconds(100);
    }

    /// <summary>
    /// Resets the burst state (call when a new download starts). Never throws.
    /// </summary>
    public void Reset()
    {
        try
        {
            lock (_gate)
            {
                _lastReported = -1;
                _lastTime = DateTimeOffset.MinValue;
            }
        }
        catch { }
    }

    /// <summary>
    /// Whether <paramref name="percent"/> should be marshalled to the UI.
    /// Never throws.
    /// </summary>
    public bool ShouldReport(int percent, DateTimeOffset now)
    {
        try
        {
            if (percent < 0)
                percent = 0;
            if (percent > 100)
                percent = 100;
            lock (_gate)
            {
                if (_lastReported < 0)
                {
                    _lastReported = percent;
                    _lastTime = now;
                    return true;
                }
                if (percent >= 100)
                {
                    // Completion always shows (even a duplicate 100 only
                    // forwards once per advance: the delta/interval rules
                    // below already covered the first 100).
                    if (_lastReported >= 100)
                        return false;
                    _lastReported = percent;
                    _lastTime = now;
                    return true;
                }
                if (percent - _lastReported >= _minDeltaPercent)
                {
                    _lastReported = percent;
                    _lastTime = now;
                    return true;
                }
                if (now - _lastTime >= _minInterval)
                {
                    _lastReported = percent;
                    _lastTime = now;
                    return true;
                }
                return false;
            }
        }
        catch
        {
            return true;
        }
    }
}
