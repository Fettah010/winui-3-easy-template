using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DevTemWinUi3.Services;
using DevTemWinUi3.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.ViewModels;

[TestClass]
public class SettingsUpdateFlowTests
{
    private string _storePath = string.Empty;

    /// <summary>Update-service fake: script IsInstalled/results, count calls.</summary>
    private sealed class FakeUpdates : IUpdateService
    {
        public bool IsInstalled { get; set; } = true;
        public bool HasPendingUpdate { get; set; }
        public string? PendingRestartVersion => null;
        public UpdateCheckResult Result { get; set; } = new(false, null);
        public Exception? CheckError;
        public int CheckCalls;
        public int DownloadCalls;
        public int ApplyCalls;
        public TaskCompletionSource? CheckGate;

        public void SetChannel(string channel) { }

        public Task<UpdateCheckResult> CheckAsync()
        {
            CheckCalls++;
            if (CheckError is not null)
                throw CheckError;
            HasPendingUpdate = Result.HasUpdate;
            if (CheckGate is not null)
                return CheckGate.Task.ContinueWith(_ => Result);
            return Task.FromResult(Result);
        }

        public Task DownloadPendingUpdateAsync(Action<int>? progress = null)
        {
            DownloadCalls++;
            return Task.CompletedTask;
        }

        public void ApplyPendingUpdateAndRestart() { ApplyCalls++; }
    }

    /// <summary>Picker fake: script the picked paths (null = cancel).</summary>
    private sealed class FakePickers : IFilePickerService
    {
        public string? SavePath;
        public string? OpenPath;

        public Task<string?> PickSaveFileAsync(string suggestedFileName, string fileExtension = ".json") =>
            Task.FromResult(SavePath);

        public Task<string?> PickOpenFileAsync() =>
            Task.FromResult(OpenPath);
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
        // Never leak state into other tests (statics are process-wide).
        SettingsBackupService.ResetAll();
        LocalizationService.Current.SetLanguage("en-US");
        LocalSettingsStore.SetTestPath(null);
        try { File.Delete(_storePath); } catch { }
    }

    [TestMethod]
    public async Task Check_NotInstalled_LeavesCardHidden()
    {
        if (AppFeatures.IsExternalUpdateMode)
            Assert.Inconclusive("Engine flow is not scaffolded in external update mode.");
        var vm = new SettingsPageViewModel(new FakeUpdates { IsInstalled = false });

        await vm.CheckForUpdatesAsync();

        Assert.AreEqual(Visibility.Collapsed, vm.UpdateCardVisibility);
        Assert.IsTrue(vm.IsCheckUpdatesEnabled);
    }

    [TestMethod]
    public async Task Check_NoUpdate_ShowsStatusCard()
    {
        if (AppFeatures.IsExternalUpdateMode)
            Assert.Inconclusive("Engine flow is not scaffolded in external update mode.");
        var vm = new SettingsPageViewModel(new FakeUpdates());

        await vm.CheckForUpdatesAsync();

        Assert.AreEqual(Visibility.Visible, vm.UpdateCardVisibility);
        Assert.AreEqual("You are running the latest version.", vm.UpdateStatusMessage);
        Assert.AreEqual(Visibility.Collapsed, vm.InstallButtonVisibility);
        Assert.IsTrue(vm.IsCheckUpdatesEnabled);
    }

    [TestMethod]
    public async Task Check_UpdateAvailable_ShowsInstall()
    {
        if (AppFeatures.IsExternalUpdateMode)
            Assert.Inconclusive("Engine flow is not scaffolded in external update mode.");
        var vm = new SettingsPageViewModel(
            new FakeUpdates { Result = new UpdateCheckResult(true, "9.9") });

        await vm.CheckForUpdatesAsync();

        Assert.AreEqual("v9.9 available", vm.UpdateStatusMessage);
        Assert.AreEqual(Visibility.Visible, vm.InstallButtonVisibility);
        Assert.IsTrue(vm.IsInstallEnabled);
        Assert.AreEqual("Install", vm.InstallButtonText);
    }

    [TestMethod]
    public async Task Check_Failure_ShowsError()
    {
        if (AppFeatures.IsExternalUpdateMode)
            Assert.Inconclusive("Engine flow is not scaffolded in external update mode.");
        var vm = new SettingsPageViewModel(
            new FakeUpdates { CheckError = new InvalidOperationException("boom") });

        await vm.CheckForUpdatesAsync();

        Assert.Contains("Check failed", vm.UpdateStatusMessage);
        Assert.AreEqual(Visibility.Collapsed, vm.InstallButtonVisibility);
        Assert.IsTrue(vm.IsCheckUpdatesEnabled);
    }

