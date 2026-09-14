using System;
using System.Collections.Generic;
using DevTemWinUi3.Services.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DevTemWinUi3.Tests.Services;

/// <summary>Factory for backend-agnostic buffer entries in headless tests (no logger needed).</summary>
internal static class TestEvents
{
    internal static LogEntry Make(
        string message,
        string sourceContext = "Test.Space",
        LogLevel level = LogLevel.Information,
        Exception? exception = null)
    {
        return new LogEntry(
            DateTimeOffset.UtcNow,
            level,
            sourceContext,
            message,
            exception,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
    }
}
