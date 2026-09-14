using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using DevTemWinUi3.Services;
using DevTemWinUi3.ViewModels;
using static DevTemWinUi3.Services.DiagnosticsService;

namespace DevTemWinUi3.Pages;

public sealed partial class DiagnosticsPage : Page, INavigationAware
{
    public DiagnosticsPageViewModel ViewModel { get; }

    private readonly DispatcherTimer _liveTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private ScrollViewer? _liveScroll;
    private bool _liveScrollHooked;

    public DiagnosticsPage()
    {
        // Resolve BEFORE InitializeComponent: compiled bindings
        // ({x:Bind ViewModel.…}) evaluate during XAML load, so a later
        // assignment would leave the log list/text bound to null.
        ViewModel = ServiceLocator.GetRequiredService<DiagnosticsPageViewModel>();
        this.InitializeComponent();
        DataContext = ViewModel;

        // Composed text below cannot bind: re-render it on language switch.
        // (Unsubscribed in OnNavigatedFrom: this page is not cached, so a
        // static-event subscription would leak every visit.)
        LocalizationService.Current.LanguageChanged += OnLanguageChanged;
        ViewModel.PropertyChanged += OnViewModelChanged;
        _liveTimer.Tick += LiveTimer_Tick;
        RenderAll();
    }

    public void OnNavigatedTo(object? parameter)
    {
        ViewModel.Refresh();
        RenderAll();
        HookLiveScroll();
        _liveTimer.Start();
    }

