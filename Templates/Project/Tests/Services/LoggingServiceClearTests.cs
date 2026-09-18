using System;
using System.IO;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class LoggingServiceClearTests
{
    [TestMethod]
    public void ClearCurrentLogFile_TruncatesOnlyTodaysFile()
    {
        if (!LoggingService.HasFileSink)
            Assert.Inconclusive("No file sink on this logging backend.");
        string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            string today = Path.Combine(dir, $"applog-{DateTime.Now:yyyyMMdd}.log");
            File.WriteAllText(today, "old lines\n");
            string archive = Path.Combine(dir, "applog-20000101.log");
            File.WriteAllText(archive, "archive\n");

            Assert.IsTrue(LoggingService.ClearCurrentLogFile(dir));
            Assert.AreEqual(0L, new FileInfo(today).Length);
            Assert.AreEqual("archive\n", File.ReadAllText(archive));
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }

    [TestMethod]
    public void ClearCurrentLogFile_MissingFile_ReturnsTrue()
    {
        if (!LoggingService.HasFileSink)
            Assert.Inconclusive("No file sink on this logging backend.");
        string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            Assert.IsTrue(LoggingService.ClearCurrentLogFile(dir));
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }
}
