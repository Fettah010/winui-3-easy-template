using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace DevTemWinUi3.Services.Diagnostics;

/// <summary>
/// Backend-agnostic log record for the in-memory diagnostics buffer.
/// Serilog events and MEL records are both converted to this shape, so
/// the diagnostics page, export, and tests never touch vendor types.
/// Members are never null (empty fallbacks instead).
/// </summary>
public sealed record LogEntry(
    DateTimeOffset Timestamp,
    LogLevel Level,
    string Category,
    string Message,
    Exception? Exception,
    IReadOnlyDictionary<string, string> Properties);
