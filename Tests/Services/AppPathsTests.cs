using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class AppPathsTests
{
    [TestMethod]
    public void IsPackaged_IsFalseInTestRunner()
    {
        // Unit tests always run unpackaged: the packaged branches below
        // are OS-touching and stay untested by house rule (never-throw
        // guarded, proven by the packaged install checklist instead).
        Assert.IsFalse(AppInfo.IsPackaged);
    }

    [TestMethod]
    public void DataFolder_UnpackagedLivesUnderLocalAppData()
    {
        var expected = System.IO.Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            AppMetadata.AppDataFolder);
        Assert.AreEqual(expected, AppPaths.DataFolder);
    }

    [TestMethod]
    public void SettingsDefaultPath_LivesUnderDataFolder()
    {
        Assert.AreEqual(
            System.IO.Path.Combine(AppPaths.DataFolder, "settings.json"),
            LocalSettingsStore.DefaultPath);
    }

    [TestMethod]
    public void StartupTaskId_DerivesFromSafeName()
    {
        // Matches the manifest TaskId (renamed by the template engines
        // alongside the safe name).
        Assert.AreEqual(AppMetadata.SafeName + "Startup", AutoStartService.StartupTaskId);
    }

    [TestMethod]
    public void GetPackagedProtocolUri_IsNullWhenUnpackaged()
    {
        Assert.IsNull(ProtocolService.GetPackagedProtocolUri());
    }
}
