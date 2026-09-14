using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevTemWinUi3.Services;

namespace DevTemWinUi3.ViewModels;

public partial class DiagnosticsPageViewModel : ObservableObject
{
    /// <summary>
    /// Level filter indices bound to the level ComboBox
    /// (0 = All, 1 = Debug, 2 = Info, 3 = Warning, 4 = Error).
    /// Kept as an index (not localized text) so the VM stays UI-free.
    /// </summary>
    public const int LevelAll = 0;
    public const int LevelDebug = 1;
    public const int LevelInfo = 2;
    public const int LevelWarning = 3;
    public const int LevelError = 4;

    public ObservableCollection<string> LogFiles { get; } = new();

    [ObservableProperty]
    private DiagnosticsStatus? _status;

    [ObservableProperty]
    private string? _selectedLogFile;

    /// <summary>Filtered tail shown in the view. Never null.</summary>
    [ObservableProperty]
    private string _logText = string.Empty;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private int _selectedLevelIndex;

    [ObservableProperty]
    private int _shownLineCount;

    [ObservableProperty]
    private int _totalLineCount;

    [ObservableProperty]
    private bool _hasLogs;

    private string _fullLogText = string.Empty;

    public DiagnosticsPageViewModel()
    {
        Refresh();
    }

    partial void OnSelectedLogFileChanged(string? value) => LoadTail();

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedLevelIndexChanged(int value) => ApplyFilter();

    /// <summary>
    /// Reloads status + log list, keeping the selection when the file is
    /// still there (newest otherwise). Never throws.
    /// </summary>
    [RelayCommand]
    public void Refresh()
    {
        try
        {
            Status = DiagnosticsService.GetStatus();

            LogFiles.Clear();
            foreach (var full in DiagnosticsService.GetLogFiles())
                LogFiles.Add(Path.GetFileName(full));

            HasLogs = LogFiles.Count > 0;
            if (!HasLogs)
            {
                SelectedLogFile = null;
                _fullLogText = string.Empty;
                LogText = string.Empty;
                ShownLineCount = 0;
                TotalLineCount = 0;
                return;
            }

            if (SelectedLogFile is null || !LogFiles.Contains(SelectedLogFile))
                SelectedLogFile = LogFiles[0];
            else
                LoadTail();
        }
        catch { }
    }

    [RelayCommand]
    public void ClearSearch()
    {
        SearchText = string.Empty;
        SelectedLevelIndex = LevelAll;
    }

    private void LoadTail()
    {
        try
        {
            if (string.IsNullOrEmpty(SelectedLogFile))
            {
                _fullLogText = string.Empty;
                ApplyFilter();
                return;
            }
            var full = DiagnosticsService.GetLogFiles()
                .FirstOrDefault(p => Path.GetFileName(p) == SelectedLogFile);
            _fullLogText = full is null ? string.Empty : DiagnosticsService.ReadLogTail(full);
            ApplyFilter();
        }
        catch
        {
            _fullLogText = string.Empty;
            ApplyFilter();
        }
    }

    /// <summary>
    /// Recomputes <see cref="LogText"/> from the loaded tail using the
    /// current search + level filter. Never throws.
    /// </summary>
    internal void ApplyFilter()
    {
        try
        {
            string[] lines = string.IsNullOrEmpty(_fullLogText)
                ? Array.Empty<string>()
                : _fullLogText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            TotalLineCount = lines.Length;

            string? marker = SelectedLevelIndex switch
            {
                LevelDebug => "[DBG]",
                LevelInfo => "[INF]",
                LevelWarning => "[WRN]",
                LevelError => "[ERR]",
                _ => null,
            };

            bool filterErrorExtra = SelectedLevelIndex == LevelError;
            string search = SearchText?.Trim() ?? string.Empty;

            var kept = lines.Where(line =>
                (marker is null ||
                    line.Contains(marker, StringComparison.OrdinalIgnoreCase) ||
                    (filterErrorExtra && line.Contains("[FTL]", StringComparison.OrdinalIgnoreCase))) &&
                (search.Length == 0 ||
                    line.Contains(search, StringComparison.OrdinalIgnoreCase)));

            string[] result = kept.ToArray();
            ShownLineCount = result.Length;
            LogText = string.Join(Environment.NewLine, result);
        }
        catch
        {
            LogText = string.Empty;
            ShownLineCount = 0;
            TotalLineCount = 0;
        }
    }
}
