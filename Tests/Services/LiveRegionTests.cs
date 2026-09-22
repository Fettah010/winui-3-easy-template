using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

/// <summary>
/// Advancement B3: status surfaces must announce to screen readers —
/// Polite live region on the toast host + the Settings update-status text,
/// Assertive only for error toasts. XAML attributes die silently when a
/// restyle drops them, so this test pins the three seams by source scan
/// (same approach as <c>XamlSymbolAuditTests</c>).
/// </summary>
[TestClass]
public class LiveRegionTests
{
    [TestMethod]
    public void ToastHost_IsPoliteLiveRegion()
    {
        string xaml = ReadAppFile("MainWindow.xaml");
        Assert.Contains("x:Name=\"NotificationHost\"", xaml);
        Assert.Contains("AutomationProperties.LiveSetting=\"Polite\"", xaml);
    }

    [TestMethod]
    public void SettingsUpdateStatus_IsPoliteLiveRegion()
    {
        string xaml = ReadAppFile(Path.Combine("Pages", "SettingsPage.xaml"));
        int at = xaml.IndexOf("x:Name=\"UpdateStatusText\"", StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, at, "UpdateStatusText not found in SettingsPage.xaml.");
        string block = xaml.Substring(at, Math.Min(600, xaml.Length - at));
        Assert.Contains("AutomationProperties.LiveSetting=\"Polite\"", block);
    }

    [TestMethod]
    public void ErrorToasts_EscalateToAssertive()
    {
        string code = ReadAppFile(Path.Combine("Controls", "NotificationCard.xaml.cs"));
        Assert.Contains("AutomationLiveSetting.Assertive", code);
        Assert.Contains("NotificationType.Error", code);
    }

    private static string ReadAppFile(string relative)
    {
        string? root = FindRepoRoot();
        Assert.IsNotNull(root, "Could not locate repo root (no .sln found walking up).");
        string path = Path.Combine(root!, relative);
        Assert.IsTrue(File.Exists(path), "Expected app file missing: " + relative);
        return File.ReadAllText(path);
    }

    private static string? FindRepoRoot()
    {
        string? dir = AppContext.BaseDirectory;
        for (int i = 0; i < 12 && dir is not null; i++)
        {
            try
            {
                if (Directory.EnumerateFiles(dir, "*.sln").Any())
                    return dir;
            }
            catch { }
            dir = Directory.GetParent(dir)?.FullName;
        }
        return null;
    }
}
