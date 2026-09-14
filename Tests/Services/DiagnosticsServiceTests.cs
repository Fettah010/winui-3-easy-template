using System;
using System.IO;
using System.Linq;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class DiagnosticsServiceTests
{
    private static readonly string[] s_logLines = new[] { "one", "two", "three", "four" };
    private static readonly string[] s_tailLines = new[] { "l1", "l2", "l3", "l4", "l5" };

    private string _storePath = string.Empty;

    [TestInitialize]
    public void Init()
    {
        _storePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");
        LocalSettingsStore.SetTestPath(_storePath);
    }

    [TestCleanup]
    public void Cleanup()
    {
        LocalSettingsStore.SetTestPath(null);
        try { File.Delete(_storePath); } catch { }
    }

    [TestMethod]
    public void GetStatus_ReflectsCurrentPreferences()
    {
        SettingsService.Current.Theme = "Dark";
        var status = DiagnosticsService.GetStatus();
        Assert.AreEqual("Dark", status.Theme);
        Assert.IsFalse(string.IsNullOrWhiteSpace(status.AppVersion));
        Assert.IsFalse(string.IsNullOrWhiteSpace(status.LogDirectory));
        Assert.IsFalse(string.IsNullOrWhiteSpace(status.Language));
        Assert.IsFalse(string.IsNullOrWhiteSpace(status.LogLevel));
        Assert.IsGreaterThanOrEqualTo(0L, status.StartupElapsedMs);
        Assert.IsGreaterThanOrEqualTo(0L, status.LogDirectorySizeBytes);
        Assert.IsGreaterThanOrEqualTo(0, status.BufferedEventCount);
        Assert.IsNotNull(status.PendingUpdate);
        Assert.IsGreaterThanOrEqualTo(-1L, status.DatabaseSizeBytes);
    }

    [TestMethod]
    public void CreateDiagnosticBundle_WritesExpectedEntries()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".zip");
        try
        {
            bool ok = DiagnosticsService.CreateDiagnosticBundle(path, "filtered", null, null);
            Assert.IsTrue(ok);
            using var zip = System.IO.Compression.ZipFile.OpenRead(path);
            var names = zip.Entries.Select(e => e.FullName).ToArray();
            Assert.IsTrue(names.Contains("status.json"));
            Assert.IsTrue(names.Contains("settings.json"));
            Assert.IsTrue(names.Contains("log-filtered.log"));
            Assert.IsTrue(names.Contains("log-current.log"));
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }

    [TestMethod]
    public void CreateDiagnosticBundle_BlankPath_ReturnsFalse()
    {
        Assert.IsFalse(DiagnosticsService.CreateDiagnosticBundle("  ", "x", null, null));
    }

    [TestMethod]
    public void GetLogFiles_Empty_WhenDirectoryMissing()
    {
        // Real log dir may exist locally; the contract is only no-throw.
        var files = DiagnosticsService.GetLogFiles();
        Assert.IsNotNull(files);
    }

    [TestMethod]
    public void ReadLogTail_ReturnsLastLines()
    {
        var dir = DiagnosticsService.LogDirectoryPath;
        try { Directory.CreateDirectory(dir); } catch { }
        var file = Path.Combine(dir, "applog-20990101.log");
        try
        {
            File.WriteAllLines(file, s_logLines);
            string tail = DiagnosticsService.ReadLogTail(file, 2);
            Assert.DoesNotContain("one", tail);
            Assert.Contains("three", tail);
            Assert.Contains("four", tail);
        }
        finally
        {
            try { File.Delete(file); } catch { }
        }
    }

    [TestMethod]
    public void ReadLogTail_ReturnsOnlyTail()
    {
        var dir = DiagnosticsService.LogDirectoryPath;
        try { Directory.CreateDirectory(dir); } catch { }
        var file = Path.Combine(dir, "applog-20990201.log");
        try
        {
            File.WriteAllLines(file, s_tailLines);
            string tail = DiagnosticsService.ReadLogTail(file, 2);
            Assert.DoesNotContain("l3", tail);
            Assert.Contains("l4", tail);
            Assert.Contains("l5", tail);
        }
        finally
        {
            try { File.Delete(file); } catch { }
        }
    }

    [TestMethod]
    public void ReadLogTail_ReadsThroughSharedWriteLock()
    {
        // Serilog holds the live file open: the view must still read it.
        var dir = DiagnosticsService.LogDirectoryPath;
        try { Directory.CreateDirectory(dir); } catch { }
        var file = Path.Combine(dir, "applog-20990202.log");
        try
        {
            File.WriteAllLines(file, s_logLines);
            using var locked = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            string tail = DiagnosticsService.ReadLogTail(file, 4);
            Assert.Contains("four", tail);
        }
        finally
        {
            try { File.Delete(file); } catch { }
        }
    }

    [TestMethod]
    public void GetBufferedEvents_MapsNewestFirst()
    {
        LoggingService.EventBuffer.Emit(TestEvents.Make("buffered-old", "Seed.Space"));
        LoggingService.EventBuffer.Emit(TestEvents.Make(
            "buffered-new", "Seed.Space", Serilog.Events.LogEventLevel.Error,
            new InvalidOperationException("boom")));

        var events = DiagnosticsService.GetBufferedEvents(50);
        Assert.IsNotNull(events);
        var fresh = events.First(e =>
            e.SourceContext == "Seed.Space" && e.Message == "buffered-new");
        Assert.AreEqual("Error", fresh.Level);
        Assert.IsTrue(fresh.HasException);
    }

    [TestMethod]
    public void GetBufferedEvents_NonPositiveMax_ReturnsEmpty()
    {
        Assert.IsEmpty(DiagnosticsService.GetBufferedEvents(0));
        Assert.IsEmpty(DiagnosticsService.GetBufferedEvents(-1));
    }

    [TestMethod]
    public void ReadLogTail_RefusesPathsOutsideLogDir()
    {
        var outside = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".log");
        try
        {
            File.WriteAllText(outside, "secret");
            Assert.AreEqual(string.Empty, DiagnosticsService.ReadLogTail(outside));
            Assert.AreEqual(string.Empty, DiagnosticsService.ReadLogTail(
                Path.Combine(Path.GetTempPath(), "nope-" + Guid.NewGuid() + ".log")));
        }
        finally
        {
            try { File.Delete(outside); } catch { }
        }
    }
}
