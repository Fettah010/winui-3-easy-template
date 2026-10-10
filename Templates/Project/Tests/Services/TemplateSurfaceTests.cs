using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

/// <summary>
/// v0.6.0: template-surface contract. Every scaffold option must be wired
/// end to end (template.json symbol + engine modifier + FEATURES row +
/// feature-manifest entry), or the matrix proves a lie. These tests pin the
/// v0.6.0 evaluation outcomes: --slnx adopted, --framework/--cpm declined
/// with reasons (see DECISIONS.md) — re-adopting either must update these
/// tests deliberately, never silently.
/// </summary>
[TestClass]
public class TemplateSurfaceTests
{
    private static readonly string[] SlnxManifestFiles = { "__SafeName__.slnx" };

    private static string TemplateRoot()
    {
        string? dir = AppContext.BaseDirectory;
        for (int i = 0; i < 12 && dir is not null; i++)
        {
            bool hasSln = false;
            try { hasSln = Directory.EnumerateFiles(dir, "*.sln").Any(); }
            catch { }
            if (hasSln)
            {
                string candidate = Path.Combine(dir, "Templates", "Project");
                // Scaffolded apps carry this test file but no template
                // sources: inconclusive there, asserted in the repo.
                if (Directory.Exists(candidate))
                    return candidate;
                Assert.Inconclusive("No template sources under the solution root (scaffold context).");
            }
            dir = Directory.GetParent(dir)?.FullName;
        }
        Assert.Fail("Could not locate repo root (no .sln found walking up).");
        throw new InvalidOperationException();
    }

