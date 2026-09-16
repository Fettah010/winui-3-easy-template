using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class LocalSettingsStoreTests
{
    private string _storePath = string.Empty;
    private TimeSpan _previousDebounce;

    [TestInitialize]
    public void Init()
    {
        _storePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        LocalSettingsStore.SetTestPath(_storePath);
        _previousDebounce = LocalSettingsStore.WriteDebounce;
        // Deterministic durability for the round-trip tests below; the
        // coalescing tests set their own debounce explicitly.
        LocalSettingsStore.WriteDebounce = TimeSpan.Zero;
    }

    [TestCleanup]
    public void Cleanup()
    {
        LocalSettingsStore.WriteDebounce = _previousDebounce;
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

    [TestMethod]
    public async Task RapidSets_CoalesceToOneDiskWrite()
    {
        // P0-2: 20 rapid Sets produce a single debounced write. The poll
        // loop (not a fixed sleep) survives loaded machines: it waits for
        // the write to land, then asserts no second write follows.
        LocalSettingsStore.WriteDebounce = TimeSpan.FromMilliseconds(150);
        var store = new LocalSettingsStore(_storePath);
        for (int i = 0; i < 20; i++)
            store.Set("burst", i);
        bool saved = false;
        for (int i = 0; i < 200 && !saved; i++)
        {
            await Task.Delay(50);
            saved = store.CompletedWrites >= 1;
        }
        Assert.IsTrue(saved, "Debounced write never landed.");
        await Task.Delay(300);
        Assert.AreEqual(1, store.CompletedWrites);
        Assert.AreEqual(19, new LocalSettingsStore(_storePath).Get("burst", -1));
    }

    [TestMethod]
    public void Flush_PersistsPendingWrites()
    {
        // P0-2: Flush makes a debounced write durable on demand (exit path).
        LocalSettingsStore.WriteDebounce = TimeSpan.FromMinutes(5);
        var store = new LocalSettingsStore(_storePath);
        store.Set("pending", "yes");
        Assert.AreEqual(0, store.CompletedWrites);
        store.Flush();
        Assert.AreEqual(1, store.CompletedWrites);
        Assert.AreEqual("yes", new LocalSettingsStore(_storePath).Get("pending", "no"));
    }

    [TestMethod]
    public void Gets_AreMemoryOnly_AfterWarmup()
    {
        // P0-2: post-warmup Gets stay well under 1 ms on average.
        var store = new LocalSettingsStore(_storePath);
        store.Set("k", "v");
        var watch = Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++)
            _ = store.Get("k", string.Empty);
        watch.Stop();
        Assert.IsLessThan(1000, watch.Elapsed.TotalMilliseconds,
            $"10000 Gets took {watch.Elapsed.TotalMilliseconds:F0} ms");
    }

    [TestMethod]
    public void PreloadShared_DoesNotThrow()
    {
        LocalSettingsStore.PreloadShared();
        LocalSettingsStore.FlushShared();
    }
}
