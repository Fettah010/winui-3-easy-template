using System;
using System.IO;
using System.Threading.Tasks;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class AuthServiceTests
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
        AuthService.Current.Shutdown();
        LocalSettingsStore.SetTestPath(null);
        try { File.Delete(_storePath); } catch { }
    }

    [TestMethod]
    public void Disabled_ByDefault_EmptyClientId()
    {
        // Template default: no client id, so nothing can ever sign in.
        AuthService.Current.Shutdown();
        AuthService.Current.Initialize();
        Assert.IsFalse(AuthService.Current.IsEnabled);
        Assert.IsNull(AuthService.Current.CurrentAccount);
    }

    [TestMethod]
    public void Initialize_IsIdempotent_AndNeverThrows()
    {
        AuthService.Current.Shutdown();
        AuthService.Current.Initialize();
        AuthService.Current.Initialize();
        AuthService.Current.Initialize(null);
        Assert.IsFalse(AuthService.Current.IsEnabled);
    }

    [TestMethod]
    public async Task SignIn_WhenDisabled_ReturnsNullWithoutThrowing()
    {
        AuthService.Current.Shutdown();
        AuthService.Current.Initialize();
        Assert.IsNull(await AuthService.Current.SignInAsync());
    }

    [TestMethod]
    public async Task TryResume_WhenDisabled_ReturnsNullWithoutThrowing()
    {
        AuthService.Current.Shutdown();
        Assert.IsNull(await AuthService.Current.TryResumeAsync());
    }

    [TestMethod]
    public async Task SignOut_WhenDisabled_NeverThrows()
    {
        AuthService.Current.Shutdown();
        await AuthService.Current.SignOutAsync();
        await AuthService.Current.SignOutAsync();
    }

    [TestMethod]
    public void Shutdown_NeverThrows()
    {
        AuthService.Current.Shutdown();
        AuthService.Current.Shutdown();
    }

    [TestMethod]
    public void CacheFilePathFor_LivesUnderTheDataFolder()
    {
        string dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        string path = AuthService.CacheFilePathFor(dir);
        Assert.AreEqual(Path.Combine(dir, "msal-cache.dat"), path);
    }

    [TestMethod]
    public void BuildStatusContext_CarriesNoIdentityValues()
    {
        // Booleans only: signed-in state without usernames, ids, or errors.
        var context = AuthService.BuildStatusContext();
        Assert.IsTrue(context.ContainsKey("authEnabled"));
        Assert.IsTrue(context.ContainsKey("signedIn"));
        foreach (string value in context.Values)
        {
            Assert.IsTrue(value is "yes" or "no", "Unexpected status value: " + value);
        }
    }

    [TestMethod]
    public void AuthAccount_CarriesUsername()
    {
        var account = new DevTemWinUi3.Services.Abstractions.AuthAccount("user@example.com", "User");
        Assert.AreEqual("user@example.com", account.Username);
        Assert.AreEqual("User", account.DisplayName);
    }
}
