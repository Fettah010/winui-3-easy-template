using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevTemWinUi3.Services;

namespace DevTemWinUi3.ViewModels;

public partial class DiagnosticsPageViewModel : ObservableObject
{
    public ObservableCollection<string> LogFiles { get; } = new();

    [ObservableProperty]
    private DiagnosticsStatus? _status;

    [ObservableProperty]
    private string? _selectedLogFile;

    [ObservableProperty]
    private string _logText = string.Empty;

    public DiagnosticsPageViewModel()
    {
        Refresh();
    }

    partial void OnSelectedLogFileChanged(string? value) => LoadTail();

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

            if (LogFiles.Count == 0)
            {
                SelectedLogFile = null;
                LogText = string.Empty;
                return;
            }

            if (SelectedLogFile is null || !LogFiles.Contains(SelectedLogFile))
                SelectedLogFile = LogFiles[0];
            else
                LoadTail();
        }
        catch { }
    }

    private void LoadTail()
    {
        try
        {
            if (string.IsNullOrEmpty(SelectedLogFile))
            {
                LogText = string.Empty;
                return;
            }
            var full = DiagnosticsService.GetLogFiles()
                .FirstOrDefault(p => Path.GetFileName(p) == SelectedLogFile);
            LogText = full is null ? string.Empty : DiagnosticsService.ReadLogTail(full);
        }
        catch
        {
            LogText = string.Empty;
        }
    }
}
