using System.IO;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class AppIconServiceTests
{
    [TestMethod]
    public void ResolveIconFileName_PicksVariant()
    {
        Assert.AreEqual("app-dark.ico", AppIconService.ResolveIconFileName(true));
        Assert.AreEqual("app-light.ico", AppIconService.ResolveIconFileName(false));
    }

    [TestMethod]
    public void IsDark_MatchesActualTheme()
    {
        Assert.IsTrue(AppIconService.IsDark(Microsoft.UI.Xaml.ElementTheme.Dark));
        Assert.IsFalse(AppIconService.IsDark(Microsoft.UI.Xaml.ElementTheme.Light));
        Assert.IsFalse(AppIconService.IsDark(Microsoft.UI.Xaml.ElementTheme.Default));
    }

    [TestMethod]
    public void ResolveIconPath_PrefersVariant_WhenPresent()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var assets = Path.Combine(dir, "Assets");
        Directory.CreateDirectory(assets);
        try
        {
            File.WriteAllText(Path.Combine(assets, "app.ico"), "base");
            File.WriteAllText(Path.Combine(assets, "app-dark.ico"), "dark");
            File.WriteAllText(Path.Combine(assets, "app-light.ico"), "light");

            Assert.AreEqual(
                Path.Combine(assets, "app-dark.ico"),
                AppIconService.ResolveIconPath(dir, true));
            Assert.AreEqual(
                Path.Combine(assets, "app-light.ico"),
                AppIconService.ResolveIconPath(dir, false));
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }

    [TestMethod]
    public void ResolveIconPath_FallsBack_WhenVariantMissing()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var assets = Path.Combine(dir, "Assets");
        Directory.CreateDirectory(assets);
        try
        {
            File.WriteAllText(Path.Combine(assets, "app.ico"), "base");

            Assert.AreEqual(
                Path.Combine(assets, "app.ico"),
                AppIconService.ResolveIconPath(dir, true));
            Assert.AreEqual(
                Path.Combine(assets, "app.ico"),
                AppIconService.ResolveIconPath(dir, false));
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }
}
