using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using DevTemWinUi3.Services;
using DevTemWinUi3.Tests.Services;
using DevTemWinUi3.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.ViewModels;

[TestClass]
public class DiagnosticsPageViewModelTests
{
    private string _storePath = string.Empty;
    private string _logFile = string.Empty;

    private static readonly string[] s_seedLines = new[]
    {
        "[2026-09-14 10:00:00.000] [INF] hello world",
        "[2026-09-14 10:00:01.000] [DBG] verbose detail",
        "[2026-09-14 10:00:02.000] [WRN] something odd",
        "[2026-09-14 10:00:03.000] [ERR] boom failed",
    };

    [TestInitialize]
    public void Init()
    {
        _storePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");
        LocalSettingsStore.SetTestPath(_storePath);

        // Far-future date sorts newest-first, so a fresh VM selects it.
        var dir = DiagnosticsService.LogDirectoryPath;
        try { Directory.CreateDirectory(dir); } catch { }
        _logFile = Path.Combine(dir, "applog-209903" + Guid.NewGuid().ToString("N")[..6] + ".log");
        File.WriteAllLines(_logFile, s_seedLines);
    }

    [TestCleanup]
    public void Cleanup()
    {
        try { File.Delete(_logFile); } catch { }
        LocalSettingsStore.SetTestPath(null);
        try { File.Delete(_storePath); } catch { }
        // The toggle mutates the process-wide switch: restore Debug.
        try { LoggingService.SetVerbose(false); } catch { }
    }

    [TestMethod]
    public async Task Refresh_SelectsSeedFile_WithFullTail()
    {
        var vm = new DiagnosticsPageViewModel();
        vm.SelectedLogFile = Path.GetFileName(_logFile);
        await vm.WaitForTailAsync();

        Assert.IsTrue(vm.HasLogs);
        Assert.Contains("hello world", vm.LogText);
        Assert.Contains("boom failed", vm.LogText);
        Assert.AreEqual(4, vm.TotalLineCount);
        Assert.AreEqual(4, vm.ShownLineCount);
    }

    [TestMethod]
    public async Task SearchText_FiltersLines_CaseInsensitive()
    {
        var vm = new DiagnosticsPageViewModel();
        vm.SelectedLogFile = Path.GetFileName(_logFile);
        await vm.WaitForTailAsync();
        vm.FilterDebounceDelay = TimeSpan.Zero;

        vm.SearchText = "BOOM";
        await vm.WaitForFilterAsync();

        Assert.Contains("boom failed", vm.LogText);
        Assert.DoesNotContain("hello world", vm.LogText);
        Assert.AreEqual(1, vm.ShownLineCount);
        Assert.AreEqual(4, vm.TotalLineCount);
    }

    [TestMethod]
    public async Task LevelFilter_ShowsOnlyMatchingLines()
    {
        var vm = new DiagnosticsPageViewModel();
        vm.SelectedLogFile = Path.GetFileName(_logFile);
        await vm.WaitForTailAsync();

        vm.SelectedLevelIndex = DiagnosticsPageViewModel.LevelError;

        Assert.Contains("boom failed", vm.LogText);
        Assert.DoesNotContain("hello world", vm.LogText);
        Assert.AreEqual(1, vm.ShownLineCount);
    }

    [TestMethod]
    public async Task ClearSearch_RestoresFullTail()
    {
        var vm = new DiagnosticsPageViewModel();
        vm.SelectedLogFile = Path.GetFileName(_logFile);
        await vm.WaitForTailAsync();
        vm.FilterDebounceDelay = TimeSpan.Zero;
        vm.SearchText = "boom";
        await vm.WaitForFilterAsync();
        vm.SelectedLevelIndex = DiagnosticsPageViewModel.LevelError;

        vm.ClearSearchCommand.Execute(null);
        await vm.WaitForFilterAsync();

        Assert.AreEqual(string.Empty, vm.SearchText);
        Assert.AreEqual(DiagnosticsPageViewModel.LevelAll, vm.SelectedLevelIndex);
        Assert.AreEqual(4, vm.ShownLineCount);
    }

    [TestMethod]
    public async Task Refresh_KeepsSelection_WhenFileStillThere()
    {
        var vm = new DiagnosticsPageViewModel();
        vm.SelectedLogFile = Path.GetFileName(_logFile);
        await vm.WaitForTailAsync();
        string selected = vm.SelectedLogFile!;

        vm.RefreshCommand.Execute(null);
        await vm.WaitForTailAsync();

        Assert.AreEqual(selected, vm.SelectedLogFile);
        Assert.Contains("hello world", vm.LogText);
    }

