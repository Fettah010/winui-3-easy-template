using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Serilog.Core;
using Serilog.Events;

namespace DevTemWinUi3.Services.Diagnostics;

/// <summary>
/// Bounded in-memory Serilog sink: keeps the newest N events for the live
/// tail, structured filtering, and export. Drop-oldest on overflow.
/// Never throws — diagnostics must not crash the app it observes.
/// </summary>
public sealed class InMemoryLogSink : ILogEventSink
{
    public const int DefaultCapacity = 1000;

    private readonly ConcurrentQueue<LogEvent> _events = new();
    private readonly int _capacity;

    public InMemoryLogSink(int capacity = DefaultCapacity)
    {
        _capacity = capacity <= 0 ? DefaultCapacity : capacity;
    }

    public int Count => _events.Count;

    public void Emit(LogEvent logEvent)
    {
        try
        {
            if (logEvent is null)
                return;
            _events.Enqueue(logEvent);
            while (_events.Count > _capacity && _events.TryDequeue(out _)) { }
        }
        catch { }
    }

    /// <summary>Snapshot, newest first. Empty on any error.</summary>
    public IReadOnlyList<LogEvent> SnapshotNewestFirst()
    {
        try
        {
            var snapshot = _events.ToArray();
            Array.Reverse(snapshot);
            return snapshot;
        }
        catch
        {
            return Array.Empty<LogEvent>();
        }
    }
}
