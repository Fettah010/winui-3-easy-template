using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

/// <summary>
/// Guards the repo against the encoding-corruption class that once grew
/// <c>Templates/Project/__SafeName__.csproj</c> to 148MB: a script read a
/// UTF-8 file with the system codepage and wrote it back as UTF-8, doubling
/// the mojibake on every release (an em-dash seed, six doublings). Every
/// run asserts: no U+FFFD replacement chars in text sources, and both
/// WinExe csprojs stay small. ASCII-only in tooling comments; non-ASCII
/// belongs in loc dictionaries (UTF-8, covered by translation tests).
/// </summary>
[TestClass]
public class RepoHygieneTests
{
    private static readonly string[] TextExtensions =
        { ".cs", ".xaml", ".ps1", ".csproj", ".props", ".md", ".yml", ".sln", ".manifest" };

    [TestMethod]
    public void TextSources_ContainNoReplacementChars()
    {
        string? root = FindRepoRoot();
        Assert.IsNotNull(root, "Could not locate repo root (no .sln found walking up).");
        var offenders = new System.Collections.Generic.List<string>();
        foreach (string file in Directory.EnumerateFiles(root!, "*", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(root!, file);
            if (rel.Split(Path.DirectorySeparatorChar).Any(s =>
                s.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                s.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
                s.Equals(".git", StringComparison.OrdinalIgnoreCase) ||
                s.Equals(".vs", StringComparison.OrdinalIgnoreCase) ||
                s.Equals("TestResults", StringComparison.OrdinalIgnoreCase) ||
                s.Equals("docs", StringComparison.OrdinalIgnoreCase) ||
                s.Equals("nupkgs", StringComparison.OrdinalIgnoreCase)))
                continue;
            if (!TextExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                continue;
            string text;
            try { text = File.ReadAllText(file); } catch { continue; }
            if (text.Contains((char)0xFFFD))
                offenders.Add(rel);
        }
        Assert.IsEmpty(offenders, "U+FFFD replacement chars (mojibake damage) in: " + string.Join(" | ", offenders));
    }

    [TestMethod]
    public void WinExeCsprojs_StaySmall()
    {
        string? root = FindRepoRoot();
        Assert.IsNotNull(root, "Could not locate repo root (no .sln found walking up).");
        foreach (string rel in new[] { "DevTemWinUi3.csproj", Path.Combine("Templates", "Project", "__SafeName__.csproj") })
        {
            string path = Path.Combine(root!, rel);
            if (File.Exists(path))
                // NOTE: argument order is (upperBound, value) in this
                // MSTest version — do not "fix" the order; the failure
                // message prints them back swapped-looking.
                Assert.IsLessThan(100L * 1024L, new FileInfo(path).Length, rel + " exceeds 100KB - check for encoding-doubling damage.");
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
            dir = Path.GetDirectoryName(dir.TrimEnd(Path.DirectorySeparatorChar));
        }
        return null;
    }
}
