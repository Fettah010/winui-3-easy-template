using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;

namespace DevTemWinUi3.Services.Diagnostics;

/// <summary>
/// Serilog-to-<see cref="LogEntry"/> adapter: the Serilog pipeline feeds
/// the backend-agnostic buffer through this sink. Scaffolded only for the
/// serilog backend. Never throws.
/// </summary>
public sealed class SerilogLogEntrySink : ILogEventSink
{
    private readonly InMemoryLogSink _sink;

    public SerilogLogEntrySink(InMemoryLogSink sink)
    {
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
    }

    public void Emit(LogEvent logEvent)
    {
        try
        {
            if (logEvent is null)
                return;
            string message;
            try { message = logEvent.RenderMessage(System.Globalization.CultureInfo.InvariantCulture); }
            catch { message = logEvent.MessageTemplate.Text; }
            string category = "?";
            var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var kv in logEvent.Properties)
                {
                    try { properties[kv.Key] = kv.Value.ToString(); } catch { }
                }
                if (logEvent.Properties.TryGetValue("SourceContext", out var v))
                    category = v.ToString().Trim('"');
            }
            catch { }
            _sink.Emit(new LogEntry(
                logEvent.Timestamp, MapLevel(logEvent.Level), category,
                message ?? string.Empty, logEvent.Exception, properties));
        }
        catch { }
    }

    private static LogLevel MapLevel(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => LogLevel.Trace,
        LogEventLevel.Debug => LogLevel.Debug,
        LogEventLevel.Information => LogLevel.Information,
        LogEventLevel.Warning => LogLevel.Warning,
        LogEventLevel.Error => LogLevel.Error,
        LogEventLevel.Fatal => LogLevel.Critical,
        _ => LogLevel.Information,
    };
}
