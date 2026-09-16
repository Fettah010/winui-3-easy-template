using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace DevTemWinUi3.Services.Diagnostics;

/// <summary>
/// Bounded in-memory log buffer: keeps the newest N <see cref="LogEntry"/>
/// records for the live tail, structured filtering, and export.
/// Drop-oldest on overflow. Never throws — diagnostics must not crash the
/// app it observes. Backend-agnostic: Serilog feeds it through
/// <c>SerilogLogEntrySink</c>, MEL through
/// <c>InMemoryLogSinkLoggerProvider</c>.
/// </summary>
public sealed class InMemoryLogSink
{
    public const int DefaultCapacity = 1000;

    private readonly ConcurrentQueue<LogEntry> _events = new();
    private readonly int _capacity;
    private readonly object _trimGate = new();
    private int _count;

    public InMemoryLogSink(int capacity = DefaultCapacity)
    {
        _capacity = capacity <= 0 ? DefaultCapacity : capacity;
    }

    /// <summary>
    /// Buffered event count. O(1): an <see cref="Interlocked"/> counter,
    /// not the queue snapshot the old <c>ConcurrentQueue.Count</c> took
    /// on every emit (P1-1). Exact at quiescence; may transiently lag by
    /// one mid-emit under contention. Never throws.
    /// </summary>
    public int Count => Math.Max(0, Volatile.Read(ref _count));

    public void Emit(LogEntry entry)
    {
        try
        {
            if (entry is null)
                return;
            // One trimmer at a time: a stale-read-then-trim race used to
            // dequeue twice for one excess item (count drifted under
            // capacity). The lock spans nanosecond integer/queue ops only.
            lock (_trimGate)
            {
                _events.Enqueue(entry);
                int count = Interlocked.Increment(ref _count);
                while (count > _capacity)
                {
                    if (_events.TryDequeue(out _))
                        count = Interlocked.Decrement(ref _count);
                    else
                        break;
                }
            }
        }
        catch { }
    }

    /// <summary>
    /// Drops every buffered event (the diagnostics Clear action).
    /// Takes the trim gate so a concurrent emit can neither slip in
    /// uncounted nor resurrect the count. Never throws.
    /// </summary>
    public void Clear()
    {
        try
        {
            lock (_trimGate)
            {
                while (_events.TryDequeue(out _)) { }
                Interlocked.Exchange(ref _count, 0);
            }
        }
        catch { }
    }
    /// <summary>Snapshot, newest first. Empty on any error.</summary>
    public IReadOnlyList<LogEntry> SnapshotNewestFirst()
    {
        try
        {
            var snapshot = _events.ToArray();
            Array.Reverse(snapshot);
            return snapshot;
        }
        catch
        {
            return Array.Empty<LogEntry>();
        }
    }
}