    [TestMethod]
    public void VerboseLogging_Toggle_PersistsAndApplies()
    {
        var vm = new DiagnosticsPageViewModel();

        vm.VerboseLogging = true;

        Assert.IsTrue(SettingsService.Current.VerboseLogging);
        Assert.AreEqual(LogLevel.Trace, LoggingService.MinimumLevel);

        vm.VerboseLogging = false;

        Assert.IsFalse(SettingsService.Current.VerboseLogging);
        Assert.AreEqual(LogLevel.Debug, LoggingService.MinimumLevel);
    }

    private sealed class FakePickers : IFilePickerService
    {
        public string? SavePath;

        public Task<string?> PickSaveFileAsync(string suggestedFileName, string fileExtension = ".json") =>
            Task.FromResult(SavePath);

        public Task<string?> PickOpenFileAsync() =>
            Task.FromResult<string?>(null);
    }

    private static string UniqueSource(string tag) =>
        "Phase2." + tag + "." + Guid.NewGuid().ToString("N")[..8];

    [TestMethod]
    public void CrashReporting_Toggle_Persists()
    {
        var vm = new DiagnosticsPageViewModel();

        vm.CrashReportingEnabled = true;

        Assert.IsTrue(SettingsService.Current.CrashReportsEnabled);

        vm.CrashReportingEnabled = false;

        Assert.IsFalse(SettingsService.Current.CrashReportsEnabled);
    }

    [TestMethod]
    public void ViewSwitch_TogglesVisibilities()
    {
        var vm = new DiagnosticsPageViewModel();

        vm.SelectedViewIndex = DiagnosticsPageViewModel.ViewLive;

        Assert.AreEqual(Visibility.Collapsed, vm.FileViewVisibility);
        Assert.AreEqual(Visibility.Visible, vm.LiveViewVisibility);

        vm.SelectedViewIndex = DiagnosticsPageViewModel.ViewFile;

        // Backends without a file sink (mel, none) keep the file view
        // hidden: there is nothing file-backed to show.
        var expectedFile = LoggingService.HasFileSink ? Visibility.Visible : Visibility.Collapsed;
        Assert.AreEqual(expectedFile, vm.FileViewVisibility);
        Assert.AreEqual(Visibility.Collapsed, vm.LiveViewVisibility);
    }

    [TestMethod]
    public void LiveFilter_Source_NarrowsResults()
    {
        string keep = UniqueSource("Keep");
        string drop = UniqueSource("Drop");
        LoggingService.EventBuffer.Emit(TestEvents.Make("keep me", keep));
        LoggingService.EventBuffer.Emit(TestEvents.Make("drop me", drop));

        var vm = new DiagnosticsPageViewModel();
        vm.SelectedViewIndex = DiagnosticsPageViewModel.ViewLive;
        vm.LiveSourceFilter = keep;

        Assert.HasCount(1, vm.LiveEvents);
        Assert.AreEqual(keep, vm.LiveEvents[0].SourceContext);
    }

    [TestMethod]
    public void LiveFilter_ExceptionsOnly_KeepsFailures()
    {
        string tag = UniqueSource("Exc");
        LoggingService.EventBuffer.Emit(TestEvents.Make("fine", tag));
        LoggingService.EventBuffer.Emit(TestEvents.Make("broken", tag,
            LogLevel.Error, new InvalidOperationException("x")));
        var vm = new DiagnosticsPageViewModel();
        vm.SelectedViewIndex = DiagnosticsPageViewModel.ViewLive;
        vm.LiveSourceFilter = tag;

        vm.ExceptionsOnly = true;

        Assert.IsNotEmpty(vm.LiveEvents);
        Assert.IsTrue(vm.LiveEvents.All(e => e.HasException));
    }

    [TestMethod]
    public async Task LiveFilter_Regex_MatchesPattern_AndFlagsInvalid()
    {
        string tag = UniqueSource("Re");
        LoggingService.EventBuffer.Emit(TestEvents.Make("boom happened", tag));
        LoggingService.EventBuffer.Emit(TestEvents.Make("all quiet", tag));

        var vm = new DiagnosticsPageViewModel();
        vm.SelectedViewIndex = DiagnosticsPageViewModel.ViewLive;
        vm.LiveSourceFilter = tag;
        vm.UseRegex = true;
        vm.FilterDebounceDelay = TimeSpan.Zero;
        vm.SearchText = "boom|crash";
        await vm.WaitForFilterAsync();

        Assert.HasCount(1, vm.LiveEvents);

        vm.SearchText = "[invalid";
        await vm.WaitForFilterAsync();

        Assert.IsTrue(vm.HasLiveFilterError);
        Assert.IsEmpty(vm.LiveEvents);
        vm.SearchText = string.Empty;
    }