    public void OnNavigatedFrom()
    {
        LocalizationService.Current.LanguageChanged -= OnLanguageChanged;
        ViewModel.PropertyChanged -= OnViewModelChanged;
        _liveTimer.Stop();
        if (_liveScroll is not null)
        {
            try { _liveScroll.ViewChanged -= LiveScroll_ViewChanged; } catch { }
            _liveScroll = null;
            _liveScrollHooked = false;
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs e) => RenderAll();

    private void LiveTimer_Tick(object? sender, object e)
    {
        try
        {
            HookLiveScroll();
            if (ViewModel.SelectedViewIndex != DiagnosticsPageViewModel.ViewLive)
                return;
            ViewModel.RefreshLive();
            RenderLiveState();
        }
        catch { }
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DiagnosticsPageViewModel.LogText)
            or nameof(DiagnosticsPageViewModel.ShownLineCount)
            or nameof(DiagnosticsPageViewModel.TotalLineCount)
            or nameof(DiagnosticsPageViewModel.HasLogs)
            or nameof(DiagnosticsPageViewModel.LogFiles)
            or nameof(DiagnosticsPageViewModel.SearchText)
            or nameof(DiagnosticsPageViewModel.LiveSourceFilter)
            or nameof(DiagnosticsPageViewModel.ExceptionsOnly)
            or nameof(DiagnosticsPageViewModel.UseRegex)
            or nameof(DiagnosticsPageViewModel.HasLiveFilterError)
            or nameof(DiagnosticsPageViewModel.LiveShownCount)
            or nameof(DiagnosticsPageViewModel.LiveTotalCount)
            or nameof(DiagnosticsPageViewModel.IsLivePaused)
            or nameof(DiagnosticsPageViewModel.NewEventsCount)
            or nameof(DiagnosticsPageViewModel.SelectedViewIndex))
            RenderLogState();
    }

    /// <summary>
    /// Re-applies everything that cannot live in XAML bindings: the
    /// localized control names, the composed status text, and the log
    /// count/empty state (kept in the page so the VM stays UI-free).
    /// </summary>
    private void RenderAll()
    {
        var loc = LocalizationService.Current;
        AutomationProperties.SetName(LogFileComboBox, loc.GetString("DiagnosticsLogs"));
        AutomationProperties.SetName(SearchBox, loc.GetString("DiagnosticsSearchPlaceholder"));
        AutomationProperties.SetName(LevelComboBox, loc.GetString("DiagnosticsLevelAll"));
        AutomationProperties.SetName(ViewSegment, loc.GetString("DiagnosticsLogs"));
        AutomationProperties.SetName(SourceBox, loc.GetString("DiagnosticsSourcePlaceholder"));
        AutomationProperties.SetName(ExceptionsCheck, loc.GetString("DiagnosticsExceptionsOnly"));
        AutomationProperties.SetName(RegexCheck, loc.GetString("DiagnosticsUseRegex"));
        AutomationProperties.SetName(VerboseToggle, loc.GetString("DiagnosticsVerbose"));
        AutomationProperties.SetName(CrashReportsToggle, loc.GetString("DiagnosticsCrashReports"));
        LogFileComboBox.PlaceholderText = loc.GetString("DiagnosticsLogs");
        RenderStatus();
        RenderMetrics();
        RenderLogState();
    }

    /// <summary>
    /// Builds the status lines from the ViewModel snapshot with localized
    /// labels (kept in the page so the VM stays UI-free and testable).
    /// </summary>
    private void RenderStatus()
    {
        var loc = LocalizationService.Current;
        var status = ViewModel.Status;
        if (status is null)
        {
            StatusText.Text = string.Empty;
            return;
        }
        string onOff = status.SentryEnabled ? loc.GetString("DiagnosticsOn") : loc.GetString("DiagnosticsOff");
        string yes = loc.GetString("DiagnosticsYes");
        string no = loc.GetString("DiagnosticsNo");
        string none = loc.GetString("DiagnosticsNone");
        string startupMs = status.StartupElapsedMs.ToString(CultureInfo.InvariantCulture) + " ms";
        string logKb = (status.LogDirectorySizeBytes / 1024).ToString(CultureInfo.InvariantCulture) + " KB";
        string dbText = status.DatabaseSizeBytes < 0
            ? none
            : (status.DatabaseSizeBytes / 1024).ToString(CultureInfo.InvariantCulture) + " KB";
        StatusText.Text =
            $"{loc.GetString("DiagnosticsVersion")}: {status.AppVersion}\n" +
            $"{loc.GetString("DiagnosticsChannel")}: {status.Channel}\n" +
            $"{loc.GetString("DiagnosticsTheme")}: {status.Theme}\n" +
            $"{loc.GetString("DiagnosticsLanguage")}: {status.Language}\n" +
            $"{loc.GetString("DiagnosticsStartup")}: {startupMs}\n" +
            $"{loc.GetString("DiagnosticsLogLevel")}: {status.LogLevel}\n" +
            $"{loc.GetString("DiagnosticsLogFiles")}: {status.LogFileCount} ({logKb})\n" +
            $"{loc.GetString("DiagnosticsBuffered")}: {status.BufferedEventCount}\n" +
            $"{loc.GetString("DiagnosticsPackaged")}: {(status.IsPackaged ? yes : no)}\n" +
            $"{loc.GetString("DiagnosticsPendingUpdate")}: {(string.IsNullOrEmpty(status.PendingUpdate) ? none : status.PendingUpdate)}\n" +
            $"{loc.GetString("DiagnosticsDatabase")}: {dbText}\n" +
            $"{loc.GetString("DiagnosticsSentry")}: {onOff}";
    }

    /// <summary>
    /// Builds the metrics card from the in-process snapshot (startup phase
    /// breakdown, navigation counts, update checks, exceptions). Kept in
    /// the page so the VM stays UI-free.
    /// </summary>
    private void RenderMetrics()
    {
        try
        {
            var loc = LocalizationService.Current;
            var snapshot = DevTemWinUi3.Services.Diagnostics.AppMetrics.GetSnapshot();
            var lines = new List<string>();
            foreach (var phase in snapshot.StartupPhasesMs.OrderBy(
                         kv => kv.Key, StringComparer.Ordinal))
                lines.Add($"startup.{phase.Key}: {phase.Value.ToString("F0", CultureInfo.InvariantCulture)} ms");
            string navs = snapshot.NavigationCounts.Count == 0
                ? "0"
                : string.Join(", ", snapshot.NavigationCounts
                    .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                    .Select(kv => $"{kv.Key} ×{kv.Value.ToString(CultureInfo.InvariantCulture)}"));
            lines.Add($"{loc.GetString("DiagnosticsNavigations")}: {navs}");
            string lastCheck = snapshot.LastUpdateCheckMs.HasValue
                ? snapshot.LastUpdateCheckMs.Value.ToString("F0", CultureInfo.InvariantCulture) + " ms"
                : loc.GetString("DiagnosticsNone");
            lines.Add($"{loc.GetString("DiagnosticsUpdateChecks")}: " +
                $"{snapshot.UpdateCheckCount.ToString(CultureInfo.InvariantCulture)} ({lastCheck})");
            lines.Add($"{loc.GetString("DiagnosticsExceptions")}: " +
                $"{snapshot.ExceptionCount.ToString(CultureInfo.InvariantCulture)}");
            MetricsText.Text = string.Join("\n", lines);
        }
        catch
        {
            MetricsText.Text = string.Empty;
        }
    }

    /// <summary>
    /// Count line + empty state, per viewer. Placeholders live in their own
    /// elements so compiled bindings are never overwritten.
    /// </summary>
    private void RenderLogState()
    {
        if (ViewModel.SelectedViewIndex == DiagnosticsPageViewModel.ViewLive)
        {
            RenderLiveState();
            return;
        }
        var loc = LocalizationService.Current;
        LogFileComboBox.IsEnabled = ViewModel.HasLogs;

        if (!ViewModel.HasLogs)
        {
            LogCountText.Text = string.Empty;
            EmptyStateText.Text = loc.GetString("DiagnosticsNoLogs");
            EmptyStateText.Visibility = Visibility.Visible;
            LogText.Visibility = Visibility.Collapsed;
            return;
        }

        LogCountText.Text = loc.GetString(
            "DiagnosticsLines", ViewModel.ShownLineCount, ViewModel.TotalLineCount);

        bool empty = string.IsNullOrEmpty(ViewModel.LogText);
        EmptyStateText.Text = empty ? loc.GetString("DiagnosticsNoMatch") : string.Empty;
        EmptyStateText.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        LogText.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
    }

    private void RenderLiveState()
    {
        var loc = LocalizationService.Current;
        LogCountText.Text = loc.GetString(
            "DiagnosticsEventsCount", ViewModel.LiveShownCount, ViewModel.LiveTotalCount);

        bool empty = ViewModel.LiveEvents.Count == 0;
        if (empty)
        {
            LiveEmptyText.Text = ViewModel.HasLiveFilterError
                ? loc.GetString("DiagnosticsInvalidPattern")
                : ViewModel.LiveTotalCount == 0
                    ? loc.GetString("DiagnosticsNoLogs")
                    : loc.GetString("DiagnosticsNoMatch");
        }
        LiveEmptyText.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        LiveList.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;

        bool showNew = ViewModel.IsLivePaused && ViewModel.NewEventsCount > 0;
        NewEventsButton.Content = showNew
            ? loc.GetString("DiagnosticsNewEvents", ViewModel.NewEventsCount)
            : string.Empty;
        NewEventsButton.Visibility = showNew ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// Pause-on-scroll: scrolling up pauses the live tail (the timer keeps
    /// counting arrivals for the "N new" button); reaching the bottom
    /// resumes. Best-effort: never touches the VM on failure.
    /// </summary>
    private void HookLiveScroll()
    {
        if (_liveScrollHooked)
            return;
        try
        {
            var scroll = FindDescendant<ScrollViewer>(LiveList);
            if (scroll is null)
                return;
            _liveScroll = scroll;
            _liveScroll.ViewChanged += LiveScroll_ViewChanged;
            _liveScrollHooked = true;
        }
        catch { }
    }

    private void LiveScroll_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        try
        {
            if (sender is not ScrollViewer sv)
                return;
            bool atBottom = sv.VerticalOffset >= sv.ScrollableHeight - 8;
            if (atBottom && ViewModel.IsLivePaused)
            {
                ViewModel.ResumeLiveCommand.Execute(null);
                RenderLiveState();
            }
            else if (!atBottom && !ViewModel.IsLivePaused)
            {
                ViewModel.IsLivePaused = true;
            }
        }
        catch { }
    }

    private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        try
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T typed)
                    return typed;
                var deep = FindDescendant<T>(child);
                if (deep is not null)
                    return deep;
            }
        }
        catch { }
        return null;
    }

    private void NewEventsButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ResumeLiveCommand.Execute(null);
        RenderLiveState();
        try
        {
            var last = ViewModel.LiveEvents.Count > 0
                ? ViewModel.LiveEvents[ViewModel.LiveEvents.Count - 1]
                : null;
            if (last is not null)
                LiveList.ScrollIntoView(last);
        }
        catch { }
    }

    private async void LiveList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is BufferedLogEvent ev)
            await ShowEventDetailAsync(ev);
    }

    /// <summary>Full event detail: timestamp, level, source, properties, message, exception.</summary>
    private async System.Threading.Tasks.Task ShowEventDetailAsync(BufferedLogEvent ev)
    {
        try
        {
            var loc = LocalizationService.Current;
            var panel = new StackPanel { Spacing = 8 };
            void AddRow(string label, string value, bool wrap)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = label,
                    Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
                    Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorTertiaryBrush"],
                });
                panel.Children.Add(new TextBlock
                {
                    Text = value,
                    TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap,
                    IsTextSelectionEnabled = true,
                    FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                    FontSize = 12,
                });
            }
            AddRow(loc.GetString("DiagnosticsDetailTime"), ev.Timestamp.ToString(CultureInfo.InvariantCulture), true);
            AddRow(loc.GetString("DiagnosticsLogLevel"), ev.Level, false);
            AddRow(loc.GetString("DiagnosticsDetailSource"), ev.SourceContext ?? loc.GetString("DiagnosticsNone"), false);
            if (ev.Properties.Count > 0)
            {
                var props = string.Join("\n", ev.Properties.Select(
                    kv => $"{kv.Key} = {kv.Value}"));
                AddRow(loc.GetString("DiagnosticsDetailProperties"), props, true);
            }
            AddRow(loc.GetString("DiagnosticsDetailMessage"), ev.Message, true);
            if (!string.IsNullOrEmpty(ev.ExceptionText))
                AddRow(loc.GetString("DiagnosticsDetailException"), ev.ExceptionText, true);

            var dialog = new ContentDialog
            {
                Title = loc.GetString("DiagnosticsDetail"),
                Content = new ScrollViewer { Content = panel, MaxHeight = 480 },
                PrimaryButtonText = loc.GetString("DiagnosticsCopy"),
                CloseButtonText = loc.GetString("DiagnosticsClose"),
                XamlRoot = XamlRoot,
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var package = new DataPackage();
                package.SetText(DiagnosticsPageViewModel.FormatExportLine(ev));
                Clipboard.SetContent(package);
            }
        }
        catch (Exception ex)
        {
            LoggingService.Log.Error(ex, "Show event detail failed");
        }
    }

    private void RefreshLogsButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.RefreshCommand.Execute(null);
        RenderStatus();
        RenderMetrics();
        RenderLogState();
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ClearSearchCommand.Execute(null);
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var package = new DataPackage();
            package.SetText(ViewModel.GetFilteredExportText());
            Clipboard.SetContent(package);
            NotificationService.Current.Success(
                LocalizationService.Current.GetString("DiagnosticsTitle"),
                LocalizationService.Current.GetString("DiagnosticsCopied"));
        }
        catch (Exception ex)
        {
            LoggingService.Log.Error(ex, "Copy logs failed");
        }
    }

    private async void SaveViewButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.SaveViewCommand.ExecuteAsync(null);
        ReportExportResult();
    }

    private async void ExportBundleButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.ExportBundleCommand.ExecuteAsync(null);
        ReportExportResult();
    }

    private void ReportExportResult()
    {
        try
        {
            var loc = LocalizationService.Current;
            if (ViewModel.LastExportError)
                NotificationService.Current.Error(
                    loc.GetString("DiagnosticsTitle"), loc.GetString("DiagnosticsExportFailed"));
            else if (!string.IsNullOrEmpty(ViewModel.LastExportPath))
                NotificationService.Current.Success(
                    loc.GetString("DiagnosticsTitle"), loc.GetString("DiagnosticsExportDone"));
        }
        catch { }
    }

    private void OpenLogFolderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{DiagnosticsService.LogDirectoryPath}\"",
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            LoggingService.Log.Error(ex, "Open log folder failed");
        }
    }
}
