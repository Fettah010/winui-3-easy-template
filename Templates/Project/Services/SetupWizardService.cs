using System;
using System.IO;

namespace DevTemWinUi3.Services;

/// <summary>
/// OS side effects for the first-run setup wizard (portable only):
/// data-folder creation, Desktop/Start shortcuts (same WScript.Shell shape
/// as <c>Scripts/create-shortcut.ps1</c>), launch-at-login, and the
/// deep-link registration note. UI-free and never-throw by house rule
/// (registry/COM/file work); untested headless like
/// <see cref="AutoStartService"/> — behavior is proven by the wizard
/// Complete smoke path instead.
/// </summary>
public sealed class SetupWizardService
{
    public static SetupWizardService Current { get; } = new();

    private SetupWizardService() { }

    /// <summary>
    /// Applies the wizard choices. Never throws; each step is independent
    /// so one failure never blocks the others.
    /// </summary>
    public void ApplyChoices(string dataFolder, bool desktopShortcut, bool startShortcut, bool launchAtLogin)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(dataFolder))
                Directory.CreateDirectory(dataFolder);
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Setup wizard: could not create data folder");
        }
        try
        {
            if (desktopShortcut || startShortcut)
                CreateShortcuts(desktopShortcut, startShortcut);
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Setup wizard: shortcut creation failed");
        }
        try
        {
            if (!AppInfo.IsPackaged)
                AutoStartService.Current.SetEnabled(launchAtLogin);
            else
                _ = AutoStartService.Current.SetPackagedStartupEnabledAsync(launchAtLogin);
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Setup wizard: auto-start apply failed");
        }
        try { ProtocolService.EnsureRegistered(); } catch { }
    }

    private static void CreateShortcuts(bool desktop, bool startMenu)
    {
        try
        {
            string? exePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
                return;
            string icon = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
            dynamic shell;
            try { shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!)!; }
            catch { return; }
            try
            {
                if (desktop)
                    WriteShortcut(shell, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), AppMetadata.AppName + ".lnk"), exePath, icon);
                if (startMenu)
                    WriteShortcut(shell, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", AppMetadata.AppName + ".lnk"), exePath, icon);
            }
            finally
            {
                try { System.Runtime.InteropServices.Marshal.ReleaseComObject(shell); } catch { }
            }
        }
        catch { }
    }

    private static void WriteShortcut(dynamic shell, string linkPath, string target, string icon)
    {
        try
        {
            string? dir = Path.GetDirectoryName(linkPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            var link = shell.CreateShortcut(linkPath);
            try
            {
                link.TargetPath = target;
                link.WorkingDirectory = Path.GetDirectoryName(target) ?? string.Empty;
                link.Description = AppMetadata.AppName;
                if (File.Exists(icon))
                    link.IconLocation = icon + ",0";
                link.Save();
            }
            finally
            {
                try { System.Runtime.InteropServices.Marshal.ReleaseComObject(link); } catch { }
            }
        }
        catch { }
    }
}
