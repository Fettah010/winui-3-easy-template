using System;
using DevTemWinUi3.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class LoggingServiceTests
{
    private static readonly string[] s_knownBackends = new[] { "serilog", "mel", "none" };

    [TestMethod]
    public void Defaults_AreSane_BeforeInitialize()
    {
        // No Initialize() call: it writes real log files, so tests pin the
        // pre-init defaults (mirroring the backend configuration).
        Assert.AreEqual(LogLevel.Debug, LoggingService.MinimumLevel);
        Assert.IsFalse(string.IsNullOrWhiteSpace(LoggingService.CurrentLogDirectory));
        Assert.Contains("Logs", LoggingService.CurrentLogDirectory);
    }

    [TestMethod]
    public void BackendName_IsRecognizedValue()
    {
        // Template replaces render BackendName from the logging choice
        // (serilog, mel, or none); the repo tree is the serilog reference.
        CollectionAssert.Contains(s_knownBackends, LoggingService.BackendName);
        bool expectFile = LoggingService.BackendName == "serilog";
        Assert.AreEqual(expectFile, LoggingService.HasFileSink);
    }

    [TestMethod]
    public void EventLogGate_RespectsEnvFlag()
    {
        // Backend-aware: the none backend has no Event Log code at all, so
        // the gate is false there even when the deployer opts in.
        string? previous = Environment.GetEnvironmentVariable("DEVTEM_EVENT_LOG");
        try
        {
            Environment.SetEnvironmentVariable("DEVTEM_EVENT_LOG", null);
            Assert.IsFalse(LoggingService.IsEventLogEnabled);
            Environment.SetEnvironmentVariable("DEVTEM_EVENT_LOG", "1");
            Assert.AreEqual(LoggingService.BackendName != "none", LoggingService.IsEventLogEnabled);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DEVTEM_EVENT_LOG", previous);
        }
    }
}
