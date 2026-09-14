using System;
using System.Globalization;
using DevTemWinUi3.Services.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// This file's purpose is exercising the MEL surface directly; LoggerMessage
// source-gen delegates (CA1848) cannot apply to intentionally varied templates.
#pragma warning disable CA1848

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class InMemoryLogSinkLoggerProviderTests
{
    private static (InMemoryLogSink Sink, ILogger Logger) Create()
    {
        var sink = new InMemoryLogSink();
        ILogger logger = new InMemoryLogSinkLoggerProvider(sink).CreateLogger("Test.Category");
        return (sink, logger);
    }

    [TestMethod]
    public void Levels_PassThroughAsMELLevels()
    {
        var (sink, logger) = Create();
        logger.LogTrace("t");
        logger.LogDebug("d");
        logger.LogInformation("i");
        logger.LogWarning("w");
        logger.LogError("e");
        logger.LogCritical("c");
        var snapshot = sink.SnapshotNewestFirst();
        Assert.HasCount(6, snapshot);
        // Newest first: c, e, w, i, d, t.
        Assert.AreEqual(LogLevel.Critical, snapshot[0].Level);
        Assert.AreEqual(LogLevel.Error, snapshot[1].Level);
        Assert.AreEqual(LogLevel.Warning, snapshot[2].Level);
        Assert.AreEqual(LogLevel.Information, snapshot[3].Level);
        Assert.AreEqual(LogLevel.Debug, snapshot[4].Level);
        Assert.AreEqual(LogLevel.Trace, snapshot[5].Level);
    }

    [TestMethod]
    public void Exception_Category_And_Message_ArePreserved()
    {
        var (sink, logger) = Create();
        var failure = new InvalidOperationException("boom");
        logger.LogError(failure, "GET {Url} failed", "https://x");
        var snapshot = sink.SnapshotNewestFirst();
        Assert.HasCount(1, snapshot);
        Assert.AreSame(failure, snapshot[0].Exception);
        Assert.Contains("https://x", snapshot[0].Message);
        Assert.AreEqual("Test.Category", snapshot[0].Category);
    }

    [TestMethod]
    public void NoneLevel_IsSkipped_AndNeverThrows()
    {
        var (sink, logger) = Create();
#pragma warning disable CA1848, CA2254 // Intentional: exercises dynamic-template + null paths.
        logger.Log(LogLevel.None, "skipped");
        Assert.HasCount(0, sink.SnapshotNewestFirst());
        logger.LogInformation(null!); // Must not throw; the entry itself is backend-defined.
    }
}

#pragma warning restore CA1848
