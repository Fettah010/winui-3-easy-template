using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class StartupCrashTests
{
    [TestMethod]
    public void BuildStartupCrashMessage_ContainsLogDirectory()
    {
        string message = Program.BuildStartupCrashMessage(@"C:\logs\here");
        Assert.Contains(@"C:\logs\here", message);
    }

    [TestMethod]
    public void BuildStartupCrashMessage_FallsBack_OnBlankInput()
    {
        Assert.Contains("?", Program.BuildStartupCrashMessage(null));
        Assert.Contains("?", Program.BuildStartupCrashMessage("  "));
        Assert.AreEqual(
            Program.BuildStartupCrashMessage(null),
            Program.BuildStartupCrashMessage(string.Empty));
    }
}
