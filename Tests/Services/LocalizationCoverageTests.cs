using System;
using System.IO;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class LocalizationCoverageTests
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
        // Never leak a test language into other tests.
        LocalizationService.Current.SetLanguage("en-US");
        LocalSettingsStore.SetTestPath(null);
        try { File.Delete(_storePath); } catch { }
    }

    [TestMethod]
    public void NewHomeKeys_AreTranslated()
    {
        var loc = LocalizationService.Current;

        loc.SetLanguage("es-ES");
        Assert.AreEqual("Buscar actualizaciones", loc.GetString("HomeCheckUpdates"));
        Assert.AreEqual("ACCIONES RÁPIDAS", loc.GetString("HomeQuickActions"));

        loc.SetLanguage("fr-FR");
        Assert.AreEqual("Rechercher les mises à jour", loc.GetString("HomeCheckUpdates"));
        Assert.AreEqual("ACTIONS RAPIDES", loc.GetString("HomeQuickActions"));
    }

    [TestMethod]
    public void NewAboutKeys_AreTranslated()
    {
        var loc = LocalizationService.Current;

        loc.SetLanguage("es-ES");
        Assert.AreEqual("DE UN VISTAZO", loc.GetString("AboutGlance"));
        Assert.AreEqual("Ver licencia", loc.GetString("AboutViewLicense"));

        loc.SetLanguage("fr-FR");
        Assert.AreEqual("EN UN COUP D'ŒIL", loc.GetString("AboutGlance"));
        Assert.AreEqual("Voir la licence", loc.GetString("AboutViewLicense"));
    }

    [TestMethod]
    public void NewSettingsAndTrayKeys_AreTranslated()
    {
        var loc = LocalizationService.Current;

        loc.SetLanguage("es-ES");
        Assert.AreEqual("Canal de actualización", loc.GetString("SettingsChannelHeader"));
        Assert.AreEqual("Buscar actualizaciones", loc.GetString("TrayCheckUpdates"));
        Assert.AreEqual("Salir", loc.GetString("TrayExit"));

        loc.SetLanguage("fr-FR");
        Assert.AreEqual("Canal de mise à jour", loc.GetString("SettingsChannelHeader"));
        Assert.AreEqual("Rechercher les mises à jour", loc.GetString("TrayCheckUpdates"));
        Assert.AreEqual("Quitter", loc.GetString("TrayExit"));
    }

    [TestMethod]
    public void SetLanguage_RaisesLanguageChanged()
    {
        var loc = LocalizationService.Current;
        int raised = 0;
        void Handler(object? s, System.EventArgs e) => raised++;

        loc.LanguageChanged += Handler;
        try
        {
            loc.SetLanguage("fr-FR");
            Assert.AreEqual(1, raised);
            Assert.AreEqual("fr-FR", loc.CurrentLanguage);
        }
        finally
        {
            loc.LanguageChanged -= Handler;
        }
    }

    [TestMethod]
    public void DownloadProgress_FormatWorksInAllLanguages()
    {
        var loc = LocalizationService.Current;

        loc.SetLanguage("en-US");
        Assert.AreEqual("Downloading… 42%", loc.GetString("SettingsDownloadingProgress", 42));

        loc.SetLanguage("es-ES");
        Assert.AreEqual("Descargando… 42%", loc.GetString("SettingsDownloadingProgress", 42));

        loc.SetLanguage("fr-FR");
        Assert.AreEqual("Téléchargement… 42%", loc.GetString("SettingsDownloadingProgress", 42));
    }

    [TestMethod]
    public void SetLanguage_UnknownTag_KeepsCurrentLanguage()
    {
        var loc = LocalizationService.Current;
        loc.SetLanguage("es-ES");
        loc.SetLanguage("xx-XX");
        Assert.AreEqual("es-ES", loc.CurrentLanguage);
    }

    [TestMethod]
    public void SetLanguage_PersistsChoice_ForNextLaunch()
    {
        var loc = LocalizationService.Current;
        loc.SetLanguage("fr-FR");

        // A fresh read (as Initialize does on next launch) sees the choice.
        string? persisted = LocalSettingsStore.Shared.Get<string?>("AppLanguage", null);
        Assert.AreEqual("fr-FR", persisted);
    }
}
