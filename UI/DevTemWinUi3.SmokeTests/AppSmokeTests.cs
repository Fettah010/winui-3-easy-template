using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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
//
// Occupied desktops: UIA reads work through occluding windows, but mouse
// clicks land on whatever is on top. ClassInit therefore stages the run —
// foreground + topmost window, physical cursor confined to its bounds
// (ClipCursor; released in ClassCleanup) — and nav clicks retry with a
// refocus. A full OS input block (BlockInput) is deliberately NOT used:
// it would swallow FlaUI's own synthetic clicks too.
[TestClass]
public sealed class AppSmokeTests
{
    private static string AppWindowTitle => Environment.GetEnvironmentVariable("DEVTEM_SMOKE_TITLE") ?? "DevTem-WinUI 3";
    private static string AppProcessName => Environment.GetEnvironmentVariable("DEVTEM_SMOKE_PROCESS") ?? "DevTemWinUi3";

    // All waits poll (FlaUI Retry) — never a blind Sleep.
    private static readonly TimeSpan LaunchTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DialogTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan RecheckTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan NavigateTimeout = TimeSpan.FromSeconds(5);

    private static Application? _app;
    private static UIA3Automation? _automation;
    private static Window? _window;

    #region P/Invoke (cursor confinement)

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ClipCursor(ref RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ClipCursor(IntPtr lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private static readonly IntPtr HWND_NOTOPMOST = new(-2);
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_SHOWWINDOW = 0x0040;

    #endregion

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
        if (_window is null)
        {
            // Say WHAT we saw: with a splash + single-instance dance, "not
            // found" without the candidate list is undebuggable. Also report
            // whether the process died (with its exit code) vs is just slow —
            // CI triage depends on that distinction.
            var app = _app;
            var automation = _automation;
            string seen = "(session not started)";
            string process = "(session not started)";
            if (app is not null && automation is not null)
            {
                try
                {
                    if (app.HasExited)
                        process = $"exited (code {app.ExitCode})";
                    else
                        process = $"alive (id {app.ProcessId})";
                }
                catch (Exception ex)
                {
                    process = "(state unknown: " + ex.Message + ")";
                }
                try
                {
                    var titles = app.GetAllTopLevelWindows(automation)
                        .Select(w => "'" + w.Title + "' (enabled=" + w.IsEnabled + ")");
                    seen = string.Join(" | ", titles);
                    if (string.IsNullOrWhiteSpace(seen))
                        seen = "(no top-level windows)";
                }
                catch (Exception ex)
                {
                    seen = "(enumeration failed: " + ex.Message + ")";
                }
            }
            Assert.Fail($"Main window '{AppWindowTitle}' did not appear within {LaunchTimeout.TotalSeconds}s. Process: {process}. Top-level windows: {seen}.");
        }

        // Stage the run before touching anything: foreground + topmost so
        // clicks cannot land on an occluding window, cursor confined so a
        // stray physical mouse cannot drag one over us mid-run.
        ForegroundWindow();
        ConfineCursorToWindow();

        DismissFirstRunDialogIfPresent(DialogTimeout, includeWhatsNew: true);
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        // Release the stage first (order matters: unclip while we still
        // know the window), then kill our process outright — Close() would
        // only hide to tray (HandleWindowClose).
        ReleaseStage();
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
        // First run shows only the welcome dialog (the setup wizard never
        // auto-opens — the installer owns setup): complete the wizard only
        // if it is somehow showing, a no-op otherwise.
        CompleteSetupWizardIfPresent(NavigateTimeout);

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
        ClickNavAndWaitForPage("NavDiagnosticsItem", "DiagnosticsTitleText");
        ClickNavAndWaitForPage("NavSettingsItem", "SettingsTitleText");
        ClickNavAndWaitForPage("NavHomeItem", "HomeTitleText");
    }

    [TestMethod]
    public void UpdateCheck_FromSettings_Resolves()
    {
        // The update surface is toasts + native dialogs (no custom
        // overlay): a manual check must always resolve — toast on
        // up-to-date / not-installed, a native dialog when an update is
        // pending — and leave Settings alive. Regression test for the
        // "checking forever" hang: the check button must come back.
        RequireWindow();
        DismissFirstRunDialogIfPresent(RecheckTimeout);
        ClickNavAndWaitForPage("NavSettingsItem", "SettingsTitleText");

        ForegroundWindow();
        var check = RequireElement("CheckUpdatesButton", "Check-for-updates button");
        InvokeOrClick(check);

        // Dismiss whatever the check raised (native dialog buttons or the
        // legacy popup), then require the page to still be alive. Toasts
        // need no dismissal: the loop exits once the page is responsive
        // with no dialog in front of it.
        DismissUpdateUiIfPresent(TimeSpan.FromSeconds(35));

        var settings = WaitForElement("SettingsTitleText", NavigateTimeout);
        Assert.IsNotNull(settings, "Settings page did not stay alive after update check.");
        var button = WaitForElement("CheckUpdatesButton", RecheckTimeout);
        Assert.IsNotNull(button, "Check button missing after update check.");
    }

    [TestMethod]
    public void SetupWizard_Complete_Path()
    {
        RequireWindow();
        DismissFirstRunDialogIfPresent(RecheckTimeout);

        // First-run only: on repeat runs the wizard is already completed
        // (persisted don't-show-again) and this path is a no-op pass.
        // (Test order is not guaranteed — App_Launches may have completed
        // it first via the same helper.)
        if (!CompleteSetupWizardIfPresent(RecheckTimeout))
            return;
        var home = WaitForElement("HomeTitleText", NavigateTimeout);
        Assert.IsNotNull(home, "Home page did not appear after wizard Complete.");
    }

    [TestMethod]
    public void Generated_Page_Navigation_Works()
    {
        var navAutomationId = Environment.GetEnvironmentVariable("DEVTEM_SMOKE_PAGE_NAV_ID");
        var titleAutomationId = Environment.GetEnvironmentVariable("DEVTEM_SMOKE_PAGE_TITLE_ID");
        if (string.IsNullOrWhiteSpace(navAutomationId) || string.IsNullOrWhiteSpace(titleAutomationId))
        {
            Assert.Inconclusive("Generated-page smoke variables are not configured.");
            return;
        }

        RequireWindow();
        DismissFirstRunDialogIfPresent(RecheckTimeout);
        ClickNavAndWaitForPage(navAutomationId, titleAutomationId);
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
        // Re-foreground before every test: an occupied desktop may have
        // covered us since the last one (UIA reads work occluded, mouse
        // clicks land on whatever is on top).
        ForegroundWindow();
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

    /// <summary>
    /// Activates an element without a mouse: UIA Invoke works even when the
    /// target is scrolled out of view (small CI screens), where a synthetic
    /// click would land on whatever happens to be at those coordinates.
    /// Falls back to a mouse click when the pattern is unavailable.
    /// </summary>
    private static void InvokeOrClick(AutomationElement element)
    {
        try
        {
            var invoke = element.Patterns.Invoke.PatternOrDefault;
            if (invoke is not null)
            {
                invoke.Invoke();
                return;
            }
        }
        catch { }
        try { element.Focus(); } catch { }
        try { element.Click(); } catch { }
    }

    private static void ClickNavAndWaitForPage(string navAutomationId, string titleAutomationId)
    {
        // A click can still miss (stray overlay, focus race): refocus and
        // retry a few times before calling it a navigation failure.
        const int attempts = 3;
        for (int i = 1; i <= attempts; i++)
        {
            ForegroundWindow();
            var navItem = RequireElement(navAutomationId, "Nav item");
            try { navItem.Focus(); } catch { }
            try { navItem.Click(); } catch { }
            var title = WaitForElement(titleAutomationId, NavigateTimeout);
            if (title is not null)
                return;
        }
        Assert.Fail($"Page title '{titleAutomationId}' did not appear after clicking '{navAutomationId}' ({attempts} attempts).");
    }

    /// <summary>
    /// Brings our window above everything else (topmost pins it there for
    /// the whole run; <see cref="ReleaseStage"/> unpins). Best-effort: a
    /// failure here must never fail a test by itself (click retries
    /// compensate).
    /// </summary>
    private static void ForegroundWindow()
    {
        var window = _window;
        if (window is null)
            return;
        try { window.Focus(); } catch { }
        try { SetTopmost(true); } catch { }
    }

    /// <summary>Current main-window handle of our app process, if known.</summary>
    private static IntPtr MainWindowHandle()
    {
        try
        {
            var app = _app;
            if (app is null)
                return IntPtr.Zero;
            using var process = Process.GetProcessById(app.ProcessId);
            process.Refresh();
            return process.MainWindowHandle;
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    private static void SetTopmost(bool topmost)
    {
        try
        {
            var hwnd = MainWindowHandle();
            if (hwnd == IntPtr.Zero)
                return;
            SetWindowPos(hwnd, topmost ? HWND_TOPMOST : HWND_NOTOPMOST,
                0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        }
        catch { }
    }

    /// <summary>
    /// Confines the PHYSICAL mouse to our window bounds so stray user input
    /// cannot drag another window over the run. Synthetic (FlaUI) input is
    /// unaffected. Best-effort; skipped when bounds are unavailable.
    /// </summary>
    private static void ConfineCursorToWindow()
    {
        try
        {
            var window = _window;
            if (window is null)
                return;
            var bounds = window.BoundingRectangle;
            if (bounds.IsEmpty || bounds.Width < 50 || bounds.Height < 50)
                return;
            var rect = new RECT
            {
                Left = bounds.Left,
                Top = bounds.Top,
                Right = bounds.Right,
                Bottom = bounds.Bottom,
            };
            ClipCursor(ref rect);
        }
        catch { }
    }

    /// <summary>Undoes <see cref="ForegroundWindow"/> + <see cref="ConfineCursorToWindow"/>.</summary>
    private static void ReleaseStage()
    {
        try { ClipCursor(IntPtr.Zero); } catch { }
        try { SetTopmost(false); } catch { }
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
    /// With <paramref name="includeWhatsNew"/>, also clears the what's-new
    /// dialog (a version bump triggers it, and it is modal — nav clicks land
    /// on it instead of the page). The what's-new check runs only at launch:
    /// once dismissed it never returns in the same run, so per-test calls
    /// skip it to stay inside the time budget.
    /// </summary>
    private static void DismissFirstRunDialogIfPresent(TimeSpan timeout, bool includeWhatsNew = false)
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

        if (includeWhatsNew)
            DismissWhatsNewDialogIfPresent(timeout);
    }

    /// <summary>
    /// Drives the first-run setup wizard to completion when it is showing
    /// (fresh machines navigate there instead of Home). Returns true when
    /// the wizard was present; false is a no-op pass (already completed).
    /// Shared by App_Launches (which must tolerate a first-run start) and
    /// the dedicated wizard path test.
    /// </summary>
    private static bool CompleteSetupWizardIfPresent(TimeSpan timeout)
    {
        var title = WaitForElement("SetupWizardTitleText", timeout);
        if (title is null)
        {
            // The pips may render before the title registers: one more
            // probe via the Next button before calling it absent.
            if (WaitForElement("SetupWizardNextButton", RecheckTimeout) is null)
                return false;
        }
        for (int i = 0; i < 4; i++)
        {
            var nextButton = WaitForElement("SetupWizardNextButton", NavigateTimeout);
            if (nextButton is null)
                break;
            try { nextButton.Focus(); } catch { }
            try { nextButton.Click(); } catch { }
        }
        var complete = WaitForElement("SetupWizardCompleteButton", NavigateTimeout);
        if (complete is null)
            return true;
        try { complete.Focus(); } catch { }
        try { complete.Click(); } catch { }
        return true;
    }

    /// <summary>
    /// Clicks the what's-new dialog's OK button when a version bump left it
    /// open. Located by dialog title (version-agnostic prefix), never by a
    /// bare "OK" (too generic to click blindly).
    /// </summary>
    private static void DismissWhatsNewDialogIfPresent(TimeSpan timeout)
    {
        var window = _window;
        if (window is null)
            return;
        var found = Retry.WhileNull<AutomationElement?>(
            () =>
            {
                // NOTE: .Name throws PropertyNotSupportedException on some
                // text elements — guard per element, never the whole search.
                var title = window
                    .FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Text))
                    .FirstOrDefault(t =>
                    {
                        try { return t.Name.StartsWith("What's New in v", StringComparison.Ordinal); }
                        catch { return false; }
                    });
                if (title is null)
                    return null;
                return window.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button).And(cf.ByName("OK")));
            },
            timeout,
            TimeSpan.FromMilliseconds(500)).Result;
        try { found?.Click(); } catch { }
    }

    /// <summary>
    /// Dismisses any update UI the check raised and returns once the app is
    /// responsive again: clicks safe-dismiss buttons only (Close / Later /
    /// Cancel / OK, all languages, plus the legacy popup buttons) — never
    /// Install / Restart / Retry, which would advance the flow instead of
    /// closing it. The name search is scoped to modal dialogs: an unscoped
    /// "Close" would match the window-chrome caption button and close the
    /// app. Toasts need no dismissal: the loop also exits once the Settings
    /// page is responsive with no dialog in front of it.
    /// </summary>
    private static void DismissUpdateUiIfPresent(TimeSpan timeout)
    {
        var window = _window;
        if (window is null)
            return;
        string[] safeNames =
        [
            "Close", "Fermer", "Cerrar",
            "Later", "Plus tard", "Más tarde",
            "Cancel", "Annuler", "Cancelar",
            "OK",
        ];
        var deadline = DateTime.UtcNow + timeout;
        var settledSince = DateTime.UtcNow;
        while (DateTime.UtcNow < deadline)
        {
            ForegroundWindow();
            // Legacy overlay buttons first (AutomationId, fastest path).
            var legacy = WaitForElement("UpdatePopupPrimary", TimeSpan.FromMilliseconds(250))
                ?? WaitForElement("UpdatePopupSecondary", TimeSpan.FromMilliseconds(250));
            if (legacy is not null)
            {
                try
                {
                    if (legacy.IsEnabled)
                        InvokeOrClick(legacy);
                }
                catch { }
                settledSince = DateTime.UtcNow;
                Task.Delay(500).Wait();
                continue;
            }
            var safe = FindModalDialogButton(window, safeNames);
            if (safe is not null)
            {
                try { InvokeOrClick(safe); } catch { }
                settledSince = DateTime.UtcNow;
                Task.Delay(500).Wait();
                continue;
            }
            // No dialog in front and the page answers: resolved. Require a
            // short quiet period so a slow-appearing dialog still gets seen.
            var alive = WaitForElement("SettingsTitleText", TimeSpan.FromMilliseconds(250));
            if (alive is not null && DateTime.UtcNow - settledSince > TimeSpan.FromSeconds(3))
                return;
            Task.Delay(500).Wait();
        }
    }

    /// <summary>
    /// Finds a safe-dismiss button inside modal dialogs only (never window
    /// chrome). Null when no modal dialog is open.
    /// </summary>
    private static AutomationElement? FindModalDialogButton(Window window, string[] names)
    {
        try
        {
            foreach (var modal in window.ModalWindows)
            {
                foreach (var name in names)
                {
                    AutomationElement? found = null;
                    try
                    {
                        found = modal.FindFirstDescendant(cf =>
                            cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button).And(cf.ByName(name)));
                    }
                    catch { found = null; }
                    if (found is not null)
                        return found;
                }
            }
        }
        catch { }
        return null;
    }

    /// <summary>
    /// Locates the built app exe: the Debug output under the repo root
    /// (identified by the solution file), preferring the platform-specific
    /// folder, with a newest-match fallback. Never resolves to a test-runner
    /// output directory.
    /// </summary>
    private static string? LocateAppExe()
    {
        var configuredPath = Environment.GetEnvironmentVariable("DEVTEM_SMOKE_EXE");
        if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
            return configuredPath;

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