    [TestMethod]
    public void LivePause_CountsArrivals_UntilResume()
    {
        var vm = new DiagnosticsPageViewModel();
        vm.SelectedViewIndex = DiagnosticsPageViewModel.ViewLive;
        vm.IsLivePaused = true;

        LoggingService.EventBuffer.Emit(TestEvents.Make("paused one"));
        LoggingService.EventBuffer.Emit(TestEvents.Make("paused two"));
        vm.RefreshLive();

        Assert.AreEqual(2, vm.NewEventsCount);

        vm.ResumeLiveCommand.Execute(null);

        Assert.IsFalse(vm.IsLivePaused);
        Assert.AreEqual(0, vm.NewEventsCount);
    }

    [TestMethod]
    public void ExportText_FollowsCurrentView()
    {
        string tag = UniqueSource("Exp");
        LoggingService.EventBuffer.Emit(TestEvents.Make("export me", tag));

        var vm = new DiagnosticsPageViewModel();
        vm.SelectedViewIndex = DiagnosticsPageViewModel.ViewLive;
        vm.LiveSourceFilter = tag;

        string text = vm.GetFilteredExportText();
        Assert.Contains("[INF]", text);
        Assert.Contains("export me", text);
    }

    [TestMethod]
    public async Task SearchText_KeystrokeStorm_CoalescesToOneRecompute()
    {
        // P1-2: 10 keys with no pause collapse into a single recompute.
        var vm = new DiagnosticsPageViewModel();
        vm.FilterDebounceDelay = TimeSpan.FromMilliseconds(100);
        int before = vm.DebouncedFilterRuns;

        for (int i = 0; i < 10; i++)
            vm.SearchText = "storm-" + i;
        await vm.WaitForFilterAsync();

        Assert.AreEqual(before + 1, vm.DebouncedFilterRuns);
    }

    [TestMethod]
    public void ShortLevel_MapsKnownLevels()
    {
        Assert.AreEqual("INF", DiagnosticsPageViewModel.ShortLevel("Information"));
        Assert.AreEqual("DBG", DiagnosticsPageViewModel.ShortLevel("Debug"));
        Assert.AreEqual("FTL", DiagnosticsPageViewModel.ShortLevel("Fatal"));
        Assert.AreEqual("Custom", DiagnosticsPageViewModel.ShortLevel("Custom"));
    }

    [TestMethod]
    public async Task SaveView_WritesFile_AndReportsPath()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".log");
        try
        {
            var vm = new DiagnosticsPageViewModel(new FakePickers { SavePath = path });

            await vm.SaveViewCommand.ExecuteAsync(null);

            Assert.IsFalse(vm.LastExportError);
            Assert.AreEqual(path, vm.LastExportPath);
            Assert.IsTrue(File.Exists(path));
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }

    [TestMethod]
    public async Task SaveView_Cancel_IsNeitherErrorNorPath()
    {
        var vm = new DiagnosticsPageViewModel(new FakePickers { SavePath = null });

        await vm.SaveViewCommand.ExecuteAsync(null);

        Assert.IsFalse(vm.LastExportError);
        Assert.AreEqual(string.Empty, vm.LastExportPath);
    }

    [TestMethod]
    public async Task ExportBundle_WritesZip_WithExpectedEntries()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".zip");
        try
        {
            var vm = new DiagnosticsPageViewModel(new FakePickers { SavePath = path });

            await vm.ExportBundleCommand.ExecuteAsync(null);

            Assert.IsFalse(vm.LastExportError);
            Assert.AreEqual(path, vm.LastExportPath);
            using var zip = ZipFile.OpenRead(path);
            var names = zip.Entries.Select(e => e.FullName).ToArray();
            Assert.IsTrue(names.Contains("status.json"));
            Assert.IsTrue(names.Contains("settings.json"));
            Assert.IsTrue(names.Contains("log-filtered.log"));
            Assert.IsTrue(names.Contains("log-current.log"));
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }
}
