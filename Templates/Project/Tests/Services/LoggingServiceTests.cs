using System;
using System.IO;
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

    [TestMethod]
    public void EnforceDirectoryQuota_DeletesOldestFirst()
    {
        // P1-1: over-cap directories shed the oldest applog files first;
        // non-matching files are never touched.
        string dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        try
        {
            string oldFile = Path.Combine(dir, "applog-20200101.log");
            string newFile = Path.Combine(dir, "applog-20260101.log");
            string keep = Path.Combine(dir, "notes.txt");
            File.WriteAllBytes(oldFile, new byte[700]);
            File.WriteAllBytes(newFile, new byte[700]);
            File.WriteAllBytes(keep, new byte[700]);
            File.SetLastWriteTimeUtc(oldFile, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            File.SetLastWriteTimeUtc(newFile, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            LoggingService.EnforceDirectoryQuota(dir, 1000);

            Assert.IsFalse(File.Exists(oldFile));
            Assert.IsTrue(File.Exists(newFile));
            Assert.IsTrue(File.Exists(keep));
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }

    [TestMethod]
    public void EnforceDirectoryQuota_UnderCap_KeepsEverything()
    {
        string dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        try
        {
            string file = Path.Combine(dir, "applog-20260101.log");
            File.WriteAllBytes(file, new byte[100]);
            LoggingService.EnforceDirectoryQuota(dir, 1000);
            Assert.IsTrue(File.Exists(file));
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }

    [TestMethod]
    public void DefaultLevels_FollowBuildConfiguration()
    {
        // P1-1: self-consistent (no compile-time DEBUG conditional — those
        // are evaluated at scaffold time): Debug builds default to
        // Debug, Release builds to Information.
        var expected = LoggingService.IsDebugBuild ? LogLevel.Debug : LogLevel.Information;
        Assert.AreEqual(expected, LoggingService.DefaultMinimumLevel);
    }
}
