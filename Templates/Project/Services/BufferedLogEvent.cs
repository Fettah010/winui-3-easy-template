using System;
using System.Collections.Generic;
using System.Globalization;

namespace DevTemWinUi3.Services;

/// <summary>
/// A single in-memory log event for the live tail. Never null members.
/// Top-level (not nested in <c>DiagnosticsService</c>) so compiled XAML
/// bindings can name it via <c>x:DataType</c> — mistyped row paths then
/// fail the build instead of rendering blank at runtime.
/// </summary>
public sealed record BufferedLogEvent(
    DateTimeOffset Timestamp,
    string Level,
    string Message,
    string? SourceContext,
    bool HasException,
    string? ExceptionText,
    IReadOnlyDictionary<string, string> Properties)
{
    /// <summary>Short clock time for list rows (invariant, parseable).</summary>
    public string DisplayTime =>
        Timestamp.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
}
