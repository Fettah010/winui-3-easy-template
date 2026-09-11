using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
[Ignore("Requires Windows App SDK runtime context (ApplicationData.Current)")]
public class WindowStateServiceTests
{
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
        Assert.IsGreaterThan(ws.Width, 0.0);
        Assert.IsGreaterThan(ws.Height, 0.0);
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
