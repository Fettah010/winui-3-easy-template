using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.SmokeTests;

// FlaUI smoke tests: prove the shipped exe actually launches, navigates,
// and persists theme/language — the one thing the headless unit tests
// cannot cover. Smoke, not coverage: four tests, one shared app launch,
// total budget ~60s.
//
// The tests drive the REAL app, so they touch REAL user settings
// (%LocalAppData%\DevTemWinUi3\settings.json). Theme and language are
// always restored in finally blocks; the first-run dialog is dismissed
// once (any manual launch would do the same). Run on demand or in CI —
// never side-by-side with a manually running app instance (single-instance
// enforcement would attach us to the wrong window; ClassInit refuses).
[TestClass]
public sealed class AppSmokeTests
{
    private const string AppWindowTitle = "DevTem-WinUI 3";
    private const string AppProcessName = "DevTemWinUi3";

    // All waits poll (FlaUI Retry) — never a blind Sleep.
    private static readonly TimeSpan LaunchTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DialogTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan RecheckTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan NavigateTimeout = TimeSpan.FromSeconds(5);

    private static Application? _app;
    private static UIA3Automation? _automation;
    private static Window? _window;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext context)
    {
        var exePath = LocateAppExe();
        if (exePath is null)
        {
            Assert.Inconclusive(
                "App exe not found. Build the app first: dotnet build -c Debug -p:Platform=x64");
            return;
        }

        // Single-instance enforcement means a second launch would just wake
        // the already-running window and exit — refuse instead of driving
        // a window we do not own.
        if (Process.GetProcessesByName(AppProcessName).Length > 0)
        {
            Assert.Inconclusive(
                $"A {AppProcessName} process is already running. Close it before running smoke tests.");
            return;
        }

        _automation = new UIA3Automation();
        _app = Application.Launch(exePath);

        // The splash screen clears its own title at runtime, so the title
        // match uniquely identifies the main window once it activates.
        // Window enumeration can throw transiently while the splash tears
        // down — swallow and keep polling until the timeout.
        var found = Retry.WhileNull(
            TryFindMainWindow,
            LaunchTimeout,
            TimeSpan.FromMilliseconds(500));
        _window = found.Result;
        Assert.IsNotNull(_window, $"Main window '{AppWindowTitle}' did not appear within {LaunchTimeout.TotalSeconds}s.");

        DismissFirstRunDialogIfPresent(DialogTimeout);
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        // Close() would only hide to tray (HandleWindowClose); the process
        // is ours, so kill it outright.
        try { _app?.Kill(); } catch { }
        try { _app?.Dispose(); } catch { }
        try { _automation?.Dispose(); } catch { }
        _app = null;
        _automation = null;
        _window = null;
    }

    [TestCleanup]
    public void CaptureScreenshotOnFailure()
    {
        if (TestContext.CurrentTestOutcome == UnitTestOutcome.Passed)
            return;
        try
        {
            var window = _window;
            if (window is null)
                return;
            var dir = TestContext.TestResultsDirectory;
            if (string.IsNullOrEmpty(dir))
                return;
            Directory.CreateDirectory(dir);
            window.Capture().Save(Path.Combine(dir, $"{TestContext.TestName}.png"), System.Drawing.Imaging.ImageFormat.Png);
            TestContext.WriteLine($"Screenshot saved for failed test {TestContext.TestName}.");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Screenshot capture failed: {ex.Message}");
        }
    }

    [TestMethod]
    public void App_Launches_And_ShowsHome()
    {
        var window = RequireWindow();
        DismissFirstRunDialogIfPresent(RecheckTimeout);

        Assert.AreEqual(AppWindowTitle, window.Title);
        var title = WaitForElement("HomeTitleText", NavigateTimeout);
        Assert.IsNotNull(title, "Home page title did not appear after launch.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(title.Name), "Home page title is empty.");
    }

    [TestMethod]
    public void Can_Navigate_All_Pages()
    {
        RequireWindow();
        DismissFirstRunDialogIfPresent(RecheckTimeout);

        ClickNavAndWaitForPage("NavAboutItem", "AboutTitleText");
        ClickNavAndWaitForPage("NavSettingsItem", "SettingsTitleText");
        ClickNavAndWaitForPage("NavHomeItem", "HomeTitleText");
    }

    [TestMethod]
    public void Theme_Switch_Applies()
    {
        RequireWindow();
        DismissFirstRunDialogIfPresent(RecheckTimeout);
        EnsureEnglish();
        ClickNavAndWaitForPage("NavSettingsItem", "SettingsTitleText");

        var combo = RequireElement("ThemeComboBox", "Theme combo box").AsComboBox();
        Assert.IsNotNull(combo.SelectedItem, "Theme combo has no selected item.");
        string initialText = combo.SelectedItem.Text;
        try
        {
            // Read the item text back first: theme labels are localized, so
            // the assertion compares against the live item, not a literal.
            string darkText = combo.Items[2].Text;
            combo.Select(2);
            bool settled = Retry.WhileFalse(() => combo.SelectedItem.Text == darkText, NavigateTimeout).Success;
            Assert.IsTrue(settled, "Theme combo did not settle on Dark.");
            Assert.AreEqual(darkText, combo.SelectedItem.Text, "Theme combo read-back mismatch after selecting Dark.");
        }
        finally
        {
            combo.Select(initialText);
        }

        bool restored = Retry.WhileFalse(() => combo.SelectedItem.Text == initialText, NavigateTimeout).Success;
        Assert.IsTrue(restored, "Theme combo did not restore its original selection.");
    }

    [TestMethod]
    public void Language_Switch_Applies()
    {
        RequireWindow();
        DismissFirstRunDialogIfPresent(RecheckTimeout);
        ClickNavAndWaitForPage("NavSettingsItem", "SettingsTitleText");

        var combo = RequireElement("LanguageComboBox", "Language combo box").AsComboBox();
        try
        {
            combo.Select(1); // es-ES
            var home = RequireElement("NavHomeItem", "Home nav item");
            Assert.IsTrue(
                Retry.WhileFalse(
                    () => string.Equals(home.Name, "Inicio", StringComparison.Ordinal),
                    NavigateTimeout).Success,
                $"Nav Home label did not switch to Spanish (still '{home.Name}').");
        }
        finally
        {
            combo.Select(0); // en-US
        }

        var homeRestored = RequireElement("NavHomeItem", "Home nav item");
        Assert.IsTrue(
            Retry.WhileFalse(
                () => string.Equals(homeRestored.Name, "Home", StringComparison.Ordinal),
                NavigateTimeout).Success,
            $"Nav Home label did not switch back to English (still '{homeRestored.Name}').");
    }

    // -- helpers ----------------------------------------------------------

    private static Window RequireWindow()
    {
        Assert.IsNotNull(_window, "App window is not available (ClassInit did not complete).");
        return _window;
    }

    private static Window? TryFindMainWindow()
    {
        try
        {
            var app = _app;
            var automation = _automation;
            if (app is null || automation is null)
                return null;
            return app.GetAllTopLevelWindows(automation)
                .FirstOrDefault(w => string.Equals(w.Title, AppWindowTitle, StringComparison.Ordinal));
        }
        catch
        {
            return null;
        }
    }

    private static AutomationElement RequireElement(string automationId, string description)
    {
        var element = WaitForElement(automationId, NavigateTimeout);
        Assert.IsNotNull(element, $"{description} (AutomationId '{automationId}') did not appear.");
        return element;
    }

    private static AutomationElement? WaitForElement(string automationId, TimeSpan timeout)
    {
        var window = _window;
        if (window is null)
            return null;
        return Retry.WhileNull(
            () => window.FindFirstDescendant(cf => cf.ByAutomationId(automationId)),
            timeout,
            TimeSpan.FromMilliseconds(250)).Result;
    }

    private static void ClickNavAndWaitForPage(string navAutomationId, string titleAutomationId)
    {
        var navItem = RequireElement(navAutomationId, "Nav item");
        navItem.Click();
        var title = WaitForElement(titleAutomationId, NavigateTimeout);
        Assert.IsNotNull(title, $"Page title '{titleAutomationId}' did not appear after clicking '{navAutomationId}'.");
    }

    /// <summary>
    /// Restores English when a previous (possibly failed) run left another
    /// language active, so theme assertions stay deterministic.
    /// </summary>
    private static void EnsureEnglish()
    {
        var home = RequireElement("NavHomeItem", "Home nav item");
        if (string.Equals(home.Name, "Home", StringComparison.Ordinal))
            return;
        var combo = RequireElement("LanguageComboBox", "Language combo box").AsComboBox();
        combo.Select(0);
        Assert.IsTrue(
            Retry.WhileFalse(
                () => string.Equals(home.Name, "Home", StringComparison.Ordinal),
                NavigateTimeout).Success,
            "Could not restore English before the theme test.");
    }

    /// <summary>
    /// Clicks the first-run "Get Started" button in any supported language
    /// when it is present; a no-op otherwise (already shown on this machine).
    /// </summary>
    private static void DismissFirstRunDialogIfPresent(TimeSpan timeout)
    {
        var window = _window;
        if (window is null)
            return;
        string[] buttonNames = ["Get Started", "Empezar", "Commencer"];
        var found = Retry.WhileNull<AutomationElement?>(
            () =>
            {
                foreach (var name in buttonNames)
                {
                    var button = window.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button).And(cf.ByName(name)));
                    if (button is not null)
                        return button;
                }
                return null;
            },
            timeout,
            TimeSpan.FromMilliseconds(500)).Result;
        try { found?.Click(); } catch { }
    }

    /// <summary>
    /// Locates the built app exe: the Debug output under the repo root
    /// (identified by the solution file), preferring the platform-specific
    /// folder, with a newest-match fallback. Never resolves to a test-runner
    /// output directory.
    /// </summary>
    private static string? LocateAppExe()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 8 && dir is not null; i++, dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "DevTemWinUi3.sln")))
            {
                // `dotnet build -p:Platform=x64` (the repo's canonical build)
                // emits under bin\x64\... without an RID folder in Debug.
                string[] candidates =
                [
                    Path.Combine(dir.FullName, "bin", "x64", "Debug", "net10.0-windows10.0.19041.0", $"{AppProcessName}.exe"),
                    Path.Combine(dir.FullName, "bin", "Debug", "net10.0-windows10.0.19041.0", "win-x64", $"{AppProcessName}.exe"),
                    Path.Combine(dir.FullName, "bin", "Debug", "net10.0-windows10.0.19041.0", $"{AppProcessName}.exe"),
                ];
                foreach (var candidate in candidates)
                {
                    if (File.Exists(candidate))
                        return candidate;
                }

                var fallback = new DirectoryInfo(Path.Combine(dir.FullName, "bin"))
                    .EnumerateFiles($"{AppProcessName}.exe", SearchOption.AllDirectories)
                    .Where(f => !f.FullName.Contains("Tests", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .FirstOrDefault();
                return fallback?.FullName;
            }
        }
        return null;
    }
}
