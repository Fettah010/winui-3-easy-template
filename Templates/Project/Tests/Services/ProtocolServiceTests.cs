using System.IO;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class ProtocolServiceTests
{
    private static readonly string[] s_launchWithUri = new[] { "App.exe", ProtocolService.Scheme + "://settings" };
    private static readonly string[] s_installerFlags = new[] { "--veloapp-install", "1.2.3" };
    private static readonly string[] s_quotedUri = new[] { '"' + ProtocolService.Scheme + "://home" + '"' };

    [TestMethod]
    public void Scheme_MatchesPrefix()
    {
        // Scheme-agnostic on purpose: scaffolded apps rename devtem:// to
        // their own scheme (both rename engines retarget the literal), and
        // this file ships verbatim in the template.
        string scheme = ProtocolService.Scheme;
        Assert.AreEqual(AppMetadata.ProtocolScheme, scheme);
        Assert.IsFalse(string.IsNullOrWhiteSpace(scheme));
        Assert.DoesNotContain(":", scheme);
        Assert.DoesNotContain("/", scheme);
    }

    private static string SchemeUri(string route) => ProtocolService.Scheme + "://" + route;

    [TestMethod]
    public void TryParseRoute_ParsesKnownFormats()
    {
        Assert.IsTrue(ProtocolService.TryParseRoute(SchemeUri("settings"), out var tag));
        Assert.AreEqual("settings", tag);

        Assert.IsTrue(ProtocolService.TryParseRoute(ProtocolService.Scheme + ":///settings", out tag));
        Assert.AreEqual("settings", tag);

        Assert.IsTrue(ProtocolService.TryParseRoute(SchemeUri("home") + "?theme=dark", out tag));
        Assert.AreEqual("home", tag);

        Assert.IsTrue(ProtocolService.TryParseRoute(
            ProtocolService.Scheme.ToUpperInvariant() + "://About", out tag));
        Assert.AreEqual("About", tag);
    }

    [TestMethod]
    public void TryParseRoute_RejectsGarbage()
    {
        Assert.IsFalse(ProtocolService.TryParseRoute(null, out _));
        Assert.IsFalse(ProtocolService.TryParseRoute("", out _));
        Assert.IsFalse(ProtocolService.TryParseRoute(ProtocolService.Scheme + "://", out _));
        Assert.IsFalse(ProtocolService.TryParseRoute(ProtocolService.Scheme + "://?x=1", out _));
        Assert.IsFalse(ProtocolService.TryParseRoute("https://example.com", out _));
        Assert.IsFalse(ProtocolService.TryParseRoute("settings", out _));
    }

    [TestMethod]
    public void ExtractProtocolUri_FindsSchemeArg()
    {
        string? uri = ProtocolService.ExtractProtocolUri(s_launchWithUri);
        Assert.AreEqual(SchemeUri("settings"), uri);
    }

    [TestMethod]
    public void ExtractProtocolUri_IgnoresInstallerFlags()
    {
        string? uri = ProtocolService.ExtractProtocolUri(s_installerFlags);
        Assert.IsNull(uri);

        Assert.IsNull(ProtocolService.ExtractProtocolUri(null));
        Assert.IsNull(ProtocolService.ExtractProtocolUri(System.Array.Empty<string>()));
    }

    [TestMethod]
    public void ExtractProtocolUri_TrimsQuotes()
    {
        string? uri = ProtocolService.ExtractProtocolUri(s_quotedUri);
        Assert.AreEqual(SchemeUri("home"), uri);
    }

    [TestMethod]
    public void PendingUri_RoundTripsThroughFile()
    {        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".txt");
        try
        {
            Assert.IsFalse(ProtocolService.TryReadAndClearPendingUri(out _, path));
            ProtocolService.WritePendingUri(SchemeUri("settings"), path);
            Assert.IsTrue(ProtocolService.TryReadAndClearPendingUri(out var uri, path));
            Assert.AreEqual(SchemeUri("settings"), uri);
            // Read clears: second read finds nothing.
            Assert.IsFalse(ProtocolService.TryReadAndClearPendingUri(out _, path));
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }

    [TestMethod]
    public void RegistryValues_QuoteSpacedPaths()
    {
        // Binary-planting rule: every registry command value must quote
        // the exe (a spaced path like "C:\My Apps\app.exe" unquoted lets
        // Windows resolve "C:\My.exe" instead). The writes themselves stay
        // untested by house rule; these builders are what get written.
        const string spaced = @"C:\My Apps\AcmeDesk.exe";
        Assert.AreEqual("\"" + spaced + "\" \"%1\"", ProtocolService.BuildCommandValue(spaced));
        Assert.AreEqual("\"" + spaced + "\",0", ProtocolService.BuildIconValue(spaced));
        Assert.AreEqual("\"" + spaced + "\"", AutoStartService.BuildRunValue(spaced));
    }
}
