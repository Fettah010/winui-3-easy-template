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

        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        var loc = LocalizationService.Current;
        DiagnosticsTitleText.Text = loc.GetString("DiagnosticsTitle");
        DiagnosticsDescText.Text = loc.GetString("DiagnosticsDesc");
        DiagnosticsStatusLabel.Text = loc.GetString("DiagnosticsStatus");
        DiagnosticsLogsLabel.Text = loc.GetString("DiagnosticsLogs");

        StatusCard.Header = loc.GetString("DiagnosticsStatus");
        LogsCard.Header = loc.GetString("DiagnosticsLogs");
        RefreshLogsButton.Content = loc.GetString("DiagnosticsRefresh");
        OpenLogFolderButton.Content = loc.GetString("DiagnosticsOpenFolder");
        AutomationProperties.SetName(LogFileComboBox, loc.GetString("DiagnosticsLogs"));

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

    public void OnNavigatedTo(object? parameter)
    {
        ViewModel.Refresh();
        ApplyLocalization();
    }

    public void OnNavigatedFrom()
    {
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
