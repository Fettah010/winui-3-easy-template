using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Animation;

namespace DevTemWinUi3.Services;

/// <summary>
/// Launch-animation policy (performance plan P0-1): the visual language
/// stays, the blocking waits go. Storyboard waits complete on the
/// animation itself (bounded timeout fallback, never a fixed sleep on the
/// critical path), and durations collapse to ~1 ms when the user prefers
/// reduced motion or opts into fast launch.
/// </summary>
public static class LaunchAnimations
{
    internal static bool? s_reducedMotionOverride;
    internal static bool? s_fastLaunchOverride;

    /// <summary>
    /// Whether the OS reports "show animations" off / reduced motion on.
    /// Best-effort (false when unreadable); never throws. Tests pin the
    /// override instead of the OS call.
    /// </summary>
    public static bool ReducedMotion
    {
        get
        {
            if (s_reducedMotionOverride.HasValue)
                return s_reducedMotionOverride.Value;
            try
            {
                return !new Windows.UI.ViewManagement.UISettings().AnimationsEnabled;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Opt-in launch escape hatch: <c>DEVTEM_FAST_LAUNCH=1</c> or the
    /// persisted <c>FastLaunch</c> setting. Never throws.
    /// </summary>
    public static bool FastLaunch
    {
        get
        {
            if (s_fastLaunchOverride.HasValue)
                return s_fastLaunchOverride.Value;
            try
            {
                if (string.Equals(
                    Environment.GetEnvironmentVariable("DEVTEM_FAST_LAUNCH"),
                    "1", StringComparison.Ordinal))
                    return true;
            }
            catch { }
            try
            {
                return SettingsService.Current.FastLaunch;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Collapses <paramref name="normal"/> to ~1 ms when animations are
    /// gated off; returns it unchanged otherwise. Pure (headless-testable).
    /// </summary>
    public static TimeSpan Scale(TimeSpan normal)
    {
        if (normal <= TimeSpan.Zero)
            return normal;
        return (FastLaunch || ReducedMotion)
            ? TimeSpan.FromMilliseconds(1)
            : normal;
    }

    /// <summary>
    /// Begins <paramref name="story"/> and completes when its
    /// <c>Completed</c> fires, or after <paramref name="timeout"/> as a
    /// bounded fallback. Must be called on the UI thread. Never throws.
    /// </summary>
    public static Task AwaitStoryboardAsync(Storyboard? story, TimeSpan timeout)
    {
        if (story is null)
            return Task.CompletedTask;
        var done = new TaskCompletionSource<object?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            void Handler(object? sender, object? e)
            {
                try { story.Completed -= Handler; } catch { }
                done.TrySetResult(null);
            }
            story.Completed += Handler;
            try
            {
                story.Begin();
            }
            catch
            {
                done.TrySetResult(null);
            }
            _ = Task.Delay(timeout).ContinueWith(
                _ => done.TrySetResult(null),
                TaskScheduler.Default);
            return done.Task;
        }
        catch
        {
            return Task.CompletedTask;
        }
    }
}
