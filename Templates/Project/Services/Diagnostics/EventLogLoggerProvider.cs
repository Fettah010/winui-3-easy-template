using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DevTemWinUi3.Services.Diagnostics;

/// <summary>
/// Windows Event Log provider for Error/Critical records (enterprise
/// collection) for the mel backend. Explicit opt-in only
/// (<c>DEVTEM_EVENT_LOG=1</c>); never throws. Mirrors the Serilog
/// <c>EventLogSink</c> gate and entry mapping. Scaffolded only for the mel
/// backend. OS-touching code stays untested behind the never-throw guard;
/// only the config gate is unit-tested (through
/// <c>LoggingService.IsEventLogEnabled</c>).
/// </summary>
public sealed class EventLogLoggerProvider : ILoggerProvider
{
    private static bool _probed;
    private static bool _usable;
    private static readonly object s_probeLock = new();

    /// <summary>Whether the deployer enabled the provider. Never throws.</summary>
    internal static bool IsEnabledByConfig()
    {
        try
        {
            return string.Equals(Environment.GetEnvironmentVariable("DEVTEM_EVENT_LOG"),
                "1", StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    public ILogger CreateLogger(string categoryName) =>
        new EventLogger(categoryName ?? "?");

    public void Dispose()
    {
    }

    private sealed class EventLogger(string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) =>
            logLevel is LogLevel.Error or LogLevel.Critical;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            try
            {
                if (!IsEnabled(logLevel) || formatter is null || !EnsureUsable())
                    return;
                string message;
                try { message = formatter(state, exception) ?? string.Empty; }
                catch { return; }
                if (exception is not null)
                    message += Environment.NewLine + exception;
                using var log = new EventLog("Application") { Source = AppMetadata.AppName };
                log.WriteEntry($"[{category}] {message}", EventLogEntryType.Error);
            }
            catch { }
        }

        private static bool EnsureUsable()
        {
            try
            {
                if (_probed)
                    return _usable;
                lock (s_probeLock)
                {
                    if (_probed)
                        return _usable;
                    try
                    {
                        using var log = new EventLog("Application") { Source = AppMetadata.AppName };
                        _ = log.Entries.Count;
                        _usable = true;
                    }
                    catch
                    {
                        _usable = false;
                    }
                    _probed = true;
                    return _usable;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
