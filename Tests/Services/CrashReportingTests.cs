using System;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class CrashReportingTests
{
    [TestMethod]
    public void Disabled_ByDefault_EmptyDsn()
    {
        // Template default: no DSN, so nothing is ever sent.
        Assert.IsTrue(string.IsNullOrEmpty(AppMetadata.SentryDsn));
        CrashReportingService.Current.Shutdown();
        Assert.IsFalse(CrashReportingService.Current.IsEnabled);
    }

    [TestMethod]
    public void Initialize_WithoutDsn_IsNoop()
    {
        CrashReportingService.Current.Shutdown();
        CrashReportingService.Current.Initialize();
        Assert.IsFalse(CrashReportingService.Current.IsEnabled);
        CrashReportingService.Current.Initialize();
        Assert.IsFalse(CrashReportingService.Current.IsEnabled);
    }

    [TestMethod]
    public void CaptureException_WhenDisabled_NeverThrows()
    {
        CrashReportingService.Current.Shutdown();
        CrashReportingService.Current.CaptureException(new InvalidOperationException("test"));
        CrashReportingService.Current.CaptureException(new InvalidOperationException("test"), "test-context");
        CrashReportingService.Current.CaptureException(null);
    }

    [TestMethod]
    public void Shutdown_NeverThrows()
    {
        CrashReportingService.Current.Shutdown();
        CrashReportingService.Current.Shutdown();
    }
}
