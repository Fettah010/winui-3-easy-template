using System;
using System.IO;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class WindowStateServiceTests
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
        var a = WindowStateService.Current;
        var b = WindowStateService.Current;
        Assert.AreSame(a, b);
    }

    [TestMethod]
    public void Defaults_AreValid()
    {
        var ws = WindowStateService.Current;
        Assert.IsGreaterThan(0.0, ws.Width);
        Assert.IsGreaterThan(0.0, ws.Height);
    }

    [TestMethod]
    public void SetAndGet_Width()
    {
        var ws = WindowStateService.Current;
        var original = ws.Width;
        ws.Width = 999;
        Assert.AreEqual(999, ws.Width);
        ws.Width = original;
    }

    [TestMethod]
    public void SetAndGet_Height()
    {
        var ws = WindowStateService.Current;
        var original = ws.Height;
        ws.Height = 777;
        Assert.AreEqual(777, ws.Height);
        ws.Height = original;
    }

    [TestMethod]
    public void SetAndGet_IsMaximized()
    {
        var ws = WindowStateService.Current;
        var original = ws.IsMaximized;
        ws.IsMaximized = true;
        Assert.IsTrue(ws.IsMaximized);
        ws.IsMaximized = original;
    }
}
