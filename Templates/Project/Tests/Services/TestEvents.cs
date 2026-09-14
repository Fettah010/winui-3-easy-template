using System;
using System.Collections.Generic;
using Serilog.Events;
using Serilog.Parsing;

namespace DevTemWinUi3.Tests.Services;

/// <summary>Factory for Serilog events in headless tests (no logger needed).</summary>
internal static class TestEvents
{
    private static readonly MessageTemplateParser s_parser = new();

    internal static LogEvent Make(
        string message,
        string sourceContext = "Test.Space",
        LogEventLevel level = LogEventLevel.Information,
        Exception? exception = null)
    {
        var template = s_parser.Parse(message);
        var properties = new List<LogEventProperty>
        {
            new("SourceContext", new ScalarValue(sourceContext)),
        };
        return new LogEvent(DateTimeOffset.UtcNow, level, exception, template, properties);
    }
}
