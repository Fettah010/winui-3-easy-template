using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

/// <summary>
/// Advancement E3: generated-docs drift guards. The 0.0.26-era stale
/// version/branch/pages proved that prose rots while code moves, so the
/// contracts are structural — version truth lives in the csproj, branch
/// truth in generic channel names, page truth in Pages/*.xaml.
/// </summary>
[TestClass]
public class DocsDriftTests
{
    // Timeless command examples (never "current": they read as shapes).
    private static readonly HashSet<string> AllowedVersions =
        new(StringComparer.Ordinal) { "0.0.1", "1.0.0" };

    [TestMethod]
    public void Agents_PointsAtCsprojForVersions()
    {
        string agents = ReadRepoFile("AGENTS.md");
        Assert.Contains("see `<Version>`", agents);
    }

    [TestMethod]
    public void Agents_HasNoHardcodedVersions()
    {
        // The app versions 0.0.x (csproj truth); SDK/dependency versions
        // (10.0.x, Velopack 1.2.x) are legitimate prose, not app claims.
        string agents = ReadRepoFile("AGENTS.md");
        var offenders = new List<string>();
        foreach (Match m in Regex.Matches(agents, @"(?<!\d)(0\.0\.\d+)(?!\d)"))
        {
            string version = m.Groups[1].Value;
            if (!AllowedVersions.Contains(version))
                offenders.Add(version);
        }
        Assert.IsEmpty(offenders, "Hardcoded app versions in AGENTS.md (use the csproj pointer + timeless examples): " + string.Join(", ", offenders.Distinct()));
    }

    [TestMethod]
    public void Agents_BranchTableIsGeneric()
    {
        // The branch table must name channels (main/beta/stable, v*-beta,
        // v*), never a concrete release tag — tags age out on every release.
        string agents = ReadRepoFile("AGENTS.md");
        var rows = new List<string>();
        bool inTable = false;
        foreach (string line in agents.Split('\n'))
        {
            if (!inTable)
            {
                if (line.Contains("| `main` |"))
                    inTable = true;
                else
                    continue;
            }
            if (!line.TrimStart().StartsWith('|'))
                break;
            rows.Add(line);
        }
        Assert.IsGreaterThanOrEqualTo(1, rows.Count, "Branch table anchor missing.");
        string table = string.Join("\n", rows);
        Assert.IsFalse(
            Regex.IsMatch(table, @"v\d+\.\d+"),
            "Branch table names a concrete tag (keep v*-beta / v* shapes).");
    }

    [TestMethod]
    public void Readme_MentionsOnlyExistingPages()
    {
        // Every "X page" / Pages-fence entry must resolve to a Pages/*.xaml
        // in the same tree. Flag-dropped pages (Diagnostics without health,
        // Setup wizard without setup) are skipped relationally — the matrix
        // runs every combo.
        string? root = FindRepoRoot();
        Assert.IsNotNull(root, "Could not locate repo root (no .sln found walking up).");
        var offenders = new List<string>();
        foreach (string readme in Directory.EnumerateFiles(root!, "README.md", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(root!, readme);
            if (rel.Split(Path.DirectorySeparatorChar).Any(s =>
                s.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                s.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
                s.Equals("TestResults", StringComparison.OrdinalIgnoreCase)))
                continue;
            string? pagesDir = Path.Combine(Path.GetDirectoryName(readme)!, "Pages");
            if (!Directory.Exists(pagesDir))
                continue;
            var existing = new HashSet<string>(
                Directory.EnumerateFiles(pagesDir, "*Page.xaml")
                    .Select(p => Path.GetFileNameWithoutExtension(p).ToLowerInvariant()),
                StringComparer.Ordinal);
            string text;
            try { text = File.ReadAllText(readme); } catch { continue; }
            foreach (string name in MentionedPages(text))
            {
                string key = (name + "page").ToLowerInvariant();
                if (!existing.Contains(key) && !IsFlagDropped(name))
                    offenders.Add(rel + ": '" + name + " page' has no " + name + "Page.xaml");
            }
        }
        Assert.IsEmpty(offenders, "README names pages that do not exist: " + string.Join(" | ", offenders));
    }

    private static HashSet<string> MentionedPages(string text)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // "Home page" prose mentions (Title-Case only: lowercase "a page"
        // / "wizard page" are prose, not references; fences cover those).
        foreach (Match m in Regex.Matches(text, @"\b([A-Z][A-Za-z]*) page\b"))
        {
            string name = m.Groups[1].Value;
            if (!string.Equals(name, "this", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(name, "each", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(name, "one", StringComparison.OrdinalIgnoreCase))
                names.Add(name);
        }
        // "Pages/  # Home, About, ..." layout fences.
        foreach (Match m in Regex.Matches(text, @"Pages/\s*#(.*)"))
        {
            foreach (string part in m.Groups[1].Value.Split(','))
            {
                string token = part.Trim().Replace(" ", string.Empty);
                if (!string.IsNullOrEmpty(token))
                    names.Add(token);
            }
        }
        return names;
    }

    private static bool IsFlagDropped(string name)
    {
        if (string.Equals(name, "Diagnostics", StringComparison.OrdinalIgnoreCase))
            return !AppFeatures.Health;
        if (string.Equals(name, "Setupwizard", StringComparison.OrdinalIgnoreCase))
            return !AppFeatures.SetupWizard;
        return false;
    }

    private static string ReadRepoFile(string relative)
    {
        string? root = FindRepoRoot();
        Assert.IsNotNull(root, "Could not locate repo root (no .sln found walking up).");
        string path = Path.Combine(root!, relative);
        Assert.IsTrue(File.Exists(path), "Expected repo file missing: " + relative);
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
