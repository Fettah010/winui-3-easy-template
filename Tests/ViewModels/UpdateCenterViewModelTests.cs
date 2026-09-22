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
public class UpdateCenterViewModelTests
{
    private string _storePath = string.Empty;

    private sealed class FakeUpdates : IUpdateService
    {
        public bool IsInstalled { get; set; } = true;
        public bool HasPendingUpdate { get; set; }
        public string? PendingRestartVersion => null;
        public UpdateCheckResult Result { get; set; } = new(false, null);
        public Exception? CheckError;
        public TaskCompletionSource? CheckGate;
        public int ProgressToReport = 100;
        public int ApplyCalls;
        public int CheckCalls;
        public Exception? DownloadError;

        public void SetChannel(string channel) { }

        public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public async Task<UpdateCheckResult> CheckAsync()
        {
            CheckCalls++;
            if (CheckGate is not null)
                await CheckGate.Task;
            if (CheckError is not null)
                throw CheckError;
            HasPendingUpdate = Result.HasUpdate;
            return Result;
        }

        public Task DownloadPendingUpdateAsync(Action<int>? progress = null)
        {
            progress?.Invoke(0);
            progress?.Invoke(ProgressToReport);
            if (DownloadError is not null)
                throw DownloadError;
            return Task.CompletedTask;
        }

        public void ApplyPendingUpdateAndRestart() { ApplyCalls++; }
    }

    [TestInitialize]
    public void Init()
    {
        _storePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        LocalSettingsStore.SetTestPath(_storePath);
        LocalizationService.Current.SetLanguage("en-US");
        UpdateCenterViewModel.ResetStickyForTests();
    }

    [TestCleanup]
    public void Cleanup()
    {
        LocalizationService.Current.SetLanguage("en-US");
        LocalSettingsStore.SetTestPath(null);
        try { File.Delete(_storePath); } catch { }
    }

    [TestMethod]
    public void NoEngine_RestingState_ReportsMode()
    {
        if (AppFeatures.IsExternalUpdateMode)
            Assert.Inconclusive("No-engine flow is not scaffolded in external update mode.");
        var vm = new UpdateCenterViewModel(null);

        Assert.AreEqual("Automatic updates are not included in this build.", vm.StatusMessage);
        Assert.IsFalse(vm.CanCheck);
        Assert.IsFalse(vm.CanDownload);
        Assert.IsFalse(vm.CanInstall);
    }

    [TestMethod]
    public void Construction_PublishesCurrentVersion()
    {
        var vm = new UpdateCenterViewModel(new FakeUpdates());

        Assert.AreEqual(AppInfo.Current.Version, vm.CurrentVersion);
        Assert.AreEqual(string.Empty, vm.LastCheckedText);
        // Relational across scaffold modes: external scaffolds resolve
        // the slim status at construction (flag set), engine scaffolds
        // wait for the first check. Runners are always unpackaged.
        Assert.AreEqual(AppFeatures.IsExternalUpdateMode, vm.HasCheckedThisSession);
    }

    [TestMethod]
    public async Task Check_StampsLastCheckedAndSessionFlag()
    {
        if (AppFeatures.IsExternalUpdateMode)
            Assert.Inconclusive("Engine flow is not scaffolded in external update mode.");
        var vm = new UpdateCenterViewModel(new FakeUpdates());

        await vm.CheckAsync();

        Assert.IsTrue(vm.HasCheckedThisSession);
        Assert.Contains("Last checked", vm.LastCheckedText);
    }

    [TestMethod]
    public async Task EnsureChecked_RunsOncePerSession()
    {
        if (AppFeatures.IsExternalUpdateMode)
            Assert.Inconclusive("Engine flow is not scaffolded in external update mode.");
        var updates = new FakeUpdates();
        var vm = new UpdateCenterViewModel(updates);

        await vm.EnsureCheckedAsync();
        await vm.EnsureCheckedAsync();

        Assert.AreEqual(1, updates.CheckCalls);
        Assert.IsTrue(vm.HasCheckedThisSession);
    }

