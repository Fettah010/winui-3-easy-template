using System.Linq;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class LocalizationServiceTests
{
    [TestMethod]
    public void Current_ReturnsSingleton()
    {
        var a = LocalizationService.Current;
        var b = LocalizationService.Current;
        Assert.AreSame(a, b);
    }

    [TestMethod]
    public void AvailableLanguages_ContainsThreeLanguages()
    {
        Assert.AreEqual(3, LocalizationService.AvailableLanguages.Count);
    }

    [TestMethod]
    public void AvailableLanguages_ContainsEnglish()
    {
        var langs = LocalizationService.AvailableLanguages;
        CollectionAssert.Contains(langs.Select(l => l.Tag).ToList(), "en-US");
    }

    [TestMethod]
    public void AvailableLanguages_ContainsSpanish()
    {
        var langs = LocalizationService.AvailableLanguages;
        CollectionAssert.Contains(langs.Select(l => l.Tag).ToList(), "es-ES");
    }

    [TestMethod]
    public void AvailableLanguages_ContainsFrench()
    {
        var langs = LocalizationService.AvailableLanguages;
        CollectionAssert.Contains(langs.Select(l => l.Tag).ToList(), "fr-FR");
    }

    [TestMethod]
    public void LanguageInfo_HasNativeName()
    {
        var lang = LocalizationService.AvailableLanguages[0];
        Assert.IsFalse(string.IsNullOrEmpty(lang.NativeName));
    }

    [TestMethod]
    public void LanguageInfo_HasEnglishName()
    {
        var lang = LocalizationService.AvailableLanguages[0];
        Assert.IsFalse(string.IsNullOrEmpty(lang.EnglishName));
    }

    [TestMethod]
    public void LanguageInfo_ToString_ReturnsNativeName()
    {
        var lang = LocalizationService.AvailableLanguages[0];
        Assert.AreEqual(lang.NativeName, lang.ToString());
    }
}
