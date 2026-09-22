using System;
using System.Threading.Tasks;

namespace DevTemWinUi3.Services;

/// <summary>
/// Windows auto-start, both distributions. Unpackaged runs (including
/// Velopack installs) use the per-user <c>Run</c> registry key.
/// MSIX-packaged runs cannot (virtualized store) and go through the
/// manifest <c>StartupTask</c> instead (<see cref="StartupTaskId"/> matches
/// the manifest <c>TaskId</c>, derived from the safe name so renames flow).
/// The sync <see cref="IsEnabled"/>/<see cref="SetEnabled"/> pair is the
/// registry path; packaged runs use the async pair (WinRT calls must not
/// be blocked on the UI thread). Untested headless by house rule
/// (registry + WinRT); every path is never-throw guarded.
/// </summary>
public sealed class AutoStartService
{
    public static AutoStartService Current { get; } = new();

    private AutoStartService() { }

    /// <summary>Manifest StartupTask id (<c>&lt;SafeName&gt;Startup</c>).</summary>
    public static string StartupTaskId => AppMetadata.SafeName + "Startup";

    /// <summary>Registry path (unpackaged). Never throws.</summary>
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
                key.SetValue(AppMetadata.AutoStartRegistryName, BuildRunValue(exePath));
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

    /// <summary>
    /// The <c>Run</c> key value for <paramref name="exePath"/>. Quoting is
    /// load-bearing (same binary-planting rule as
    /// <see cref="ProtocolService.BuildCommandValue"/>). Pure and
    /// headless-testable; the registry write itself stays untested by
    /// house rule.
    /// </summary>
    internal static string BuildRunValue(string exePath) => $"\"{exePath}\"";

    /// <summary>
    /// Packaged equivalent of <see cref="IsEnabled"/> over the manifest
    /// StartupTask. States DisabledByUser/DisabledByPolicy can only be
    /// changed in Windows Settings (returns false, never throws).
    /// </summary>
    public async Task<bool> IsPackagedStartupEnabledAsync()
    {
        try
        {
            var task = await Windows.ApplicationModel.StartupTask.GetAsync(StartupTaskId);
            return task.State == Windows.ApplicationModel.StartupTaskState.Enabled;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Packaged equivalent of <see cref="SetEnabled"/>. Enabling may show
    /// the system consent prompt; returns the resulting state. Never throws.
    /// </summary>
    public async Task<bool> SetPackagedStartupEnabledAsync(bool enabled)
    {
        try
        {
            var task = await Windows.ApplicationModel.StartupTask.GetAsync(StartupTaskId);
            if (enabled)
            {
                var state = await task.RequestEnableAsync();
                var on = state == Windows.ApplicationModel.StartupTaskState.Enabled;
                AppLog.Information("Packaged auto-start enable requested: {State}", state);
                return on;
            }
            task.Disable();
            AppLog.Information("Packaged auto-start disabled");
            return false;
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Failed to set packaged auto-start");
            return false;
        }
    }
}