    [TestMethod]
    public async Task Check_NoUpdate_ShowsIdle()
    {
        if (AppFeatures.IsExternalUpdateMode)
            Assert.Inconclusive("Engine flow is not scaffolded in external update mode.");
        var vm = new UpdateCenterViewModel(new FakeUpdates());

        await vm.CheckAsync();

        Assert.AreEqual("You are running the latest version.", vm.StatusMessage);
        Assert.IsFalse(vm.HasUpdate);
        Assert.IsTrue(vm.CanCheck);
    }

    [TestMethod]
    public async Task Check_UpdateAvailable_PublishesNotes()
    {
        if (AppFeatures.IsExternalUpdateMode)
            Assert.Inconclusive("Engine flow is not scaffolded in external update mode.");
        var vm = new UpdateCenterViewModel(
            new FakeUpdates { Result = new UpdateCheckResult(true, "9.9", "Highlights") });

        await vm.CheckAsync();

        Assert.IsTrue(vm.HasUpdate);
        Assert.AreEqual("9.9", vm.PendingVersion);
        Assert.AreEqual("Highlights", vm.ReleaseNotes);
        Assert.AreEqual(Visibility.Visible, vm.NotesVisibility);
        Assert.IsTrue(vm.CanDownload);
    }

    [TestMethod]
    public async Task Download_Completes_EnablesInstall()
    {
        if (AppFeatures.IsExternalUpdateMode)
            Assert.Inconclusive("Engine flow is not scaffolded in external update mode.");
        var vm = new UpdateCenterViewModel(
            new FakeUpdates { Result = new UpdateCheckResult(true, "9.9") });

        await vm.CheckAsync();
        await vm.DownloadAsync();

        Assert.IsTrue(vm.CanInstall);
        Assert.AreEqual("Installing…", vm.StatusMessage);
    }

    [TestMethod]
    public async Task Check_Failure_SurfacesError()
    {
        if (AppFeatures.IsExternalUpdateMode)
            Assert.Inconclusive("Engine flow is not scaffolded in external update mode.");
        var vm = new UpdateCenterViewModel(
            new FakeUpdates { CheckError = new InvalidOperationException("boom") });

        await vm.CheckAsync();

        Assert.Contains("Check failed", vm.StatusMessage);
        Assert.IsTrue(vm.CanCheck);
    }

    [TestMethod]
    public void External_RestingState_DisablesEngine()
    {
        if (!AppFeatures.IsExternalUpdateMode)
            Assert.Inconclusive("External flow is not scaffolded in engine update mode.");
        var vm = new UpdateCenterViewModel(new FakeUpdates());

        Assert.IsFalse(vm.CanCheck);
        Assert.IsFalse(string.IsNullOrWhiteSpace(vm.StatusMessage));
    }

    [TestMethod]
    public void ExternalHandlerText_BothViewModelsAgree()
    {
        // Relational across scaffold modes (matrix runs every combo).
        // Runners are always unpackaged, so the packaged-dual branch is
        // unreachable here; packaged routing is proven by the install
        // checklist instead.
        var loc = LocalizationService.Current;
        loc.SetLanguage("en-US");
        string expected = AppFeatures.UpdateMode == "store"
            ? loc.GetString("SettingsUpdatesExternalStore")
            : loc.GetString("SettingsUpdatesExternalAppInstaller");

        Assert.AreEqual(expected, SettingsPageViewModel.ExternalHandlerText(loc));
        Assert.AreEqual(expected, UpdateCenterViewModel.ExternalHandlerText(loc));
    }

    [TestMethod]
    public void PublishResult_Update_SeedsPopupState()
    {
        // The ask-mode background path seeds the popup without re-checking.
        if (AppFeatures.IsExternalUpdateMode)
            Assert.Inconclusive("Engine flow is not scaffolded in external update mode.");
        var vm = new UpdateCenterViewModel(new FakeUpdates());
        vm.PublishResult(new UpdateCheckResult(true, "9.9", "Highlights"));

        Assert.IsTrue(vm.HasUpdate);
        Assert.AreEqual("9.9", vm.PendingVersion);
        Assert.AreEqual("Highlights", vm.ReleaseNotes);
        Assert.IsTrue(vm.HasCheckedThisSession);
        Assert.IsFalse(vm.LastCheckFailed);
        Assert.IsTrue(vm.CanDownload);
    }

