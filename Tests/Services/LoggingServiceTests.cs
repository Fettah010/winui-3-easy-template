using System;
using DevTemWinUi3.Services;
using DevTemWinUi3.Services.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog.Events;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class LoggingServiceTests
{
    [TestMethod]
    public void Defaults_AreSane_BeforeInitialize()
    {
        // No Initialize() call: it writes real log files, so tests pin the
        // pre-init defaults (mirroring the Serilog configuration).
        Assert.AreEqual(LogEventLevel.Debug, LoggingService.MinimumLevel);
        Assert.IsFalse(string.IsNullOrWhiteSpace(LoggingService.CurrentLogDirectory));
        Assert.Contains("Logs", LoggingService.CurrentLogDirectory);
    }

    [TestMethod]
    public void EventLogSink_RespectsEnvFlag()
    {
        string? previous = Environment.GetEnvironmentVariable("DEVTEM_EVENT_LOG");
        try
        {
            Environment.SetEnvironmentVariable("DEVTEM_EVENT_LOG", null);
            Assert.IsFalse(EventLogSink.IsEnabledByConfig());
            Environment.SetEnvironmentVariable("DEVTEM_EVENT_LOG", "1");
            Assert.IsTrue(EventLogSink.IsEnabledByConfig());
        }
        finally
        {
            Environment.SetEnvironmentVariable("DEVTEM_EVENT_LOG", previous);
        }
    }
}
