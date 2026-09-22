using DevTemWinUi3.Services;
using DevTemWinUi3.Services.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

/// <summary>
/// Pain-log #23/#25: overlay math needs a named unit-conversion seam plus a
/// test with real-world dimensions, and mutation flows need their own
/// generation signal. A4 (210x297mm = 8.27x11.69in) pins both systems:
/// 595.3pt vs 793.7px. A silent 75% shrink fails here, not on empty lines.
/// </summary>
[TestClass]
public class HardeningHelpersTests
{
    [TestMethod]
    public void PointsToPixels_MapsA4Width()
    {
        // A4 width: 8.27in x 72 = 595.4pt -> x96 = 793.9px.
        double px = UnitConversion.PointsToPixels(595.28);
        Assert.IsTrue(px > 790 && px < 798, "A4 width in px: " + px);
    }

    [TestMethod]
    public void PixelsToPoints_RoundTrips()
    {
        double pt = UnitConversion.PixelsToPoints(UnitConversion.PointsToPixels(100.0));
        Assert.AreEqual(100.0, pt, 1e-9);
    }

    [TestMethod]
    public void PointsPixels_RatioIs96Over72()
    {
        Assert.AreEqual(96.0 / 72.0, UnitConversion.PointsToPixels(1.0), 1e-12);
    }

    [TestMethod]
    public void ScaleToFit_FitsInside()
    {
        double s = UnitConversion.ScaleToFit(100, 200, 50, 50);
        Assert.AreEqual(0.25, s, 1e-12);
    }

    [TestMethod]
    public void ScaleToFit_DegenerateSourceIsOne()
    {
        Assert.AreEqual(1.0, UnitConversion.ScaleToFit(0, 10, 50, 50), 1e-12);
    }

    [TestMethod]
    public void ChangeEpoch_BumpsMonotonically()
    {
        var epoch = new ChangeEpoch();
        Assert.AreEqual(0, epoch.Current);
        Assert.AreEqual(1, epoch.Next());
        Assert.AreEqual(2, epoch.Next());
        Assert.AreEqual(2, epoch.Current);
    }

    [TestMethod]
    public void FirstRunContent_IsModeAware()
    {
        // The welcome body always carries one update-mode line, so no
        // scaffold advertises an update mechanism it does not have.
        string content = FirstRunDialogService.GetWelcomeContent();
        Assert.IsFalse(string.IsNullOrWhiteSpace(content));
        string mode = AppFeatures.UpdateMode;
        string expected = mode switch
        {
            "velopack" => LocalizationService.Current.GetString("FirstRunUpdatesVelopack"),
            "basic" => LocalizationService.Current.GetString("FirstRunUpdatesBasic"),
            "none" => LocalizationService.Current.GetString("FirstRunUpdatesNone"),
            _ => LocalizationService.Current.GetString("FirstRunUpdatesExternal"),
        };
        Assert.Contains(expected, content);
    }

    [TestMethod]
    public void ProductLinks_ResolveFromConfiguration()
    {
        Assert.StartsWith("https://github.com/", AppMetadata.RepoUrl);
        Assert.IsFalse(string.IsNullOrWhiteSpace(AppMetadata.LicenseUrl));
        Assert.IsFalse(string.IsNullOrWhiteSpace(AppMetadata.LicenseName));
    }
}
