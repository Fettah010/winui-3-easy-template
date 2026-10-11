using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

/// <summary>
/// v0.7.0 (WB-02): the accessibility contract, enforced headlessly by scanning
/// app XAML sources — same source-scan approach as <c>XamlSymbolAuditTests</c> /
/// <c>TextScalingAuditTests</c>. Rules: every button/link has a name (Content,
/// Name, or code-assigned Content in the matching code-behind); every input has
/// a name (Header, Name, TextBox placeholder, CheckBox/RadioButton content);
/// every icon/image is Raw or named; titles/captions carry heading levels; the
/// live-region wiring (Polite statuses, Assertive errors) is present. Narrator
/// runs, high-contrast and 200% scaling remain Live-UI manual evidence
/// (<c>docs/accessibility.md</c>); this test keeps the tree honest between
/// those passes. <c>SplashScreen.xaml</c> is excluded: the transient
/// pre-localization window carries hardcoded strings by design (localization
/// is not initialized yet; code-behind sets version/status immediately).
/// </summary>
[TestClass]
public class A11yAuditTests
{
    // Start tags may span lines: ((?:"[^"]*"|[^">])*) consumes quoted
    // attribute values and everything else up to the closing ">".
    private static readonly Regex ButtonTag = new(
        @"<(Button|HyperlinkButton)\b(?!\.)((?:""[^""]*""|[^"">])*)>",
        RegexOptions.Compiled);
    private static readonly Regex InputTag = new(
        @"<((?:\w+:)?(?:ToggleSwitch|ComboBox|TextBox|CheckBox|RadioButton|Slider|PipsPager|Segmented|ListView|GridView))\b(?!\.)((?:""[^""]*""|[^"">])*)>",
        RegexOptions.Compiled);
    private static readonly Regex IconTag = new(
        @"<(FontIcon|Image)\b(?!\.)((?:""[^""]*""|[^"">])*)>",
        RegexOptions.Compiled);
    private static readonly Regex TextTag = new(
        @"<TextBlock\b(?!\.)((?:""[^""]*""|[^"">])*)>",
        RegexOptions.Compiled);
    private static readonly Regex NameAttr = new(
        @"x:Name\s*=\s*""([^""]+)""",
        RegexOptions.Compiled);
    private static readonly Regex CodeContentAssign = new(
        @"(\w+)\.Content\s*=",
        RegexOptions.Compiled);

    [TestMethod]
    public void Buttons_HaveAccessibleNames()
    {
        var offenders = new List<string>();
        foreach (var (rel, text) in AppXamlFiles())
        {
            var codeLabeled = CodeLabeledNames(rel);
            foreach (Match m in ButtonTag.Matches(text))
            {
                string tag = m.Groups[2].Value;
                if (tag.Contains("Content=") || tag.Contains("AutomationProperties.Name="))
                    continue;
                var name = NameAttr.Match(tag);
                if (name.Success && codeLabeled.Contains(name.Groups[1].Value))
                    continue;
                offenders.Add(rel + ": <" + m.Groups[1].Value + "> " + FirstLine(tag));
            }
        }
        Assert.IsEmpty(offenders, "Buttons without an accessible name (need Content=, AutomationProperties.Name=, or code-behind Content assignment): " + string.Join(" | ", offenders));
    }

    [TestMethod]
    public void Inputs_HaveAccessibleNames()
    {
        var offenders = new List<string>();
        foreach (var (rel, text) in AppXamlFiles())
        {
            foreach (Match m in InputTag.Matches(text))
            {
                string kind = m.Groups[1].Value;
                string shortKind = kind.Contains(':') ? kind.Split(':')[1] : kind;
                string tag = m.Groups[2].Value;
                if (tag.Contains("Header=") || tag.Contains("AutomationProperties.Name="))
                    continue;
                if (shortKind == "TextBox" && tag.Contains("PlaceholderText="))
                    continue;
                if ((shortKind == "CheckBox" || shortKind == "RadioButton") && tag.Contains("Content="))
                    continue;
                offenders.Add(rel + ": <" + kind + "> " + FirstLine(tag));
            }
        }
        Assert.IsEmpty(offenders, "Inputs without an accessible name: " + string.Join(" | ", offenders));
    }

    [TestMethod]
    public void Icons_AreNamedOrRaw()
    {
        var offenders = new List<string>();
        foreach (var (rel, text) in AppXamlFiles())
        {
            foreach (Match m in IconTag.Matches(text))
            {
                string tag = m.Groups[2].Value;
                if (tag.Contains("AccessibilityView=\"Raw\"") || tag.Contains("AutomationProperties.Name="))
                    continue;
                offenders.Add(rel + ": <" + m.Groups[1].Value + "> " + FirstLine(tag));
            }
        }
        Assert.IsEmpty(offenders, "Icons/images that are neither Raw nor named (decorative icons must be Raw): " + string.Join(" | ", offenders));
    }

