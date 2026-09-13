using System;
using System.IO;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class FirstRunServiceTests
{
    private string _storePath = string.Empty;

    [TestInitialize]
    public void Init()
    {
        _storePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        LocalSettingsStore.SetTestPath(_storePath);
    }

    [TestCleanup]
    public void Cleanup()
    {
        LocalSettingsStore.SetTestPath(null);
        try { File.Delete(_storePath); } catch { }
    }
    [TestMethod]
    public void Current_ReturnsSingleton()
    {
        var a = FirstRunService.Current;
        var b = FirstRunService.Current;
        Assert.AreSame(a, b);
    }

    [TestMethod]
    public void LastShownVersion_ReturnsString()
    {
        var fr = FirstRunService.Current;
        var version = fr.LastShownVersion;
        Assert.IsNotNull(version);
    }

    [TestMethod]
    public void GetChangelog_ContainsVersion()
    {
        var fr = FirstRunService.Current;
        var changelog = fr.GetChangelog();
        Assert.IsFalse(string.IsNullOrWhiteSpace(changelog));
        Assert.Contains("What's New", changelog);
    }

    [TestMethod]
    public void MarkAsShown_DoesNotThrow()
    {
        var fr = FirstRunService.Current;
        fr.MarkAsShown();
        Assert.IsFalse(fr.IsFirstRun);
    }
}
