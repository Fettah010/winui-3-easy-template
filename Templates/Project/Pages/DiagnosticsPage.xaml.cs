using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using DevTemWinUi3.Services;
using DevTemWinUi3.ViewModels;

namespace DevTemWinUi3.Pages;

public sealed partial class DiagnosticsPage : Page, INavigationAware
{
    public DiagnosticsPageViewModel ViewModel { get; }

    public DiagnosticsPage()
    {
        this.InitializeComponent();
        // ViewModel comes from the container (transient per page), never new.
        ViewModel = ServiceLocator.GetRequiredService<DiagnosticsPageViewModel>();
        DataContext = ViewModel;

        // Composed text below cannot bind: re-render it on language switch.
        // (Unsubscribed in OnNavigatedFrom: this page is not cached, so a
        // static-event subscription would leak every visit.)
        LocalizationService.Current.LanguageChanged += OnLanguageChanged;
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
    }

    private void OnLanguageChanged(object? sender, EventArgs e) => RenderAll();

    /// <summary>
    /// Re-applies everything that cannot live in XAML bindings: the
    /// localized combo-box name and the composed status/placeholder text
    /// (kept in the page so the VM stays UI-free and testable).
    /// </summary>
    private void RenderAll()
    {
        AutomationProperties.SetName(LogFileComboBox,
            LocalizationService.Current.GetString("DiagnosticsLogs"));
        RenderStatus();
        RenderLogPlaceholder();
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

    private void RenderLogPlaceholder()
    {
        if (ViewModel.LogFiles.Count == 0 && string.IsNullOrEmpty(ViewModel.LogText))
            LogText.Text = LocalizationService.Current.GetString("DiagnosticsNoLogs");
    }

    private void RefreshLogsButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.RefreshCommand.Execute(null);
        RenderStatus();
        RenderLogPlaceholder();
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
