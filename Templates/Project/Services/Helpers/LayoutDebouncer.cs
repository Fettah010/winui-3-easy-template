using System;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace DevTemWinUi3.Services;

/// <summary>
/// Coalesces rapid width updates (window resize drags) so breakpoint reflows
/// apply once the size settles instead of on every tick. (Pane toggles never
/// resize the content — the nav pane is an overlay — so this only sees real
/// window resizes.) Steady-state results are identical to applying immediately (same thresholds, same apply action) —
/// only the timing changes. A settled size in the SAME breakpoint is a pure
/// no-op: no Grid mutation, no opacity animation. A real breakpoint flip
/// applies instantly with no fade. UI-bound by house rule: construct on the
/// UI thread (pages do this in their constructor), every path never-throw
/// guarded, safe to call headless (no-op without a queue).
/// </summary>
internal sealed class LayoutDebouncer
{
    /// <summary>How long the size must sit still before the reflow applies.</summary>
    internal static readonly TimeSpan SettleDelay = TimeSpan.FromMilliseconds(120);

    private readonly DispatcherQueue? _queue;
    private readonly DispatcherQueueTimer? _timer;
    private Action? _pending;
    private bool? _appliedNarrow;

    public LayoutDebouncer(DispatcherQueue? queue)
    {
        _queue = queue;
        try
        {
            if (queue is not null)
            {
                _timer = queue.CreateTimer();
                _timer.Tick += OnTick;
            }
        }
        catch { }
    }

    /// <summary>
    /// Requests a layout apply once sizes settle. Rapid calls coalesce:
    /// only the latest action runs, <see cref="SettleDelay"/> after the
    /// last request. Never throws.
    /// </summary>
    public void Request(Action apply)
    {
        try
        {
            if (_timer is null || _queue is null)
            {
                // No dispatcher (headless/test): apply inline so behavior
                // stays identical, just not deferred.
                try { apply(); } catch { }
                return;
            }
            _pending = apply;
            _timer.Stop();
            _timer.Interval = SettleDelay;
            _timer.Start();
        }
        catch { }
    }

    /// <summary>Drops any pending apply. Never throws.</summary>
    public void Cancel()
    {
        try
        {
            _pending = null;
            _timer?.Stop();
        }
        catch { }
    }

    /// <summary>
    /// Applies the resting layout immediately (first paint, navigation
    /// arrival) and records the narrow state without any fade. Never throws.
    /// </summary>
    public void PaintInitial(bool narrow, Action apply)
    {
        try
        {
            _appliedNarrow = narrow;
            try { apply(); } catch { }
        }
        catch { }
    }

    /// <summary>
    /// Settle-aware breakpoint switch: coalesces rapid SizeChanged ticks and,
    /// once the width settles, re-measures fresh. Same breakpoint = no-op;
    /// a real flip applies instantly with no fade. The first tick after idle
    /// also applies immediately (leading edge) so a flip lands with the
    /// motion instead of snapping after the settle delay, while the trailing
    /// settle keeps window drags coalesced. Never throws.
    /// </summary>
    public void RequestSwap(FrameworkElement? root, Func<bool> measure, Action apply)
    {
        bool leading = false;
        try { leading = _timer is not null && !_timer.IsRunning; } catch { }
        if (leading)
            SwapNow(measure, apply); // first change after idle: apply now
        Request(() => SwapNow(measure, apply)); // trailing: flip or no-op
    }

    private void SwapNow(Func<bool> measure, Action apply)
    {
        bool narrow = false;
        try { narrow = measure(); } catch { }
        try
        {
            if (_appliedNarrow.HasValue && _appliedNarrow.Value == narrow)
            {
                // Same breakpoint: touch nothing — even re-applying identical
                // Grid lengths invalidates layout and reads as jitter.
                return;
            }
            _appliedNarrow = narrow;
        }
        catch { }
        try { apply(); } catch { }
    }

    private void OnTick(DispatcherQueueTimer timer, object? _)
    {
        try
        {
            timer.Stop();
            var run = _pending;
            _pending = null;
            run?.Invoke();
        }
        catch { }
    }
}
