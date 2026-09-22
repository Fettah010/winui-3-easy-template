using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

/// <summary>
/// Pain-log #19: XAML <c>Symbol="X"</c> strings compile green and die with
/// <c>XamlParseException</c> on first navigation when X is not a
/// <c>Symbol</c> member. This test audits every <c>Symbol="…"</c> in the
/// repo's XAML against the known-good member set (which mirrors the
/// template's --icon choice list), so a bad icon fails the build instead
/// of a user's first click. Prefer FontIcon glyphs for decorative icons
/// (a bad glyph shows tofu, never throws).
/// </summary>
[TestClass]
public class XamlSymbolAuditTests
{
    // Known-good Symbol members used by the template + the item template's
    // --icon choice list. Extend in both places when adding an icon.
    private static readonly HashSet<string> Allowed = new(StringComparer.Ordinal)
    {
        "Home", "Document", "Shop", "Mail", "Calendar", "People",
        "Globe", "Pictures", "Video", "Camera", "Map", "Phone",
        "Repair", "Setting", "Help", "Accept", "Cancel", "Refresh",
    };

    [TestMethod]
    public void AllXamlSymbolAttributes_AreKnownMembers()
    {
        string? root = FindRepoRoot();
        Assert.IsNotNull(root, "Could not locate repo root (no .sln found walking up).");
        var offenders = new List<string>();
        var rx = new Regex(@"Symbol\s*=\s*""([^""]+)""", RegexOptions.Compiled);
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
            foreach (Match m in rx.Matches(text))
            {
                string symbol = m.Groups[1].Value;
                if (!Allowed.Contains(symbol))
                    offenders.Add(rel + ": Symbol=\"" + symbol + "\"");
            }
        }
        Assert.IsEmpty(offenders, "Unknown WinUI Symbol members (XamlParseException at runtime): " + string.Join(" | ", offenders));
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
            dir = Path.GetDirectoryName(dir.TrimEnd(Path.DirectorySeparatorChar));
        }
        return null;
    }
}
