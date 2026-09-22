using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace DevTemWinUi3.Services;

/// <summary>What a background task needs from the app to run.</summary>
public sealed class BackgroundTaskContext
{
    public BackgroundTaskContext(Window? mainWindow) => MainWindow = mainWindow;

    /// <summary>Main window for dialog-owning tasks (null headless).</summary>
    public Window? MainWindow { get; }
}

/// <summary>
/// One unit of periodic background work (Advancement C5). The update check
/// is task #1; future tasks (feed sync, cleanup) plug in without new
/// timers. <see cref="Interval"/> is the minimum spacing between runs;
/// <see cref="RequiresNetwork"/> skips ticks while offline.
/// </summary>
public interface IBackgroundTask
{
    string Name { get; }
    TimeSpan Interval { get; }
    bool RequiresNetwork { get; }
    Task RunAsync(BackgroundTaskContext context, CancellationToken cancellationToken);
}

/// <summary>
/// Tiny task registry + runner (Advancement C5): interval tracking,
/// single-flight per task (an overrunning task never stacks — the
/// shared-slot lesson), per-task cancellation, and an offline gate for
/// network tasks. Every path is never-throw guarded: one failing task
/// must never abort the loop or its siblings. The loop's lifetime stays
/// with the caller (window close / token), as before.
/// </summary>
public static class BackgroundTaskRunner
{
    private sealed class Entry
    {
        public Entry(IBackgroundTask task) => Task = task;
        public IBackgroundTask Task { get; }
        public DateTimeOffset LastRunUtc { get; set; } = DateTimeOffset.MinValue;
        public SemaphoreSlim Slot { get; } = new(1, 1);
        public CancellationTokenSource? CurrentCts;
    }

    private static readonly Dictionary<string, Entry> _tasks =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly object _gate = new();

    /// <summary>Test seam for the offline gate (default: real probe).</summary>
    internal static Func<bool>? NetworkProbeOverride;

    /// <summary>Registers (or replaces) a task. Never throws.</summary>
    public static void Register(IBackgroundTask task)
    {
        try
        {
            if (task is null || string.IsNullOrWhiteSpace(task.Name))
                return;
            lock (_gate)
            {
                if (_tasks.TryGetValue(task.Name, out var existing))
                {
                    try { existing.CurrentCts?.Cancel(); } catch { }
                }
                _tasks[task.Name] = new Entry(task);
            }
        }
        catch { }
    }

    /// <summary>Removes a task (cancelling its in-flight run). Never throws.</summary>
    public static void Unregister(string name)
    {
        try
        {
            lock (_gate)
            {
                if (_tasks.TryGetValue(name, out var existing))
                {
                    try { existing.CurrentCts?.Cancel(); } catch { }
                    _tasks.Remove(name);
                }
            }
        }
        catch { }
    }

    public static bool IsRegistered(string name)
    {
        try
        {
            lock (_gate)
            {
                return !string.IsNullOrWhiteSpace(name) && _tasks.ContainsKey(name);
            }
        }
        catch
        {
            return false;
        }
    }

