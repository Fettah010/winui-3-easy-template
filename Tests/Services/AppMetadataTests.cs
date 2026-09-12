using System.Linq;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class AppMetadataTests
{
    [TestMethod]
    public void Identity_IsCompleteAndConsistent()
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(AppMetadata.AppName));
        Assert.IsFalse(string.IsNullOrWhiteSpace(AppMetadata.Company));
        Assert.StartsWith("https://github.com/", AppMetadata.RepoUrl);
        Assert.IsFalse(string.IsNullOrWhiteSpace(AppMetadata.UserAgent));
    }

    [TestMethod]
    public void SafeName_IsIdentifierSafe()
    {
        // Usable in mutex names, registry values, and folder names.
        Assert.IsTrue(char.IsLetter(AppMetadata.SafeName[0]));
        Assert.IsTrue(AppMetadata.SafeName.All(char.IsLetterOrDigit));
        Assert.DoesNotContain(" ", AppMetadata.SafeName);
    }

    [TestMethod]
    public void ProcessWideNames_DeriveFromSafeName()
    {
        Assert.Contains(AppMetadata.SafeName, AppMetadata.SingleInstanceMutexName);
        Assert.Contains(AppMetadata.SafeName, AppMetadata.SingleInstanceEventName);
        Assert.AreEqual(AppMetadata.AppName, AppMetadata.TrayTooltip);
    }
}

[TestClass]
public class ThemeServiceTests
{
    [TestMethod]
    public void ToElementTheme_MapsKnownThemes()
    {
        Assert.AreEqual(Microsoft.UI.Xaml.ElementTheme.Light, ThemeService.ToElementTheme("Light"));
        Assert.AreEqual(Microsoft.UI.Xaml.ElementTheme.Dark, ThemeService.ToElementTheme("Dark"));
    }

    [TestMethod]
    public void ToElementTheme_UnknownHonorsSystem()
    {
        Assert.AreEqual(Microsoft.UI.Xaml.ElementTheme.Default, ThemeService.ToElementTheme("System"));
        Assert.AreEqual(Microsoft.UI.Xaml.ElementTheme.Default, ThemeService.ToElementTheme(null));
        Assert.AreEqual(Microsoft.UI.Xaml.ElementTheme.Default, ThemeService.ToElementTheme("neon"));
    }

    [TestMethod]
    public void Normalize_KeepsOnlyRealThemes()
    {
        Assert.AreEqual("Light", ThemeService.Normalize("Light"));
        Assert.AreEqual("Dark", ThemeService.Normalize("Dark"));
        Assert.AreEqual("System", ThemeService.Normalize("System"));
        Assert.AreEqual("System", ThemeService.Normalize("neon"));
        Assert.AreEqual("System", ThemeService.Normalize(null));
    }
}
