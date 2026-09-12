using System;
using System.IO;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class ChannelResolverTests
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
    public void IsBetaVersion_DetectsSuffixCaseInsensitively()
    {
        Assert.IsTrue(ChannelResolver.IsBetaVersion("0.0.2-beta"));
        Assert.IsTrue(ChannelResolver.IsBetaVersion("1.0.0-BETA"));
        Assert.IsFalse(ChannelResolver.IsBetaVersion("0.0.2"));
        Assert.IsFalse(ChannelResolver.IsBetaVersion(null));
        Assert.IsFalse(ChannelResolver.IsBetaVersion(string.Empty));
    }

    [TestMethod]
    public void Normalize_KeepsOnlyRealChannels()
    {
        Assert.AreEqual("stable", ChannelResolver.Normalize("stable"));
        Assert.AreEqual("beta", ChannelResolver.Normalize("beta"));
        Assert.AreEqual("beta", ChannelResolver.Normalize("BETA"));
        // Legacy dev never had a feed: forward to beta so updates keep flowing.
        Assert.AreEqual("beta", ChannelResolver.Normalize("dev"));
        Assert.AreEqual("stable", ChannelResolver.Normalize("nightly"));
        Assert.AreEqual("stable", ChannelResolver.Normalize(null));
        Assert.AreEqual("stable", ChannelResolver.Normalize(string.Empty));
    }

    [TestMethod]
    public void ResolveDefaultChannel_PersistedChoiceAlwaysWins()
    {
        Assert.AreEqual("stable", ChannelResolver.ResolveDefaultChannel("stable", "0.0.2-beta"));
        Assert.AreEqual("beta", ChannelResolver.ResolveDefaultChannel("beta", "0.0.2"));
        Assert.AreEqual("beta", ChannelResolver.ResolveDefaultChannel("dev", "0.0.2"));
    }

    [TestMethod]
    public void ResolveDefaultChannel_FreshInstallFollowsBuild()
    {
        Assert.AreEqual("beta", ChannelResolver.ResolveDefaultChannel(null, "0.0.2-beta"));
        Assert.AreEqual("stable", ChannelResolver.ResolveDefaultChannel(null, "0.0.2"));
        // Empty counts as unset, so the build default applies.
        Assert.AreEqual("beta", ChannelResolver.ResolveDefaultChannel("", "0.0.2-beta"));
        Assert.AreEqual("stable", ChannelResolver.ResolveDefaultChannel("", "0.0.2"));
    }

    [TestMethod]
    public void EnsureChannelForCurrentBuild_MigratesStaleStableOnBetaBuild()
    {
        // Test assembly is a beta build (InformationalVersion 0.0.2-beta).
        Assert.IsTrue(AppInfo.Current.IsBetaBuild);

        SettingsService.Current.Channel = ChannelResolver.Stable;
        SettingsService.Current.EnsureChannelForCurrentBuild();
        Assert.AreEqual(ChannelResolver.Beta, SettingsService.Current.Channel);

        // Second run is a no-op (once per version).
        SettingsService.Current.EnsureChannelForCurrentBuild();
        Assert.AreEqual(ChannelResolver.Beta, SettingsService.Current.Channel);
    }

    [TestMethod]
    public void EnsureChannelForCurrentBuild_FreshInstallKeepsBuildDefault()
    {
        SettingsService.Current.EnsureChannelForCurrentBuild();
        Assert.AreEqual(AppInfo.Current.DefaultChannel, SettingsService.Current.Channel);
    }
    [TestMethod]
    public void AppInfo_DefaultChannelMatchesBuild()
    {
        // Tied to the build, not a fixed channel: stays green for both
        // stable and beta releases while pinning the delegation chain.
        bool beta = AppInfo.Current.InformationalVersion.Contains(
            "beta", System.StringComparison.OrdinalIgnoreCase);
        Assert.AreEqual(beta, AppInfo.Current.IsBetaBuild);
        Assert.AreEqual(beta ? "beta" : "stable", AppInfo.Current.DefaultChannel);
    }
}
