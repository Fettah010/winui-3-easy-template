using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace DevTemWinUi3.Services.Diagnostics;

/// <summary>
/// MEL bridge into <see cref="InMemoryLogSink"/>: converts
/// <see cref="Microsoft.Extensions.Logging"/> records to backend-agnostic
/// <see cref="LogEntry"/> records so the diagnostics buffer, filtering, and
/// export work identically regardless of the logging backend. Wired by the
/// mel and none backends (the latter only when health is on); the Serilog
/// backend uses <c>SerilogLogEntrySink</c> instead. Never throws.
/// </summary>
public sealed class InMemoryLogSinkLoggerProvider : ILoggerProvider
{
    private readonly InMemoryLogSink _sink;

    public InMemoryLogSinkLoggerProvider(InMemoryLogSink sink)
    {
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
    }

    public ILogger CreateLogger(string categoryName) =>
        new SinkLogger(_sink, categoryName ?? "?");

    public void Dispose()
    {
    }

    private sealed class SinkLogger(InMemoryLogSink sink, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            try
            {
                if (logLevel == LogLevel.None || formatter is null)
                    return;
                string rendered;
                try
                {
                    rendered = formatter(state, exception) ?? string.Empty;
                }
                catch
                {
                    return;
                }

                var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["EventId"] = eventId.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                };
                try
                {
                    if (state is IEnumerable<KeyValuePair<string, object?>> structured)
                    {
                        foreach (var kv in structured)
                        {
                            if (kv.Key == "{OriginalFormat}")
                                continue;
                            try { properties[kv.Key] = kv.Value?.ToString() ?? string.Empty; } catch { }
                        }
                    }
                }
                catch { }

                sink.Emit(new LogEntry(
                    DateTimeOffset.Now, logLevel, category, rendered, exception, properties));
            }
            catch { }
        }
    }
}
