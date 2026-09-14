using System;
using System.Collections.Generic;
using DevTemWinUi3.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class AppLogTests
{
    /// <summary>In-memory MEL provider: proves AppLog routes through ILogger, not Serilog.</summary>
    private sealed class ListLoggerProvider : ILoggerProvider
    {
        public readonly List<(LogLevel Level, string Message, Exception? Error)> Entries = new();

        public ILogger CreateLogger(string categoryName) => new ListLogger(Entries);

        public void Dispose() { }

        private sealed class ListLogger(List<(LogLevel, string, Exception?)> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel, EventId eventId, TState state,
                Exception? exception, Func<TState, Exception?, string> formatter)
            {
                lock (entries)
                {
                    entries.Add((logLevel, formatter(state, exception), exception));
                }
            }
        }
    }

    private static void UseProvider(ListLoggerProvider provider)
    {
        using var factory = LoggerFactory.Create(builder => builder.AddProvider(provider));
        AppLog.Initialize(factory);
    }

    private static void RestoreSilent()
    {
        using var factory = LoggerFactory.Create(static _ => { });
        AppLog.Initialize(factory);
    }

    [TestMethod]
    public void Information_RoutesThroughMLogger_WithRenderedArgs()
    {
        var provider = new ListLoggerProvider();
        try
        {
            UseProvider(provider);
            AppLog.Information("Hello {Name}", "world");
            Assert.HasCount(1, provider.Entries);
            Assert.AreEqual(LogLevel.Information, provider.Entries[0].Level);
            Assert.AreEqual("Hello world", provider.Entries[0].Message);
        }
        finally
        {
            RestoreSilent();
        }
    }

    [TestMethod]
    public void Error_PreservesException()
    {
        var provider = new ListLoggerProvider();
        var failure = new InvalidOperationException("boom");
        try
        {
            UseProvider(provider);
            AppLog.Error(failure, "GET {Url} failed", "https://x");
            Assert.HasCount(1, provider.Entries);
            Assert.AreEqual(LogLevel.Error, provider.Entries[0].Level);
            Assert.AreSame(failure, provider.Entries[0].Error);
        }
        finally
        {
            RestoreSilent();
        }
    }

    [TestMethod]
    public void Fatal_MapsToCritical()
    {
        var provider = new ListLoggerProvider();
        try
        {
            UseProvider(provider);
            AppLog.Fatal(new InvalidOperationException("x"), "fatal path");
            Assert.AreEqual(LogLevel.Critical, provider.Entries[0].Level);
        }
        finally
        {
            RestoreSilent();
        }
    }

    [TestMethod]
    public void NeverThrows_BeforeInitialize_NullFactory_NullMessage()
    {
        RestoreSilent();
        AppLog.Information("pre-init is silent");
        AppLog.Initialize(null);
        AppLog.Error(null, "null exception is fine");
        AppLog.Fatal("null factory was ignored");
    }
}
