using System;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using DevTemWinUi3.Services;
using DevTemWinUi3.ViewModels;

namespace DevTemWinUi3.Pages;

public sealed partial class DiagnosticsPage : Page, INavigationAware
{
    public DiagnosticsPageViewModel ViewModel { get; }

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
        RenderAll();
    }

    public void OnNavigatedTo(object? parameter)
    {
        ViewModel.Refresh();
        RenderAll();
    }

    public void OnNavigatedFrom()
    {
        LocalizationService.Current.LanguageChanged -= OnLanguageChanged;
        ViewModel.PropertyChanged -= OnViewModelChanged;
    }

    private void OnLanguageChanged(object? sender, EventArgs e) => RenderAll();

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DiagnosticsPageViewModel.LogText)
            or nameof(DiagnosticsPageViewModel.ShownLineCount)
            or nameof(DiagnosticsPageViewModel.TotalLineCount)
            or nameof(DiagnosticsPageViewModel.HasLogs)
            or nameof(DiagnosticsPageViewModel.LogFiles))
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
        LogFileComboBox.PlaceholderText = loc.GetString("DiagnosticsLogs");
        RenderStatus();
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
        StatusText.Text =
            $"{loc.GetString("DiagnosticsVersion")}: {status.AppVersion}\n" +
            $"{loc.GetString("DiagnosticsChannel")}: {status.Channel}\n" +
            $"{loc.GetString("DiagnosticsTheme")}: {status.Theme}\n" +
            $"{loc.GetString("DiagnosticsLanguage")}: {status.Language}\n" +
            $"{loc.GetString("DiagnosticsSentry")}: {onOff}";
    }

    /// <summary>
    /// Count line + empty state. The placeholder lives in its own TextBlock
    /// so the compiled <c>LogText</c> binding is never overwritten.
    /// </summary>
    private void RenderLogState()
    {
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

    private void RefreshLogsButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.RefreshCommand.Execute(null);
        RenderStatus();
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
            package.SetText(ViewModel.LogText);
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
