using System;
using System.IO;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

/// <summary>
/// Advancement B7: every ContentDialog button row follows one rule —
/// Primary is a verb, the dismiss button is a Close/Cancel/Later word per
/// state, never two verbs that both dismiss. This test pins the FirstRun /
/// whats-new / update trio (titles + button labels per language) so a new
/// dialog or a translation edit cannot silently break the contract.
/// </summary>
[TestClass]
public class DialogButtonLanguageTests
{
    private static readonly string[] Langs = { "en-US", "es-ES", "fr-FR" };

    // Primary buttons: verbs (they DO something).
    private static readonly string[] Verbs =
    {
        "FirstRunButton", "UpdateInstallNow", "UpdateRestartNow", "UpdateRetry",
    };

    // Dismiss buttons: Close/Cancel/Later words per state (they close).
    private static readonly string[] Dismiss =
    {
        "UpdateClose", "UpdateLater", "UpdateCancel", "UpdateRestartLater",
    };

    private string _storePath = string.Empty;

    [TestInitialize]
    public void Init()
    {
        _storePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        LocalSettingsStore.SetTestPath(_storePath);
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Never leak a test language into other tests.
        LocalizationService.Current.SetLanguage("en-US");
        LocalSettingsStore.SetTestPath(null);
        try { File.Delete(_storePath); } catch { }
    }

    [TestMethod]
    public void DialogButtons_ResolveInEveryLanguage()
    {
        var loc = LocalizationService.Current;
        string[] keys =
        {
            "FirstRunTitle", "FirstRunButton", "WhatsNewTitle",
            "UpdatePopupTitle", "UpdateInstallNow", "UpdateLater",
            "UpdateRestartNow", "UpdateRestartLater", "UpdateRetry",
            "UpdateClose", "UpdateCancel",
        };
        foreach (string lang in Langs)
        {
            loc.SetLanguage(lang);
            foreach (string key in keys)
            {
                string value = loc.GetString(key);
                Assert.IsFalse(string.IsNullOrWhiteSpace(value), lang + ": " + key + " is blank.");
                Assert.AreNotEqual(key, value, lang + ": " + key + " is untranslated.");
            }
        }
    }

    [TestMethod]
    public void WhatsNewTitle_FormatsVersion()
    {
        var loc = LocalizationService.Current;
        foreach (string lang in Langs)
        {
            loc.SetLanguage(lang);
            string title = loc.GetString("WhatsNewTitle", "1.2.3-test");
            Assert.Contains("1.2.3-test", title);
        }
    }

    [TestMethod]
    public void Verbs_NeverEqualDismissWords()
    {
        // A verb that reads like a dismiss word (or vice versa) breaks the
        // one-glance contract in that language.
        var loc = LocalizationService.Current;
        foreach (string lang in Langs)
        {
            loc.SetLanguage(lang);
            foreach (string verb in Verbs)
            {
                foreach (string dismiss in Dismiss)
                {
                    Assert.AreNotEqual(
                        loc.GetString(dismiss), loc.GetString(verb),
                        lang + ": verb " + verb + " reads as dismiss " + dismiss + ".");
                }
            }
        }
    }

    [TestMethod]
    public void DialogButtonPairs_FollowPrimaryVerbRule()
    {
        // (primaryKey, secondaryKey?) per dialog. Two-button dialogs pair
        // one verb with one dismiss word; single-button dialogs use a verb
        // (welcome) or the Close word (whats-new info).
        (string Primary, string? Secondary)[] pairs =
        {
            ("FirstRunButton", null),          // welcome
            ("UpdateClose", null),             // whats-new (info)
            ("UpdateInstallNow", "UpdateLater"),       // available
            ("UpdateRestartNow", "UpdateRestartLater"), // ready
            ("UpdateRetry", "UpdateClose"),             // error
        };
        var loc = LocalizationService.Current;
        foreach (string lang in Langs)
        {
            loc.SetLanguage(lang);
            foreach (var (primary, secondary) in pairs)
            {
                bool primaryIsVerb = Array.IndexOf(Verbs, primary) >= 0;
                bool primaryIsDismiss = Array.IndexOf(Dismiss, primary) >= 0;
                if (!primaryIsVerb && !primaryIsDismiss)
                    Assert.Fail(lang + ": unknown primary " + primary + ".");
                if (secondary is null)
                    continue;
                Assert.IsTrue(primaryIsVerb, lang + ": two-button dialog with non-verb primary " + primary + ".");
                Assert.IsGreaterThanOrEqualTo(
                    0,
                    Array.IndexOf(Dismiss, secondary),
                    lang + ": two-button dialog with non-dismiss secondary " + secondary + ".");
            }
        }
    }
}
