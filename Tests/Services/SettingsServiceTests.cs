using System.IO;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class SettingsServiceTests
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
        LocalSettingsStore.SetTestPath(null);
        try { File.Delete(_storePath); } catch { }
    }

    [TestMethod]
    public void VerboseLogging_Default_MatchesBuildConfiguration()
    {
#if DEBUG
        Assert.IsTrue(SettingsService.Current.VerboseLogging);
#else
        Assert.IsFalse(SettingsService.Current.VerboseLogging);
#endif
    }

    [TestMethod]
    public void VerboseLogging_RoundTrips()
    {
        SettingsService.Current.VerboseLogging = true;
        Assert.IsTrue(SettingsService.Current.VerboseLogging);
        SettingsService.Current.VerboseLogging = false;
        Assert.IsFalse(SettingsService.Current.VerboseLogging);
    }

    [TestMethod]
    public void ResetToDefaults_RestoresVerboseDefault()
    {
        SettingsService.Current.VerboseLogging = false;
        SettingsService.Current.ResetToDefaults();
#if DEBUG
        Assert.IsTrue(SettingsService.Current.VerboseLogging);
#else
        Assert.IsFalse(SettingsService.Current.VerboseLogging);
#endif
    }

    [TestMethod]
    public void CrashReports_DefaultOff_AndRoundTrips()
    {
        Assert.IsFalse(SettingsService.Current.CrashReportsEnabled);
        SettingsService.Current.CrashReportsEnabled = true;
        Assert.IsTrue(SettingsService.Current.CrashReportsEnabled);
        SettingsService.Current.ResetToDefaults();
        Assert.IsFalse(SettingsService.Current.CrashReportsEnabled);
    }
}
