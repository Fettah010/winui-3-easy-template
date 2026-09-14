using System.Threading.Tasks;

namespace DevTemWinUi3.Services;

/// <summary>
/// File-picker seam: WinRT pickers need a window handle (unpackaged apps),
/// so the ViewModel programs against this and tests use a fake returning
/// scratch paths. Returns <c>null</c> when the user cancels.
/// </summary>
public interface IFilePickerService
{
    Task<string?> PickSaveFileAsync(string suggestedFileName, string fileExtension = ".json");

    Task<string?> PickOpenFileAsync();
}