    private static JsonDocument LoadTemplateJson()
    {
        string path = Path.Combine(TemplateRoot(), ".template.config", "template.json");
        Assert.IsTrue(File.Exists(path), "template.json missing: " + path);
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    [TestMethod]
    public void SlnxSymbol_IsOptInBool()
    {
        using JsonDocument doc = LoadTemplateJson();
        Assert.IsTrue(doc.RootElement.GetProperty("symbols").TryGetProperty("slnx", out JsonElement slnx),
            "template.json has no slnx symbol.");
        Assert.AreEqual("bool", slnx.GetProperty("datatype").GetString());
        Assert.AreEqual("false", slnx.GetProperty("defaultValue").GetString(),
            "slnx must default off (classic .sln stays the default).");
        Assert.AreEqual("__SLNX__", slnx.GetProperty("replaces").GetString());
    }

    [TestMethod]
    public void SlnxModifier_ExcludesFileWhenOff()
    {
        using JsonDocument doc = LoadTemplateJson();
        bool found = false;
        foreach (JsonElement modifier in doc.RootElement.GetProperty("sources")[0].GetProperty("modifiers").EnumerateArray())
        {
            if (modifier.GetProperty("condition").GetString() != "(!slnx)")
                continue;
            found = true;
            var excluded = modifier.GetProperty("exclude").EnumerateArray()
                .Select(e => e.GetString()).ToHashSet(StringComparer.Ordinal);
            Assert.Contains("__SafeName__.slnx", excluded,
                "(!slnx) must exclude __SafeName__.slnx.");
        }
        Assert.IsTrue(found, "No (!slnx) modifier in template.json sources.");
    }

    [TestMethod]
    public void CpmSymbol_StaysAbsentByDecision()
    {
        // --cpm was evaluated in v0.6.0 and declined with probe evidence
        // (the template engine offers no XML conditional path this repo can
        // use: MSBuild Condition-doubling would noise every default scaffold).
        // Re-adopting it must update this test + DECISIONS deliberately.
        using JsonDocument doc = LoadTemplateJson();
        Assert.IsFalse(doc.RootElement.GetProperty("symbols").TryGetProperty("cpm", out _),
            "--cpm reappeared without a decision update (see DECISIONS.md v0.6.0).");
        Assert.IsFalse(File.Exists(Path.Combine(TemplateRoot(), "Directory.Packages.props")),
            "Directory.Packages.props reappeared without a decision update.");
    }

    [TestMethod]
    public void SlnxFile_IsValidXmlSolutionReferencingSourceName()
    {
        string path = Path.Combine(TemplateRoot(), "__SafeName__.slnx");
        Assert.IsTrue(File.Exists(path), "__SafeName__.slnx missing from the template.");
        string text = File.ReadAllText(path);
        Assert.IsFalse(text.Contains("__SafeName__", StringComparison.Ordinal),
            "slnx content must not carry the safe-name token (filename does; content uses the DevTemWinUi3 sourceName so scaffolds substitute).");
        XDocument xml = XDocument.Parse(text);
        Assert.AreEqual("Solution", xml.Root?.Name.LocalName);
        var projects = xml.Root?.Elements("Project")
            .Select(p => p.Attribute("Path")?.Value ?? string.Empty).ToList() ?? new List<string>();
        Assert.IsGreaterThanOrEqualTo(2, projects.Count, "slnx must list the app + tests projects.");
        Assert.IsTrue(projects.Any(p => p.EndsWith("DevTemWinUi3.csproj", StringComparison.Ordinal)),
            "slnx must reference the DevTemWinUi3 sourceName project.");
    }

    [TestMethod]
    public void FeaturesRow_CoversSlnx()
    {
        string path = Path.Combine(TemplateRoot(), "docs", "FEATURES.md");
        Assert.IsTrue(File.Exists(path), "Template FEATURES.md missing.");
        Assert.Contains("| SLNX solution | __SLNX__ |", File.ReadAllText(path));
    }

    [TestMethod]
    public void FeatureManifest_CoversSlnx()
    {
        string path = Path.Combine(TemplateRoot(), "docs", "template-features.json");
        Assert.IsTrue(File.Exists(path), "Template template-features.json missing.");
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
        Assert.IsTrue(doc.RootElement.GetProperty("features").TryGetProperty("slnx", out JsonElement slnx),
            "Feature manifest has no slnx entry (matrix Assert-FeatureManifest requires it).");
        Assert.IsFalse(slnx.GetProperty("default").GetBoolean());
        var files = slnx.GetProperty("files").EnumerateArray()
            .Select(e => e.GetString()).ToList();
        CollectionAssert.AreEqual(SlnxManifestFiles, files);
    }

    [TestMethod]
    public void TemplateDefault_TargetFrameworkStaysNet10()
    {
        // The --framework selector was evaluated and declined (matrix-cost
        // rule): net10 stays the hardcoded default, retarget is a documented
        // 3-line edit (TEMPLATE-GUIDE). This pins the default.
        string path = Path.Combine(TemplateRoot(), "__SafeName__.csproj");
        string csproj = File.ReadAllText(path);
        Assert.Contains("net10.0-windows10.0.19041.0", csproj);
    }

    [TestMethod]
    public void EveryReplacesToken_IsReferencedInTemplateSources()
    {
        // Dead `replaces` tokens are silent lies in --help output: the flag
        // claims to stamp files it never touches. Each token must occur in
        // at least one template source file.
        using JsonDocument doc = LoadTemplateJson();
        var missing = new List<string>();
        foreach (JsonProperty symbol in doc.RootElement.GetProperty("symbols").EnumerateObject())
        {
            if (!symbol.Value.TryGetProperty("replaces", out JsonElement replaces))
                continue;
            string? token = replaces.GetString();
            if (string.IsNullOrEmpty(token))
                continue;
            bool found = false;
            foreach (string file in Directory.EnumerateFiles(TemplateRoot(), "*", SearchOption.AllDirectories))
            {
                string name = Path.GetFileName(file);
                if (name.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
                    continue;
                string text;
                try { text = File.ReadAllText(file); }
                catch { continue; }
                if (text.Contains(token, StringComparison.Ordinal))
                {
                    found = true;
                    break;
                }
            }
            if (!found)
                missing.Add(symbol.Name + " -> " + token);
        }
        Assert.IsEmpty(missing, "replaces tokens referenced nowhere in template sources: " + string.Join(", ", missing));
    }
}
