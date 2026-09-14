using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

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

    public InMemoryLogSink(int capacity = DefaultCapacity)
    {
        _capacity = capacity <= 0 ? DefaultCapacity : capacity;
    }

    public int Count => _events.Count;

    public void Emit(LogEntry entry)
    {
        try
        {
            if (entry is null)
                return;
            _events.Enqueue(entry);
            while (_events.Count > _capacity && _events.TryDequeue(out _)) { }
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