    public static string[] RegisteredNames()
    {
        try
        {
            lock (_gate)
            {
                return _tasks.Keys.ToArray();
            }
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Registers the built-in tasks once (update check is task #1).
    /// Idempotent. Never throws.
    /// </summary>
    public static void EnsureDefaultsRegistered()
    {
        try
        {
            if (!IsRegistered(UpdateCheckBackgroundTask.TaskName))
                Register(new UpdateCheckBackgroundTask());
        }
        catch { }
    }

    /// <summary>Cancels a task's in-flight run (the loop keeps going). Never throws.</summary>
    public static void CancelTask(string name)
    {
        try
        {
            lock (_gate)
            {
                if (_tasks.TryGetValue(name, out var existing))
                {
                    try { existing.CurrentCts?.Cancel(); } catch { }
                }
            }
        }
        catch { }
    }

    /// <summary>
    /// Best-effort link check: false only when the OS reports no internet
    /// profile at all. Never throws.
    /// </summary>
    internal static bool HasInternetAccess()
    {
        try
        {
            if (NetworkProbeOverride is not null)
            {
                try { return NetworkProbeOverride(); } catch { return true; }
            }
            try
            {
                return Windows.Networking.Connectivity.NetworkInformation
                    .GetInternetConnectionProfile() is not null;
            }
            catch
            {
                return true;
            }
        }
        catch
        {
            return true;
        }
    }

    /// <summary>
    /// Runs one task now (single-flight: a still-running task is skipped,
    /// never stacked). Honors the offline gate. Returns whether it ran.
    /// Never throws.
    /// </summary>
    public static async Task<bool> RunTaskAsync(
        string name, BackgroundTaskContext context, CancellationToken cancellationToken)
    {
        Entry? entry;
        lock (_gate)
        {
            if (!_tasks.TryGetValue(name, out entry) || entry is null)
                return false;
        }
        if (context is null)
            return false;
        if (entry.Task.RequiresNetwork && !HasInternetAccess())
        {
            try { AppLog.Information("Background task '{Name}' skipped: offline", entry.Task.Name); } catch { }
            return false;
        }
        // Non-blocking try-enter on purpose: an overrunning task is
        // skipped, never stacked (None documents the intent for CA2016).
        if (!entry.Slot.Wait(0, CancellationToken.None))
            return false;
        var runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        lock (_gate)
        {
            entry.CurrentCts = runCts;
        }
        try
        {
            await entry.Task.RunAsync(context, runCts.Token).ConfigureAwait(false);
            entry.LastRunUtc = DateTimeOffset.UtcNow;
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            try { AppLog.Error(ex, "Background task '{Name}' failed", entry.Task.Name); } catch { }
            return false;
        }
        finally
        {
            lock (_gate)
            {
                if (ReferenceEquals(entry.CurrentCts, runCts))
                    entry.CurrentCts = null;
            }
            try { runCts.Dispose(); } catch { }
        }
    }

    /// <summary>
    /// Runs every due task once (due = never ran or interval elapsed).
    /// Never throws.
    /// </summary>
    public static async Task RunDueAsync(
        BackgroundTaskContext context, CancellationToken cancellationToken)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            List<string> due = new();
            lock (_gate)
            {
                foreach (var pair in _tasks)
                {
                    try
                    {
                        var interval = pair.Value.Task.Interval;
                        if (interval <= TimeSpan.Zero)
                            interval = TimeSpan.FromMinutes(30);
                        if (now - pair.Value.LastRunUtc >= interval)
                            due.Add(pair.Key);
                    }
                    catch { }
                }
            }
            foreach (string name in due)
            {
                try { await RunTaskAsync(name, context, cancellationToken).ConfigureAwait(false); }
                catch { }
            }
        }
        catch { }
    }

    /// <summary>
    /// Ticks every <paramref name="cadence"/> (first tick after one
    /// cadence, like the old loop) running due tasks, until cancelled.
    /// Never throws.
    /// </summary>
    public static async Task RunPeriodicAsync(
        BackgroundTaskContext context, TimeSpan cadence, CancellationToken cancellationToken)
    {
        if (cadence <= TimeSpan.Zero)
            cadence = TimeSpan.FromMinutes(30);
        try
        {
            using var timer = new PeriodicTimer(cadence);
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                try { await RunDueAsync(context, cancellationToken).ConfigureAwait(false); }
                catch { }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            try { AppLog.Error(ex, "Background task loop ended"); } catch { }
        }
    }

    internal static void ResetForTests()
    {
        try
        {
            lock (_gate)
            {
                foreach (var entry in _tasks.Values)
                {
                    try { entry.CurrentCts?.Cancel(); } catch { }
                }
                _tasks.Clear();
            }
            NetworkProbeOverride = null;
        }
        catch { }
    }
}

/// <summary>
/// The update check as background task #1. Interval tracks the service's
/// backend interval live (Velopack vs basic); the tick body is the
/// service's own check (auto-check setting, installed guard, ask-mode,
/// metered guard).
/// </summary>
internal sealed class UpdateCheckBackgroundTask : IBackgroundTask
{
    internal const string TaskName = "update-check";

    public string Name => TaskName;

    public TimeSpan Interval => BackgroundUpdateService.CheckInterval;

    public bool RequiresNetwork => true;

    public Task RunAsync(BackgroundTaskContext context, CancellationToken cancellationToken) =>
        BackgroundUpdateService.Current.CheckForUpdatesAsync(context?.MainWindow);
}
