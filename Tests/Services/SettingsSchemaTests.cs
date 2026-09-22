using System.IO;
using System;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

/// <summary>
/// Advancement C4: validated-options layer — schema migrations run in
/// order with one test per migration, validation normalizes on read +
/// write, future stores are left untouched. Test-store isolated via
/// <c>LocalSettingsStore.SetTestPath</c> like every settings test.
/// </summary>
[TestClass]
public class SettingsSchemaTests
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
    public void EnsureMigrated_FreshStore_StampsCurrentVersion()
    {
        SettingsSchema.EnsureMigrated();

        Assert.AreEqual(SettingsSchema.CurrentVersion, LocalSettingsStore.Shared.Get("SettingsSchemaVersion", 0));
    }

    [TestMethod]
    public void MigrateToV1_RewritesLegacyDevChannel()
    {
        // Raw legacy value on disk (reads normalize, but the stored raw
        // value would otherwise live forever).
        LocalSettingsStore.Shared.Set("UpdateChannel", "dev");

        SettingsSchema.EnsureMigrated();

        Assert.AreEqual("beta", LocalSettingsStore.Shared.Get("UpdateChannel", string.Empty));
        Assert.AreEqual(SettingsSchema.CurrentVersion, LocalSettingsStore.Shared.Get("SettingsSchemaVersion", 0));
    }

    [TestMethod]
    public void EnsureMigrated_FutureStore_LeftUntouched()
    {
        // Never migrate down: a newer app's store must survive an older
        // binary without version-stomping.
        LocalSettingsStore.Shared.Set("SettingsSchemaVersion", SettingsSchema.CurrentVersion + 10);
        LocalSettingsStore.Shared.Set("UpdateChannel", "dev");

        SettingsSchema.EnsureMigrated();

        Assert.AreEqual(SettingsSchema.CurrentVersion + 10, LocalSettingsStore.Shared.Get("SettingsSchemaVersion", 0));
        Assert.AreEqual("dev", LocalSettingsStore.Shared.Get("UpdateChannel", string.Empty));
    }

    [TestMethod]
    public void Theme_NormalizesOnWriteAndRead()
    {
        var settings = SettingsService.Current;

        settings.Theme = "Neon";
        Assert.AreEqual("System", LocalSettingsStore.Shared.Get("AppTheme", string.Empty));
        Assert.AreEqual("System", settings.Theme);

        settings.Theme = "Dark";
        Assert.AreEqual("Dark", settings.Theme);

        // Legacy garbage already on disk self-heals on read.
        LocalSettingsStore.Shared.Set("AppTheme", "Neon");
        Assert.AreEqual("System", settings.Theme);
    }

    [TestMethod]
    public void EnsureSchemaCurrent_RunsMigrations()
    {
        LocalSettingsStore.Shared.Set("UpdateChannel", "dev");

        SettingsService.Current.EnsureSchemaCurrent();

        Assert.AreEqual("beta", LocalSettingsStore.Shared.Get("UpdateChannel", string.Empty));
        Assert.AreEqual(SettingsSchema.CurrentVersion, LocalSettingsStore.Shared.Get("SettingsSchemaVersion", 0));
    }
}
