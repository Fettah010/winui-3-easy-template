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
    public async Task<string?> PickSaveFileAsync(string suggestedFileName)
    {
        try
        {
            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = suggestedFileName,
            };
            picker.FileTypeChoices.Add("JSON", new List<string> { ".json" });
            InitializeWithWindow(picker);

            var file = await picker.PickSaveFileAsync();
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

            var file = await picker.PickSingleFileAsync();
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
