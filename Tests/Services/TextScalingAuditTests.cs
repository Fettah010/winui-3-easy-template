using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

/// <summary>
/// Advancement B5 (static half): 200% text scaling + high-contrast survival
/// rests on three invariants — no fixed <c>Height</c> on text elements
/// (fixed heights clip scaled text; caps must scroll), no hardcoded
/// black/white text colors (they ignore high-contrast themes; every brush
/// must be a <c>ThemeResource</c>), and capped regions scroll. The
/// screenshot matrix (200%, all four HC themes, EN/ES, 900px/1920px) is a
/// Live-UI manual pass on top — this test keeps the tree honest between
/// those passes. Same source-scan approach as <c>XamlSymbolAuditTests</c>.
/// </summary>
[TestClass]
public class TextScalingAuditTests
{
    [TestMethod]
    public void NoTextElement_HasFixedHeight()
    {
        // Fixed heights are allowed on chrome (images, rings, progress bars,
        // title bar) but never on elements that render localizable text.
        var offenders = new List<string>();
        var textWithHeight = new Regex(
            @"<(TextBlock|TextBox|RichTextBlock|ContentDialog|TeachingTip)\b[^>]*\bHeight\s*=\s*""[0-9]",
            RegexOptions.Compiled);
        foreach (var (rel, text) in AppXamlFiles())
        {
            foreach (Match m in textWithHeight.Matches(text))
                offenders.Add(rel + ": " + m.Value.Trim());
        }
        Assert.IsEmpty(offenders, "Text elements with fixed Height (clip at 200% scaling): " + string.Join(" | ", offenders));
    }

    [TestMethod]
    public void NoHardcoded_TextColors()
    {
        var offenders = new List<string>();
        var hardcoded = new Regex(
            @"(Foreground|Background)\s*=\s*""(White|Black|#[0-9A-Fa-f]{3,8})""",
            RegexOptions.Compiled);
        foreach (var (rel, text) in AppXamlFiles())
        {
            foreach (Match m in hardcoded.Matches(text))
                offenders.Add(rel + ": " + m.Value.Trim());
        }
        Assert.IsEmpty(offenders, "Hardcoded text colors (break high-contrast themes): " + string.Join(" | ", offenders));
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