    [TestMethod]
    public void TitlesAndCaptions_HaveHeadingLevels()
    {
        var offenders = new List<string>();
        foreach (var (rel, text) in AppXamlFiles())
        {
            foreach (Match m in TextTag.Matches(text))
            {
                string tag = m.Groups[2].Value;
                bool isTitle = tag.Contains("TitleTextBlockStyle") || tag.Contains("SubtitleTextBlockStyle");
                bool isSectionCaption = (tag.Contains("BodyStrongTextBlockStyle") && tag.Contains("loc:Loc Key="))
                    || (tag.Contains("CaptionTextBlockStyle") && tag.Contains("FontWeight=\"SemiBold\""));
                if ((isTitle || isSectionCaption) && !tag.Contains("HeadingLevel="))
                    offenders.Add(rel + ": " + FirstLine(tag));
            }
        }
        Assert.IsEmpty(offenders, "Titles/captions without a heading level: " + string.Join(" | ", offenders));
    }

    [TestMethod]
    public void LiveRegions_AreWired()
    {
        // The contract (docs/accessibility.md): status text announces Polite,
        // validation errors interrupt Assertive, the applog tail stays silent.
        // Flag-dropped pages (wizard without setup, Diagnostics without
        // health) are skipped relationally — the matrix runs every combo, so
        // each file's asserts execute wherever the file exists.
        AssertPolite("MainWindow.xaml", 1, "toast host");
        AssertPolite("Pages\\SettingsPage.xaml", 2, "update + account status");
        AssertPolite("Pages\\DiagnosticsPage.xaml", 1, "log count");
        AssertPolite("Controls\\UpdatePopup.xaml", 1, "update status");

        string? wizard = TryReadXaml("Pages\\SetupWizardPage.xaml");
        if (wizard is not null)
            Assert.AreEqual(1, Count(wizard, "AutomationProperties.LiveSetting=\"Assertive\""),
                "SetupWizardPage.xaml must wire exactly one Assertive live region (location validation).");

        string? root = FindRepoRoot();
        Assert.IsNotNull(root, "Could not locate repo root (no .sln found walking up).");
        string cardCodePath = Path.Combine(root!, "Controls", "NotificationCard.xaml.cs");
        if (File.Exists(cardCodePath))
            Assert.Contains("AutomationLiveSetting.Assertive", File.ReadAllText(cardCodePath),
                "NotificationCard must escalate error cards to Assertive in code (ApplyTypeVisuals).");

        string diagCodePath = Path.Combine(root!, "Pages", "DiagnosticsPage.xaml.cs");
        if (File.Exists(diagCodePath))
            Assert.Contains("SetLiveSetting(LiveEmptyText", File.ReadAllText(diagCodePath),
                "Diagnostics live-empty text must set its urgency in code (Assertive on filter errors, Polite otherwise).");
    }

    private static void AssertPolite(string rel, int expected, string what)
    {
        string? text = TryReadXaml(rel);
        if (text is null)
            return;
        Assert.AreEqual(expected, Count(text, "AutomationProperties.LiveSetting=\"Polite\""),
            rel + " must wire exactly " + expected + " Polite live region(s) (" + what + ").");
    }

    private static int Count(string text, string value)
    {
        int count = 0, i = 0;
        while ((i = text.IndexOf(value, i, StringComparison.Ordinal)) >= 0)
        {
            count++;
            i += value.Length;
        }
        return count;
    }

    private static string FirstLine(string tag)
    {
        string flat = tag.Replace("\r", " ").Replace("\n", " ");
        flat = Regex.Replace(flat, @"\s+", " ").Trim();
        return flat.Length <= 90 ? flat : string.Concat(flat.AsSpan(0, 90), "…");
    }

    private static HashSet<string> CodeLabeledNames(string rel)
    {
        // Buttons whose Content is assigned in the matching code-behind
        // (dialog buttons, event-count pills) are named at runtime.
        var names = new HashSet<string>(StringComparer.Ordinal);
        string? root = FindRepoRoot();
        if (root is null)
            return names;
        // Code-behind keeps the full XAML name (Foo.xaml.cs), so append —
        // ChangeExtension would wrongly yield Foo.cs.
        string code = Path.Combine(root, rel) + ".cs";
        if (!File.Exists(code))
            return names;
        string text;
        try { text = File.ReadAllText(code); } catch { return names; }
        foreach (Match m in CodeContentAssign.Matches(text))
            names.Add(m.Groups[1].Value);
        return names;
    }

    private static string? TryReadXaml(string rel)
    {
        string? root = FindRepoRoot();
        if (root is null)
            return null;
        string path = Path.Combine(root, rel);
        if (!File.Exists(path))
            return null;
        try { return File.ReadAllText(path); }
        catch { return null; }
    }

    private static IEnumerable<(string Rel, string Text)> AppXamlFiles()
    {
        string? root = FindRepoRoot();
        Assert.IsNotNull(root, "Could not locate repo root (no .sln found walking up).");
        foreach (string file in Directory.EnumerateFiles(root!, "*.xaml", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(root!, file);
            if (rel.Split(Path.DirectorySeparatorChar).Any(s =>
                s.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                s.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
                s.Equals("TestResults", StringComparison.OrdinalIgnoreCase) ||
                s.Equals("docs", StringComparison.OrdinalIgnoreCase) ||
                s.Equals("Templates", StringComparison.OrdinalIgnoreCase)))
                continue;
            if (Path.GetFileName(file).Equals("SplashScreen.xaml", StringComparison.OrdinalIgnoreCase))
                continue;
            string text;
            try { text = File.ReadAllText(file); } catch { continue; }
            yield return (rel, text);
        }
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
