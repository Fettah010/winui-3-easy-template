using System;
using System.IO;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class LocalSettingsStoreTests
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
    public void Values_SurviveReload()
    {
        var store = new LocalSettingsStore(_storePath);
        store.Set("MinimizeToTray", false);
        store.Set("AppTheme", "Dark");

        var reloaded = new LocalSettingsStore(_storePath);
        Assert.IsFalse(reloaded.Get("MinimizeToTray", true));
        Assert.AreEqual("Dark", reloaded.Get("AppTheme", "System"));
    }

    [TestMethod]
    public void MissingKeys_ReturnDefaults()
    {
        var store = new LocalSettingsStore(_storePath);
        Assert.IsTrue(store.Get("MissingBool", true));
        Assert.AreEqual("d", store.Get("MissingString", "d"));
    }

    [TestMethod]
    public void SettingsService_RoundTrips_ThroughSharedStore()
    {
        SettingsService.Current.MinimizeToTray = false;
        Assert.IsFalse(new LocalSettingsStore(_storePath).Get("MinimizeToTray", true));
        SettingsService.Current.MinimizeToTray = true;
    }
}
