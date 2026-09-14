using System;
using System.IO;
using DevTemWinUi3.Services;
using DevTemWinUi3.ViewModels;
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
    }

    [TestMethod]
    public void Refresh_SelectsSeedFile_WithFullTail()
    {
        var vm = new DiagnosticsPageViewModel();
        vm.SelectedLogFile = Path.GetFileName(_logFile);

        Assert.IsTrue(vm.HasLogs);
        Assert.Contains("hello world", vm.LogText);
        Assert.Contains("boom failed", vm.LogText);
        Assert.AreEqual(4, vm.TotalLineCount);
        Assert.AreEqual(4, vm.ShownLineCount);
    }

    [TestMethod]
    public void SearchText_FiltersLines_CaseInsensitive()
    {
        var vm = new DiagnosticsPageViewModel();
        vm.SelectedLogFile = Path.GetFileName(_logFile);

        vm.SearchText = "BOOM";

        Assert.Contains("boom failed", vm.LogText);
        Assert.DoesNotContain("hello world", vm.LogText);
        Assert.AreEqual(1, vm.ShownLineCount);
        Assert.AreEqual(4, vm.TotalLineCount);
    }

    [TestMethod]
    public void LevelFilter_ShowsOnlyMatchingLines()
    {
        var vm = new DiagnosticsPageViewModel();
        vm.SelectedLogFile = Path.GetFileName(_logFile);

        vm.SelectedLevelIndex = DiagnosticsPageViewModel.LevelError;

        Assert.Contains("boom failed", vm.LogText);
        Assert.DoesNotContain("hello world", vm.LogText);
        Assert.AreEqual(1, vm.ShownLineCount);
    }

    [TestMethod]
    public void ClearSearch_RestoresFullTail()
    {
        var vm = new DiagnosticsPageViewModel();
        vm.SelectedLogFile = Path.GetFileName(_logFile);
        vm.SearchText = "boom";
        vm.SelectedLevelIndex = DiagnosticsPageViewModel.LevelError;

        vm.ClearSearchCommand.Execute(null);

        Assert.AreEqual(string.Empty, vm.SearchText);
        Assert.AreEqual(DiagnosticsPageViewModel.LevelAll, vm.SelectedLevelIndex);
        Assert.AreEqual(4, vm.ShownLineCount);
    }

    [TestMethod]
    public void Refresh_KeepsSelection_WhenFileStillThere()
    {
        var vm = new DiagnosticsPageViewModel();
        vm.SelectedLogFile = Path.GetFileName(_logFile);
        string selected = vm.SelectedLogFile!;

        vm.RefreshCommand.Execute(null);

        Assert.AreEqual(selected, vm.SelectedLogFile);
        Assert.Contains("hello world", vm.LogText);
    }
}
