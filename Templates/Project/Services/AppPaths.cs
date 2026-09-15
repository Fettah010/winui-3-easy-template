using System;
using System.IO;

namespace DevTemWinUi3.Services;

/// <summary>
/// Writable data locations for both distributions. The app ships as one
/// binary (external MSIX packaging — see <c>Scripts/build-msix.ps1</c>), so
/// every path forks at runtime on <see cref="AppInfo.IsPackaged"/> instead
/// of at scaffold time:
/// unpackaged → <c>%LocalAppData%\&lt;AppDataFolder&gt;</c>;
/// packaged → the package's <c>ApplicationData.Current.LocalFolder</c>
/// (the install directory is read-only and the classic locations may be
/// virtualized). Never throws; falls back down the chain.
/// </summary>
public static class AppPaths
{
    /// <summary>Root for settings, database, logs, and handoff files.</summary>
    public static string DataFolder
    {
        get
        {
            try
            {
                if (AppInfo.IsPackaged)
                {
                    try
                    {
                        var local = Windows.Storage.ApplicationData.Current?.LocalFolder?.Path;
                        if (!string.IsNullOrWhiteSpace(local))
                            return local;
                    }
                    catch { }
                }
            }
            catch { }
            try
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    AppMetadata.AppDataFolder);
            }
            catch
            {
                return Path.Combine(Path.GetTempPath(), AppMetadata.AppDataFolder);
            }
        }
    }
}
