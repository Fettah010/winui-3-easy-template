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
    private int _count;

    public InMemoryLogSink(int capacity = DefaultCapacity)
    {
        _capacity = capacity <= 0 ? DefaultCapacity : capacity;
    }

    /// <summary>
    /// Buffered event count. O(1): an <see cref="Interlocked"/> counter,
    /// not the queue snapshot the old <c>ConcurrentQueue.Count</c> took
    /// on every emit (P1-1). May lag the true length by a hair under
    /// contention; never throws.
    /// </summary>
    public int Count => Math.Max(0, Volatile.Read(ref _count));

    public void Emit(LogEntry entry)
    {
        try
        {
            if (entry is null)
                return;
            _events.Enqueue(entry);
            Interlocked.Increment(ref _count);
            while (Volatile.Read(ref _count) > _capacity)
            {
                if (_events.TryDequeue(out _))
                    Interlocked.Decrement(ref _count);
                else
                    break;
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