    [TestMethod]
    public async Task Check_ConcurrentCalls_RunOnce()
    {
        if (AppFeatures.IsExternalUpdateMode)
            Assert.Inconclusive("Engine flow is not scaffolded in external update mode.");
        var updates = new FakeUpdates { CheckGate = new TaskCompletionSource() };
        var vm = new SettingsPageViewModel(updates);

        var first = vm.CheckForUpdatesAsync();
        await Task.Delay(100);
        var second = vm.CheckForUpdatesAsync();
        updates.CheckGate.SetResult();
        await Task.WhenAll(first, second);

        Assert.AreEqual(1, updates.CheckCalls);
    }

    [TestMethod]
    public async Task Check_Cancelled_ResetsButtonQuietly()
    {
        if (AppFeatures.IsExternalUpdateMode)
            Assert.Inconclusive("Engine flow is not scaffolded in external update mode.");
        var updates = new FakeUpdates();
        var vm = new SettingsPageViewModel(updates);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await vm.CheckForUpdatesAsync(cts.Token);

        Assert.AreEqual(0, updates.CheckCalls);
        Assert.IsTrue(vm.IsCheckUpdatesEnabled);
        Assert.AreEqual(Visibility.Collapsed, vm.UpdateCardVisibility);
    }

    [TestMethod]
    public async Task Install_WithoutPendingUpdate_NoOp()
    {
        if (AppFeatures.IsExternalUpdateMode)
            Assert.Inconclusive("Engine flow is not scaffolded in external update mode.");
        var vm = new SettingsPageViewModel(new FakeUpdates { HasPendingUpdate = false });

        await vm.InstallPendingUpdateAsync();

        Assert.AreEqual(string.Empty, vm.UpdateStatusMessage);
        Assert.AreEqual(Visibility.Collapsed, vm.DownloadProgressVisibility);
    }

    [TestMethod]
    public async Task External_RestingState_ShowsHandlerAndAction()
    {
        if (!AppFeatures.IsExternalUpdateMode)
            Assert.Inconclusive("External flow is not scaffolded in engine update mode.");
        var vm = new SettingsPageViewModel(new FakeUpdates());

        Assert.AreEqual(Visibility.Collapsed, vm.UpdateCheckCardVisibility);
        Assert.AreEqual(Visibility.Visible, vm.UpdateCardVisibility);
        Assert.IsFalse(string.IsNullOrWhiteSpace(vm.UpdateCardDescription));
        Assert.AreEqual(Visibility.Visible, vm.InstallButtonVisibility);
        Assert.IsFalse(string.IsNullOrWhiteSpace(vm.InstallButtonText));
        Assert.IsTrue(vm.IsInstallEnabled);
    }

    [TestMethod]
    public async Task External_CheckRoutesToOwnerWithoutTouchingService()
    {
        if (!AppFeatures.IsExternalUpdateMode)
            Assert.Inconclusive("External flow is not scaffolded in engine update mode.");
        var updates = new FakeUpdates();
        var vm = new SettingsPageViewModel(updates);

        await vm.CheckForUpdatesAsync();

        Assert.AreEqual(0, updates.CheckCalls);
        Assert.IsTrue(vm.IsCheckUpdatesEnabled);
    }

    [TestMethod]
    public async Task External_InstallRoutesToOwnerWithoutDownloading()
    {
        if (!AppFeatures.IsExternalUpdateMode)
            Assert.Inconclusive("External flow is not scaffolded in engine update mode.");
        var updates = new FakeUpdates { HasPendingUpdate = true };
        var vm = new SettingsPageViewModel(updates);

        await vm.InstallPendingUpdateAsync();

        Assert.AreEqual(0, updates.DownloadCalls);
        Assert.AreEqual(0, updates.ApplyCalls);
    }

    [TestMethod]
    public async Task ExportCommand_WritesPickedFile()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            var vm = new SettingsPageViewModel(
                new FakeUpdates(), new FakePickers { SavePath = path });

            await vm.ExportSettingsCommand.ExecuteAsync(null);

            Assert.IsTrue(File.Exists(path));
            Assert.Contains("theme", File.ReadAllText(path));
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }

    [TestMethod]
    public async Task ImportCommand_AppliesPickedFile()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            File.WriteAllText(path,
                """{"theme":"Dark","channel":"stable","minimizeToTray":true,"autoCheck":true,"language":"en-US"}""");
            var vm = new SettingsPageViewModel(
                new FakeUpdates(), new FakePickers { OpenPath = path });

            await vm.ImportSettingsCommand.ExecuteAsync(null);

            Assert.AreEqual(2, vm.SelectedThemeIndex);
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }
}
