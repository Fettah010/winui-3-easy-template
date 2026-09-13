using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class TrayNavigationRequestTests
{
    [TestMethod]
    public void ShouldAutoCheck_CheckUpdatesString_ReturnsTrue()
    {
        Assert.IsTrue(TrayNavigationRequest.ShouldAutoCheck("check-updates"));
        Assert.IsTrue(TrayNavigationRequest.ShouldAutoCheck("Check-Updates"));
        Assert.IsTrue(TrayNavigationRequest.ShouldAutoCheck("CHECK-UPDATES"));
    }

    [TestMethod]
    public void ShouldAutoCheck_BoolTrue_ReturnsTrue()
    {
        Assert.IsTrue(TrayNavigationRequest.ShouldAutoCheck(true));
    }

    [TestMethod]
    public void ShouldAutoCheck_RequestWithFlag_ReturnsFlag()
    {
        Assert.IsTrue(TrayNavigationRequest.ShouldAutoCheck(new TrayNavigationRequest("settings", true)));
        Assert.IsFalse(TrayNavigationRequest.ShouldAutoCheck(new TrayNavigationRequest("settings", false)));
    }

    [TestMethod]
    public void ShouldAutoCheck_NullOrOther_ReturnsFalse()
    {
        Assert.IsFalse(TrayNavigationRequest.ShouldAutoCheck(null));
        Assert.IsFalse(TrayNavigationRequest.ShouldAutoCheck(false));
        Assert.IsFalse(TrayNavigationRequest.ShouldAutoCheck("settings"));
        Assert.IsFalse(TrayNavigationRequest.ShouldAutoCheck(string.Empty));
        Assert.IsFalse(TrayNavigationRequest.ShouldAutoCheck(42));
    }

    [TestMethod]
    public void ToParameter_AutoCheckTrue_ReturnsCheckUpdatesParameter()
    {
        var req = new TrayNavigationRequest("settings", true);
        Assert.AreEqual(TrayNavigationRequest.CheckUpdatesParameter, req.ToParameter());
    }

    [TestMethod]
    public void ToParameter_AutoCheckFalse_ReturnsNull()
    {
        var req = new TrayNavigationRequest("settings", false);
        Assert.IsNull(req.ToParameter());
    }

    [TestMethod]
    public void CheckUpdatesParameter_RoundTripsThroughShouldAutoCheck()
    {
        var req = new TrayNavigationRequest("settings", true);
        var parameter = req.ToParameter();
        Assert.IsTrue(TrayNavigationRequest.ShouldAutoCheck(parameter));
    }
}
