using System;
using System.IO;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class CrashReportingTests
{
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
        try { SettingsService.Current.CrashReportsEnabled = false; } catch { }
        CrashReportingService.Current.Shutdown();
        LocalSettingsStore.SetTestPath(null);
        try { File.Delete(_storePath); } catch { }
    }
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

    [TestMethod]
    public void OptIn_DefaultOff()
    {
        Assert.IsFalse(SettingsService.Current.CrashReportsEnabled);
    }

    [TestMethod]
    public void AddBreadcrumb_WhenDisabled_NeverThrows()
    {
        CrashReportingService.Current.Shutdown();
        CrashReportingService.AddBreadcrumb("nav test", "navigation");
        CrashReportingService.AddBreadcrumb(string.Empty);
        CrashReportingService.AddBreadcrumb(null!);
    }

    [TestMethod]
    public void BuildStatusContext_ContainsVersion()
    {
        var context = CrashReportingService.BuildStatusContext();
        Assert.IsTrue(context.ContainsKey("version"));
        Assert.IsFalse(string.IsNullOrWhiteSpace(context["version"]));
        Assert.IsTrue(context.ContainsKey("channel"));
    }
}