    [TestMethod]
    public void PublishResult_NoUpdate_SeedsIdleState()
    {
        if (AppFeatures.IsExternalUpdateMode)
            Assert.Inconclusive("Engine flow is not scaffolded in external update mode.");
        var vm = new UpdateCenterViewModel(new FakeUpdates());
        vm.PublishResult(new UpdateCheckResult(false, null));

        Assert.IsFalse(vm.HasUpdate);
        Assert.IsTrue(vm.HasCheckedThisSession);
        Assert.IsFalse(vm.LastCheckFailed);
        Assert.Contains("latest version", vm.StatusMessage);
    }

    [TestMethod]
    public async Task CheckAsync_Failure_SetsLastCheckFailed()
    {
        // The popup error state (Retry) keys off this flag.
        if (AppFeatures.IsExternalUpdateMode)
            Assert.Inconclusive("Engine flow is not scaffolded in external update mode.");
        var vm = new UpdateCenterViewModel(
            new FakeUpdates { CheckError = new InvalidOperationException("boom") });

        Assert.IsFalse(vm.IsChecking);
        await vm.CheckAsync();

        Assert.IsTrue(vm.LastCheckFailed);
        Assert.IsFalse(vm.IsChecking);
    }

    [TestMethod]
    public async Task CheckAsync_Running_SetsIsChecking()
    {
        if (AppFeatures.IsExternalUpdateMode)
            Assert.Inconclusive("Engine flow is not scaffolded in external update mode.");
        var gate = new TaskCompletionSource();
        var updates = new FakeUpdates { CheckGate = gate };
        var vm = new UpdateCenterViewModel(updates);

        var check = vm.CheckAsync();
        Assert.IsTrue(vm.IsChecking);
        gate.SetResult();
        await check;

        Assert.IsFalse(vm.IsChecking);
    }

    [TestMethod]
    public async Task Check_Failure_RetainsLastGoodAndSetsSticky()
    {
        // B9: a failed re-check must not blank the known update (Retry and
        // the install path stay valid) and must raise the session-sticky
        // flag the Home status card reads.
        if (AppFeatures.IsExternalUpdateMode)
            Assert.Inconclusive("Engine flow is not scaffolded in external update mode.");
        var updates = new FakeUpdates
        {
            Result = new UpdateCheckResult(true, "9.9", "Highlights"),
            CheckError = new InvalidOperationException("boom"),
        };
        var vm = new UpdateCenterViewModel(updates);
        vm.PublishResult(new UpdateCheckResult(true, "9.9", "Highlights"));

        await vm.CheckAsync();

        Assert.IsTrue(vm.LastCheckFailed);
        Assert.IsTrue(UpdateCenterViewModel.LastCheckFailedSticky);
        Assert.IsTrue(vm.HasUpdate);
        Assert.AreEqual("9.9", vm.PendingVersion);

        updates.CheckError = null;
        updates.Result = new UpdateCheckResult(false, null);
        await vm.CheckAsync();

        Assert.IsFalse(vm.LastCheckFailed);
        Assert.IsFalse(UpdateCenterViewModel.LastCheckFailedSticky);
    }

    [TestMethod]
    public async Task Download_Failure_RearmsDownload()
    {
        // B9: a failed download keeps the staged update and re-arms the
        // download (Retry without a fresh check); install stays off.
        if (AppFeatures.IsExternalUpdateMode)
            Assert.Inconclusive("Engine flow is not scaffolded in external update mode.");
        var vm = new UpdateCenterViewModel(
            new FakeUpdates
            {
                Result = new UpdateCheckResult(true, "9.9"),
                DownloadError = new InvalidOperationException("boom"),
            });
        vm.PublishResult(new UpdateCheckResult(true, "9.9"));

        await vm.DownloadAsync();

        Assert.IsTrue(vm.HasUpdate);
        Assert.AreEqual("9.9", vm.PendingVersion);
        Assert.IsTrue(vm.CanDownload);
        Assert.IsFalse(vm.CanInstall);
        Assert.Contains("boom", vm.StatusMessage);
    }
}
