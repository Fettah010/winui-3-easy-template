using System;

namespace DevTemWinUi3.Services;

/// <summary>
/// Windows auto-start via the per-user <c>Run</c> registry key (unpackaged
/// runs, including Velopack installs). MSIX-packaged runs cannot use it
/// (virtualized store) — the Settings toggle disables itself there (see
/// <c>SettingsPageViewModel.AutoStartAvailable</c>). Untested headless by
/// house rule (registry); every path is never-throw guarded.
/// </summary>
public sealed class AutoStartService
{
    public static AutoStartService Current { get; } = new();

    private AutoStartService() { }

    public bool IsEnabled()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", false);
            return key?.GetValue(AppMetadata.AutoStartRegistryName) is not null;
        }
        catch
        {
            return false;
        }
    }

    public void SetEnabled(bool enabled)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (key is null) return;

            if (enabled)
            {
                var exePath = Environment.ProcessPath ?? string.Empty;
                key.SetValue(AppMetadata.AutoStartRegistryName, $"\"{exePath}\"");
                AppLog.Information("Auto-start enabled");
            }
            else
            {
                key.DeleteValue(AppMetadata.AutoStartRegistryName, false);
                AppLog.Information("Auto-start disabled");
            }
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Failed to set auto-start");
        }
    }
}
