using System.Threading.Tasks;
using DevTemWinUi3.Services;
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
}
