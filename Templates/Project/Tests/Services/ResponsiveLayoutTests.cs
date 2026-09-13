using System;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class ResponsiveLayoutTests
{
    [TestMethod]
    public void LayoutConstants_AreSane()
    {
        // Locals (not consts) so the assertion conditions stay meaningful.
        int minW = ResponsiveLayout.MinWindowWidth;
        int minH = ResponsiveLayout.MinWindowHeight;
        double threshold = ResponsiveLayout.CompactLayoutThreshold;

        Assert.IsGreaterThanOrEqualTo(640, minW);
        Assert.IsGreaterThanOrEqualTo(480, minH);
        Assert.IsLessThan(1200, minW);
        Assert.IsGreaterThan(minW, threshold);
    }

    [TestMethod]
    public void ShouldUseCompactPane_NarrowWidth_ReturnsTrue()
    {
        Assert.IsTrue(ResponsiveLayout.ShouldUseCompactPane(720));
        Assert.IsTrue(ResponsiveLayout.ShouldUseCompactPane(859));
    }

    [TestMethod]
    public void ShouldUseCompactPane_WideWidth_ReturnsFalse()
    {
        Assert.IsFalse(ResponsiveLayout.ShouldUseCompactPane(860));
        Assert.IsFalse(ResponsiveLayout.ShouldUseCompactPane(1200));
    }

    [TestMethod]
    public void ShouldUseCompactPane_InvalidWidth_ReturnsFalse()
    {
        Assert.IsFalse(ResponsiveLayout.ShouldUseCompactPane(0));
        Assert.IsFalse(ResponsiveLayout.ShouldUseCompactPane(-10));
        Assert.IsFalse(ResponsiveLayout.ShouldUseCompactPane(double.NaN));
    }

    [TestMethod]
    public void ShouldUseNarrowPage_BelowThreshold_ReturnsTrue()
    {
        Assert.IsTrue(ResponsiveLayout.ShouldUseNarrowPage(699));
        Assert.IsTrue(ResponsiveLayout.ShouldUseNarrowPage(400));
    }

    [TestMethod]
    public void ShouldUseNarrowPage_AtOrAboveThreshold_ReturnsFalse()
    {
        Assert.IsFalse(ResponsiveLayout.ShouldUseNarrowPage(700));
        Assert.IsFalse(ResponsiveLayout.ShouldUseNarrowPage(1200));
    }

    [TestMethod]
    public void ShouldUseNarrowPage_InvalidWidth_ReturnsFalse()
    {
        Assert.IsFalse(ResponsiveLayout.ShouldUseNarrowPage(0));
        Assert.IsFalse(ResponsiveLayout.ShouldUseNarrowPage(-5));
        Assert.IsFalse(ResponsiveLayout.ShouldUseNarrowPage(double.NaN));
    }
    [TestMethod]
    public void ScaleLogicalToPhysical_96Dpi_IsIdentity()
    {
        Assert.AreEqual(720, ResponsiveLayout.ScaleLogicalToPhysical(720, 96));
        Assert.AreEqual(540, ResponsiveLayout.ScaleLogicalToPhysical(540, 96));
    }

    [TestMethod]
    public void ScaleLogicalToPhysical_200Percent_ScalesDoubles()
    {
        Assert.AreEqual(1440, ResponsiveLayout.ScaleLogicalToPhysical(720, 192));
        Assert.AreEqual(1080, ResponsiveLayout.ScaleLogicalToPhysical(540, 192));
    }

    [TestMethod]
    public void ScaleLogicalToPhysical_125Percent_RoundsCorrectly()
    {
        // 720 * 120 / 96 = 900 exactly
        Assert.AreEqual(900, ResponsiveLayout.ScaleLogicalToPhysical(720, 120));
    }

    [TestMethod]
    public void ScaleLogicalToPhysical_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ResponsiveLayout.ScaleLogicalToPhysical(-1, 96));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ResponsiveLayout.ScaleLogicalToPhysical(720, 0));
    }
}
