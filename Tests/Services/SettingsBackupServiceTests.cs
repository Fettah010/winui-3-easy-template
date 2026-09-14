using System.IO;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class SettingsBackupServiceTests
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
        LocalizationService.Current.SetLanguage("en-US");
        LocalSettingsStore.SetTestPath(null);
        try { File.Delete(_storePath); } catch { }
    }

    [TestMethod]
    public void ExportImport_RoundTripsPreferences()
    {
        // English-only scaffolds have a single language: round-trip it
        // instead of Spanish/French (the persistence wiring is identical).
        string first = LocalizationService.AvailableLanguages.Count > 1 ? "es-ES" : "en-US";
        string second = LocalizationService.AvailableLanguages.Count > 1 ? "fr-FR" : "en-US";
        var settings = SettingsService.Current;
        settings.Theme = "Dark";
        settings.Channel = ChannelResolver.Beta;
        settings.MinimizeToTray = false;
        settings.AutoCheck = false;
        LocalizationService.Current.SetLanguage(first);

        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");
        try
        {
            Assert.IsTrue(SettingsBackupService.ExportToFile(path));

            // Change everything, then restore from the file.
            settings.Theme = "Light";
            settings.Channel = ChannelResolver.Stable;
            settings.MinimizeToTray = true;
            settings.AutoCheck = true;
            LocalizationService.Current.SetLanguage(second);

            Assert.IsTrue(SettingsBackupService.ImportFromFile(path));
            Assert.AreEqual("Dark", settings.Theme);
            Assert.AreEqual(ChannelResolver.Beta, settings.Channel);
            Assert.IsFalse(settings.MinimizeToTray);
            Assert.IsFalse(settings.AutoCheck);
            Assert.AreEqual(first, LocalizationService.Current.CurrentLanguage);
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }

    [TestMethod]
    public void Import_MissingFile_ReturnsFalse()
    {
        Assert.IsFalse(SettingsBackupService.ImportFromFile(
            Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json")));
    }

    [TestMethod]
    public void Import_CorruptFile_ReturnsFalse()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");
        try
        {
            File.WriteAllText(path, "{not json");
            Assert.IsFalse(SettingsBackupService.ImportFromFile(path));
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }

    [TestMethod]
    public void Import_PartialFile_KeepsCurrentValues()
    {
        SettingsService.Current.Theme = "Dark";
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");
        try
        {
            File.WriteAllText(path, """{"channel": "beta"}""");
            Assert.IsTrue(SettingsBackupService.ImportFromFile(path));
            Assert.AreEqual("Dark", SettingsService.Current.Theme);
            Assert.AreEqual(ChannelResolver.Beta, SettingsService.Current.Channel);
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }

    [TestMethod]
    public void Import_UnknownValues_NormalizeToDefaults()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");
        try
        {
            File.WriteAllText(path, """{"theme": "neon", "channel": "dev", "language": "xx-XX"}""");
            Assert.IsTrue(SettingsBackupService.ImportFromFile(path));
            Assert.AreEqual("System", SettingsService.Current.Theme);
            Assert.AreEqual(ChannelResolver.Beta, SettingsService.Current.Channel);
            Assert.AreEqual("en-US", LocalizationService.Current.CurrentLanguage);
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }

    [TestMethod]
    public void ResetAll_RestoresDefaults()
    {
        SettingsService.Current.Theme = "Dark";
        SettingsService.Current.MinimizeToTray = false;
        LocalizationService.Current.SetLanguage("fr-FR");

        SettingsBackupService.ResetAll();

        Assert.AreEqual("System", SettingsService.Current.Theme);
        Assert.IsTrue(SettingsService.Current.MinimizeToTray);
        Assert.IsTrue(SettingsService.Current.AutoCheck);
        Assert.AreEqual("en-US", LocalizationService.Current.CurrentLanguage);
    }
}
