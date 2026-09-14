using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevTemWinUi3.Services;
using Microsoft.UI.Xaml;
using static DevTemWinUi3.Services.DiagnosticsService;

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

    /// <summary>Viewer indices bound to the File/Live Segmented control.</summary>
    public const int ViewFile = 0;
    public const int ViewLive = 1;

    private const int LiveEventCap = 500;

    private readonly IFilePickerService _pickers;

    public ObservableCollection<string> LogFiles { get; } = new();

    public ObservableCollection<BufferedLogEvent> LiveEvents { get; } = new();

    [ObservableProperty]
    private DiagnosticsStatus? _status;

    [ObservableProperty]
    private string? _selectedLogFile;

    /// <summary>Filtered tail shown in the file view. Never null.</summary>
    [ObservableProperty]
    private string _logText = string.Empty;

    /// <summary>
    /// Shared message filter: substring match on file lines, substring or
    /// regex match on live event messages (see <see cref="UseRegex"/>).
    /// </summary>
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

    [ObservableProperty]
    private bool _verboseLogging;

    /// <summary>
    /// User consent for crash reporting (a DSN must still be configured
    /// for anything to flow). Never throws.
    /// </summary>
    [ObservableProperty]
    private bool _crashReportingEnabled;

    [ObservableProperty]
    private int _selectedViewIndex;

    [ObservableProperty]
    private Visibility _fileViewVisibility = Visibility.Visible;

    [ObservableProperty]
    private Visibility _liveViewVisibility = Visibility.Collapsed;

    /// <summary>Live-only: substring filter on the event source.</summary>
    [ObservableProperty]
    private string _liveSourceFilter = string.Empty;

    /// <summary>Live-only: keep only events carrying an exception.</summary>
    [ObservableProperty]
    private bool _exceptionsOnly;

    /// <summary>Live-only: treat <see cref="SearchText"/> as a regex.</summary>
    [ObservableProperty]
    private bool _useRegex;

    [ObservableProperty]
    private bool _hasLiveFilterError;

    [ObservableProperty]
    private int _liveShownCount;

    [ObservableProperty]
    private int _liveTotalCount;

    [ObservableProperty]
    private bool _isLivePaused;

    [ObservableProperty]
    private int _newEventsCount;

    /// <summary>Outcome of the last export command (for page toasts).</summary>
    [ObservableProperty]
    private bool _lastExportError;

    [ObservableProperty]
    private string _lastExportPath = string.Empty;

    private string _fullLogText = string.Empty;
    private int _liveTotalAtPause;

    public DiagnosticsPageViewModel(IFilePickerService? pickers = null)
    {
        _pickers = pickers ?? new FilePickerService();
        Refresh();
    }

    partial void OnSelectedLogFileChanged(string? value) => LoadTail();

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
        if (SelectedViewIndex == ViewLive && !IsLivePaused)
            ApplyLiveFilter();
    }

    partial void OnSelectedLevelIndexChanged(int value) => ApplyFilter();

    partial void OnVerboseLoggingChanged(bool value)
    {
        try
        {
            SettingsService.Current.VerboseLogging = value;
            LoggingService.SetVerbose(value);
        }
        catch { }
    }

    partial void OnCrashReportingEnabledChanged(bool value)
    {
        try
        {
            SettingsService.Current.CrashReportsEnabled = value;
            if (value)
                CrashReportingService.Current.Initialize();
            else
                CrashReportingService.Current.Shutdown();
        }
        catch { }
    }

    partial void OnSelectedViewIndexChanged(int value)
    {
        FileViewVisibility = value == ViewLive ? Visibility.Collapsed : Visibility.Visible;
        LiveViewVisibility = value == ViewLive ? Visibility.Visible : Visibility.Collapsed;
        if (value == ViewLive)
            RefreshLive();
    }

    partial void OnLiveSourceFilterChanged(string value)
    {
        if (!IsLivePaused)
            ApplyLiveFilter();
    }

    partial void OnExceptionsOnlyChanged(bool value)
    {
        if (!IsLivePaused)
            ApplyLiveFilter();
    }

    partial void OnUseRegexChanged(bool value)
    {
        if (!IsLivePaused)
            ApplyLiveFilter();
    }

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

            try { VerboseLogging = SettingsService.Current.VerboseLogging; } catch { }
            try { CrashReportingEnabled = SettingsService.Current.CrashReportsEnabled; } catch { }

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
        LiveSourceFilter = string.Empty;
        ExceptionsOnly = false;
        UseRegex = false;
    }

    /// <summary>
    /// Rebuilds the live list from the ring buffer (timer tick / view
    /// switch). While paused, only counts new arrivals. Never throws.
    /// </summary>
    internal void RefreshLive()
    {
        try
        {
            int freshTotal = 0;
            try { freshTotal = LoggingService.EventBuffer.Count; } catch { }
            if (IsLivePaused)
            {
                NewEventsCount = Math.Max(0, freshTotal - _liveTotalAtPause);
                return;
            }
            ApplyLiveFilter();
            _liveTotalAtPause = freshTotal;
            NewEventsCount = 0;
        }
        catch { }
    }

    [RelayCommand]
    public void ResumeLive()
    {
        IsLivePaused = false;
        NewEventsCount = 0;
        RefreshLive();
    }

    /// <summary>
    /// Recomputes <see cref="LiveEvents"/> from the buffer using the live
    /// filters. Never throws.
    /// </summary>
    internal void ApplyLiveFilter()
    {
        try
        {
            int total = 0;
            try { total = LoggingService.EventBuffer.Count; } catch { }
            LiveTotalCount = total;
            HasLiveFilterError = false;

            string source = LiveSourceFilter?.Trim() ?? string.Empty;
            string search = SearchText?.Trim() ?? string.Empty;
            Regex? rx = null;
            if (UseRegex && search.Length > 0)
            {
                try
                {
                    rx = new Regex(search,
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                        TimeSpan.FromMilliseconds(250));
                }
                catch (ArgumentException)
                {
                    HasLiveFilterError = true;
                }
            }

            LiveEvents.Clear();
            if (HasLiveFilterError)
            {
                LiveShownCount = 0;
                return;
            }

            foreach (var e in DiagnosticsService.GetBufferedEvents(LiveEventCap))
            {
                if (ExceptionsOnly && !e.HasException)
                    continue;
                if (source.Length > 0 &&
                    (e.SourceContext is null ||
                     !e.SourceContext.Contains(source, StringComparison.OrdinalIgnoreCase)))
                    continue;
                if (rx is not null)
                {
                    bool match;
                    try { match = rx.IsMatch(e.Message); }
                    catch { match = false; }
                    if (!match)
                        continue;
                }
                else if (search.Length > 0 &&
                    !e.Message.Contains(search, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                LiveEvents.Add(e);
            }
            LiveShownCount = LiveEvents.Count;
        }
        catch
        {
            LiveEvents.Clear();
            LiveShownCount = 0;
            LiveTotalCount = 0;
        }
    }

    /// <summary>
    /// Text exported by Save/Bundle: file tail in file mode, formatted live
    /// events in live mode. Never throws, never null.
    /// </summary>
    internal string GetFilteredExportText()
    {
        try
        {
            if (SelectedViewIndex == ViewLive)
                return string.Join(Environment.NewLine, LiveEvents.Select(FormatExportLine));
            return LogText;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>Single-line file-style rendering of a buffered event.</summary>
    internal static string FormatExportLine(BufferedLogEvent e)
    {
        try
        {
            string line = string.Format(CultureInfo.InvariantCulture,
                "[{0:yyyy-MM-dd HH:mm:ss.fff}] [{1}] {2}",
                e.Timestamp, ShortLevel(e.Level), e.Message);
            if (!string.IsNullOrEmpty(e.ExceptionText))
                line += Environment.NewLine + e.ExceptionText;
            return line;
        }
        catch
        {
            return e.Message;
        }
    }

    internal static string ShortLevel(string level)
    {
        if (string.Equals(level, "Verbose", StringComparison.OrdinalIgnoreCase))
            return "VRB";
        if (string.Equals(level, "Debug", StringComparison.OrdinalIgnoreCase))
            return "DBG";
        if (string.Equals(level, "Information", StringComparison.OrdinalIgnoreCase))
            return "INF";
        if (string.Equals(level, "Warning", StringComparison.OrdinalIgnoreCase))
            return "WRN";
        if (string.Equals(level, "Error", StringComparison.OrdinalIgnoreCase))
            return "ERR";
        if (string.Equals(level, "Fatal", StringComparison.OrdinalIgnoreCase))
            return "FTL";
        return level;
    }

    [RelayCommand]
    public async Task SaveViewAsync()
    {
        LastExportError = false;
        LastExportPath = string.Empty;
        try
        {
            var path = await _pickers.PickSaveFileAsync("devtem-log", ".log");
            if (string.IsNullOrWhiteSpace(path))
                return;
            File.WriteAllText(path, GetFilteredExportText());
            LastExportPath = path;
        }
        catch
        {
            LastExportError = true;
        }
    }

    [RelayCommand]
    public async Task ExportBundleAsync()
    {
        LastExportError = false;
        LastExportPath = string.Empty;
        try
        {
            var path = await _pickers.PickSaveFileAsync("devtem-diagnostics", ".zip");
            if (string.IsNullOrWhiteSpace(path))
                return;
            bool ok = DiagnosticsService.CreateDiagnosticBundle(
                path, GetFilteredExportText(), SelectedLogFile, Status);
            if (ok)
                LastExportPath = path;
            else
                LastExportError = true;
        }
        catch
        {
            LastExportError = true;
        }
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
