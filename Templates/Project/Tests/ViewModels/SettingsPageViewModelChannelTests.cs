using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DevTemWinUi3.Services;
using DevTemWinUi3.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.ViewModels;

[TestClass]
public class SettingsPageViewModelChannelTests
{
    private string _storePath = string.Empty;

    private sealed class TrackingUpdates : IUpdateService
    {
        public int SetChannelCalls;
        public string? LastChannel;
        public bool IsInstalled => true;
        public bool HasPendingUpdate => false;
        public string? PendingRestartVersion => null;

        public void SetChannel(string channel)
        {
            SetChannelCalls++;
            LastChannel = channel;
        }

        public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<UpdateCheckResult> CheckAsync() =>
            Task.FromResult(new UpdateCheckResult(false, null));

        public Task DownloadPendingUpdateAsync(Action<int>? progress = null) =>
            Task.CompletedTask;

        public void ApplyPendingUpdateAndRestart() { }
    }

    [TestInitialize]
    public void Init()
    {
        _storePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        LocalSettingsStore.SetTestPath(_storePath);
        LocalizationService.Current.SetLanguage("en-US");
    }

    [TestCleanup]
    public void Cleanup()
    {
        LocalizationService.Current.SetLanguage("en-US");
        LocalSettingsStore.SetTestPath(null);
        try { File.Delete(_storePath); } catch { }
    }

    [TestMethod]
    public void Construction_DoesNotPushChannelToEngine()
    {
        // Regression: building the VM pushed the default channel index
        // into the engine. Velopack drops its manager on a channel change,
        // so IsInstalled went false and the next manual check wrongly
        // reported "updates are only available for installed apps" on a
        // properly installed app.
        try { SettingsService.Current.Channel = ChannelResolver.Beta; } catch { }
        var updates = new TrackingUpdates();
        var vm = new SettingsPageViewModel(updates);
        Assert.AreEqual(0, updates.SetChannelCalls);
        Assert.AreEqual(1, vm.SelectedChannelIndex);
    }

    [TestMethod]
    public void UserChannelChange_PushesToEngine()
    {
        var updates = new TrackingUpdates();
        var vm = new SettingsPageViewModel(updates);
        Assert.AreEqual(0, updates.SetChannelCalls);
        vm.SelectedChannelIndex = vm.SelectedChannelIndex == 0 ? 1 : 0;
        Assert.AreEqual(1, updates.SetChannelCalls);
        Assert.IsNotNull(updates.LastChannel);
    }
}
