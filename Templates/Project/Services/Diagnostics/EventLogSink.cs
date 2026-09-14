using System;
using System.Diagnostics;
using System.Globalization;
using Serilog.Core;
using Serilog.Events;

namespace DevTemWinUi3.Services.Diagnostics;

/// <summary>
/// Windows Event Log sink for Error/Fatal events (enterprise collection).
/// Explicit opt-in only (<c>DEVTEM_EVENT_LOG=1</c>); never throws. Emitting
/// is best-effort: source registration needs elevation on first use, so a
/// one-time probe gates all writes. OS-touching code stays untested behind
/// the never-throw guard; only the config gate is unit-tested.
/// </summary>
internal sealed class EventLogSink : ILogEventSink
{
    private static bool _probed;
    private static bool _usable;
    private static readonly object s_probeLock = new();

    /// <summary>Whether the deployer enabled the sink. Never throws.</summary>
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

    public void Emit(LogEvent logEvent)
    {
        try
        {
            if (logEvent is null || !EnsureUsable())
                return;
            EventLogEntryType type = logEvent.Level switch
            {
                LogEventLevel.Fatal or LogEventLevel.Error => EventLogEntryType.Error,
                LogEventLevel.Warning => EventLogEntryType.Warning,
                _ => EventLogEntryType.Information,
            };
            using var log = new EventLog("Application") { Source = AppMetadata.AppName };
            log.WriteEntry(logEvent.RenderMessage(CultureInfo.InvariantCulture), type);
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
