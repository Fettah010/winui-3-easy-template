using System;
using System.IO;
using System.Threading.Tasks;
using DevTemWinUi3.Services;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class DatabaseServiceTests
{
    [TestMethod]
    public void Current_ReturnsSingleton()
    {
        var a = DatabaseService.Current;
        var b = DatabaseService.Current;
        Assert.AreSame(a, b);
    }

    [TestMethod]
    public void DatabasePath_ContainsAppName()
    {
        Assert.Contains("app.db", DatabaseService.Current.DatabasePath);
    }

    [TestMethod]
    public async Task InitializeAsync_CreatesDatabaseFile()
    {
        var db = DatabaseService.Current;
        await db.InitializeAsync();
        Assert.IsTrue(System.IO.File.Exists(db.DatabasePath));
    }

    [TestMethod]
    public async Task SetAndGetSetting_Works()
    {
        var db = DatabaseService.Current;
        await db.InitializeAsync();
        await db.SetSettingAsync("test-key", "test-value");
        var result = await db.GetSettingAsync("test-key");
        Assert.AreEqual("test-value", result);
    }

    [TestMethod]
    public async Task GetSettingAsync_ReturnsNullForMissingKey()
    {
        var db = DatabaseService.Current;
        await db.InitializeAsync();
        var result = await db.GetSettingAsync("nonexistent-key");
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task ExecuteAsync_Works()
    {
        var db = DatabaseService.Current;
        await db.InitializeAsync();
        await db.ExecuteAsync("INSERT OR REPLACE INTO Settings (Key, Value) VALUES ('k', 'v')");
        var result = await db.ExecuteScalarAsync<string>("SELECT Value FROM Settings WHERE Key = 'k'");
        Assert.AreEqual("v", result);
    }
    [TestMethod]
    public void QueryGuards_HaveSaneDefaults()
    {
        // D4: the hard guard must be positive (a wedged query fails), the
        // tripwire above zero (local answers land in ms).
        Assert.IsGreaterThan(TimeSpan.Zero, DatabaseService.QueryTimeout);
        Assert.IsGreaterThan(TimeSpan.Zero, DatabaseService.SlowQueryThreshold);
    }

    [TestMethod]
    public void QueryGuards_RejectNonPositiveValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DatabaseService.QueryTimeout = TimeSpan.Zero);
        Assert.Throws<ArgumentOutOfRangeException>(() => DatabaseService.SlowQueryThreshold = TimeSpan.FromSeconds(-1));
    }

    [TestMethod]
    public async Task GuardedQuery_Works()
    {
        // Guard plumbing must not break the normal path.
        var db = DatabaseService.Current;
        await db.InitializeAsync();
        await db.SetSettingAsync("guard-key", "guard-value");
        Assert.AreEqual("guard-value", await db.GetSettingAsync("guard-key"));
    }

    [TestMethod]

    public async Task Migrations_ApplyInOrder_AndAreIdempotent()
    {
        // P1-3: fresh DBs land on the current schema version; re-running
        // applies nothing (kill-during-init safety).
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".db");
        try
        {
            var db = new DatabaseService(path);
            await db.InitializeAsync();
            Assert.IsTrue(db.IsInitialized);
            Assert.AreEqual(DatabaseService.CurrentSchemaVersion,
                await ReadVersionAsync(path));

            var again = new DatabaseService(path);
            await again.InitializeAsync();
            Assert.IsTrue(again.IsInitialized);
            Assert.AreEqual(DatabaseService.CurrentSchemaVersion,
                await ReadVersionAsync(path));
        }
        finally
        {
            TryDelete(path);
        }
    }

    [TestMethod]
    public async Task FailedFirstInit_Retries_AndRecovers()
    {
        // P1-3: a failed first init no longer latches success — the next
        // call after the cause is gone succeeds.
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path); // a directory where the file goes: open fails
        try
        {
            var broken = new DatabaseService(path);
            await broken.InitializeAsync();
            Assert.IsFalse(broken.IsInitialized);

            Directory.Delete(path);
            var recovered = new DatabaseService(path);
            await recovered.InitializeAsync();
            Assert.IsTrue(recovered.IsInitialized);
            Assert.IsTrue(File.Exists(path));
        }
        finally
        {
            TryDelete(path);
        }
    }

    private static async Task<int> ReadVersionAsync(string path)
    {
        await using var connection = new SqliteConnection($"Data Source={path}");
        await connection.OpenAsync();
        return await DatabaseService.ReadSchemaVersionAsync(connection);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
            else if (Directory.Exists(path))
                Directory.Delete(path, true);
            foreach (var extra in new[] { path + "-wal", path + "-shm", path + "-journal" })
            {
                try { if (File.Exists(extra)) File.Delete(extra); } catch { }
            }
        }
        catch { }
    }
}
