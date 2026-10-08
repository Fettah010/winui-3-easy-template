using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

/// <summary>
/// MSAL backend tests. Ships only with the auth feature
/// (<c>!auth</c> excludes this file — it names backend types).
/// </summary>
[TestClass]
public class MsalAuthProviderTests
{
    [TestMethod]
    public void SplitScopes_ParsesSeparators_AndNeverThrows()
    {
        var scopes = MsalAuthProvider.SplitScopes("User.Read Mail.Read");
        Assert.HasCount(2, scopes);
        Assert.HasCount(0, MsalAuthProvider.SplitScopes(null));
        Assert.HasCount(0, MsalAuthProvider.SplitScopes("   "));
        Assert.HasCount(2, MsalAuthProvider.SplitScopes("User.Read;Mail.Read"));
    }

    [TestMethod]
    public void TryCreate_WithoutClientId_ReturnsNull()
    {
        // No client id is configured in the test host: the backend must
        // decline instead of constructing a half-wired client.
        Assert.IsNull(MsalAuthProvider.TryCreate());
    }
}
