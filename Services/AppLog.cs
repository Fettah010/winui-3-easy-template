using System;
using Microsoft.Extensions.Logging;

// A facade takes caller-owned templates by design: source-generated
// LoggerMessage delegates (CA1848) and constant-template rules (CA2254)
// cannot apply here. The never-throws wrapper below is the mitigation.
#pragma warning disable CA1848
#pragma warning disable CA2254

namespace DevTemWinUi3.Services;

/// <summary>
/// Static logging facade for app code. Everything logs through
/// <see cref="Microsoft.Extensions.Logging.ILogger"/> so the backing
/// pipeline (Serilog today, swappable tomorrow) never leaks into call
/// sites. Backed by a silent no-op factory until
/// <see cref="LoggingService.Initialize"/> swaps in the real pipeline.
/// Never throws â€” logging must not crash the app it observes.
/// </summary>
public static class AppLog
{
    private static readonly object _lock = new();
    private static ILogger _logger = LoggerFactory.Create(static _ => { }).CreateLogger("DevTem");

    /// <summary>
    /// Swaps the backing logger. Called once by
    /// <see cref="LoggingService.Initialize"/>; tests inject their own
    /// factory. Null is ignored. Never throws.
    /// </summary>
    public static void Initialize(ILoggerFactory? factory)
    {
        try
        {
            if (factory is null)
                return;
            lock (_lock)
            {
                _logger = factory.CreateLogger("DevTem");
            }
        }
        catch { }
    }

    public static void Debug(string message, params object?[] args) =>
        Write(LogLevel.Debug, null, message, args);

    public static void Information(string message, params object?[] args) =>
        Write(LogLevel.Information, null, message, args);

    public static void Warning(string message, params object?[] args) =>
        Write(LogLevel.Warning, null, message, args);

    public static void Warning(Exception? exception, string message, params object?[] args) =>
        Write(LogLevel.Warning, exception, message, args);

    public static void Error(string message, params object?[] args) =>
        Write(LogLevel.Error, null, message, args);

    public static void Error(Exception? exception, string message, params object?[] args) =>
        Write(LogLevel.Error, exception, message, args);

    public static void Fatal(string message, params object?[] args) =>
        Write(LogLevel.Critical, null, message, args);

    public static void Fatal(Exception? exception, string message, params object?[] args) =>
        Write(LogLevel.Critical, exception, message, args);

    private static void Write(LogLevel level, Exception? exception, string message, object?[] args)
    {
        try
        {
            _logger.Log(level, exception, message, args);
        }
        catch { }
    }
}

#pragma warning restore CA2254
#pragma warning restore CA1848
