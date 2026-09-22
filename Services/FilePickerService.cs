using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace DevTemWinUi3.Services;

/// <summary>
/// WinRT file pickers for settings backup (JSON). The window association
/// (<c>InitializeWithWindow</c>) is required for unpackaged apps and lives
/// here so ViewModels stay UI-free and testable via
/// <see cref="IFilePickerService"/>. Never throws: failures surface as
/// <c>null</c> (treated as cancel).
/// </summary>
public sealed class FilePickerService : IFilePickerService
{
    /// <summary>
    /// How long a native picker may stay open before the call degrades to
    /// cancel. OS-modal dialogs are harness-hostile and can stall forever
    /// under automation or a wedged shell (pain-log #22): the timeout
    /// guarantees the app never hangs — callers treat it as cancel.
    /// </summary>
    internal static TimeSpan PickerTimeout { get; set; } = TimeSpan.FromSeconds(60);

    public async Task<string?> PickSaveFileAsync(string suggestedFileName, string fileExtension = ".json")
    {
        try
        {
            string ext = string.IsNullOrWhiteSpace(fileExtension) ? ".json" : fileExtension;
            if (!ext.StartsWith('.'))
                ext = "." + ext;
            string label = "Files";
            if (ext.Equals(".json", StringComparison.OrdinalIgnoreCase))
                label = "JSON";
            else if (ext.Equals(".log", StringComparison.OrdinalIgnoreCase))
                label = "Log files";
            else if (ext.Equals(".zip", StringComparison.OrdinalIgnoreCase))
                label = "ZIP archive";
            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = suggestedFileName,
            };
            picker.FileTypeChoices.Add(label, new List<string> { ext });
            InitializeWithWindow(picker);

            var pickTask = picker.PickSaveFileAsync().AsTask();
            var completed = await Task.WhenAny(pickTask, Task.Delay(PickerTimeout));
            if (!ReferenceEquals(completed, pickTask))
            {
                try { AppLog.Warning("FilePicker: save picker timed out after {0}s; degrading to cancel.", PickerTimeout.TotalSeconds); } catch { }
                return null;
            }
            var file = await pickTask;
            return file?.Path;
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> PickOpenFileAsync()
    {
        try
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            };
            picker.FileTypeFilter.Add(".json");
            InitializeWithWindow(picker);

            var pickTask = picker.PickSingleFileAsync().AsTask();
            var completed = await Task.WhenAny(pickTask, Task.Delay(PickerTimeout));
            if (!ReferenceEquals(completed, pickTask))
            {
                try { AppLog.Warning("FilePicker: open picker timed out after {0}s; degrading to cancel.", PickerTimeout.TotalSeconds); } catch { }
                return null;
            }
            var file = await pickTask;
            return file?.Path;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Associates a WinRT picker with the main window (required for
    /// unpackaged apps). No-op when the window is unavailable.
    /// </summary>
    private static void InitializeWithWindow(object picker)
    {
        try
        {
            if (App.Current is App app && app.MainWindowInstance is not null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(app.MainWindowInstance);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            }
        }
        catch { }
    }
}
