using Serilog.Core;
using Serilog.Events;

namespace DevTemWinUi3.Services.Diagnostics;

/// <summary>
/// Adds the managed thread id to every event (dependency-free alternative
/// to the Thread enricher package). Never throws.
/// </summary>
internal sealed class ThreadIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        try
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "ThreadId", System.Environment.CurrentManagedThreadId));
        }
        catch { }
    }
}
