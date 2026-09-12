using System;
using System.Collections.Generic;
using DevTemWinUi3.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;

namespace DevTemWinUi3.Tests.Controls;

[TestClass]
public class WrapLayoutTests
{
    private static List<Size> Sizes(params (double w, double h)[] items)
    {
        var list = new List<Size>(items.Length);
        foreach (var (w, h) in items)
            list.Add(new Size(w, h));
        return list;
    }

    [TestMethod]
    public void Arrange_SingleRow_NoWrap()
    {
        var rects = WrapLayout.Arrange(Sizes((100, 30), (120, 30)), 500, 8, 8, out var extent);

        Assert.HasCount(2, rects);
        Assert.AreEqual(0, rects[0].X);
        Assert.AreEqual(0, rects[0].Y);
        Assert.AreEqual(108, rects[1].X);
        Assert.AreEqual(0, rects[1].Y);
        Assert.AreEqual(228, extent.Width);
        Assert.AreEqual(30, extent.Height);
    }

    [TestMethod]
    public void Arrange_Overflow_WrapsToNextRow()
    {
        var rects = WrapLayout.Arrange(Sizes((200, 30), (200, 30), (200, 30)), 410, 10, 8, out var extent);

        // First two fit (200 + 10 + 200 = 410); third wraps.
        Assert.AreEqual(0, rects[0].Y);
        Assert.AreEqual(0, rects[1].Y);
        Assert.AreEqual(0, rects[2].X);
        Assert.AreEqual(38, rects[2].Y);
        Assert.AreEqual(410, extent.Width);
        Assert.AreEqual(68, extent.Height);
    }

    [TestMethod]
    public void Arrange_InfiniteWidth_NeverWraps()
    {
        var rects = WrapLayout.Arrange(
            Sizes((300, 20), (300, 20)), double.PositiveInfinity, 8, 8, out var extent);

        Assert.AreEqual(0, rects[1].Y);
        Assert.AreEqual(608, extent.Width);
    }

    [TestMethod]
    public void Arrange_Empty_ReturnsZeroExtent()
    {
        var rects = WrapLayout.Arrange(new List<Size>(), 500, 8, 8, out var extent);

        Assert.IsEmpty(rects);
        Assert.AreEqual(0, extent.Width);
        Assert.AreEqual(0, extent.Height);
    }

    [TestMethod]
    public void Arrange_ShorterChild_IsVerticallyCentered()
    {
        var rects = WrapLayout.Arrange(Sizes((100, 40), (100, 20)), 500, 8, 8, out _);

        Assert.AreEqual(0, rects[0].Y);
        Assert.AreEqual(10, rects[1].Y);
    }

    [TestMethod]
    public void Arrange_InvalidArgs_Throw()
    {
        Assert.Throws<ArgumentNullException>(
            () => WrapLayout.Arrange(null!, 500, 8, 8, out _));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => WrapLayout.Arrange(Sizes((10, 10)), -1, 8, 8, out _));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => WrapLayout.Arrange(Sizes((10, 10)), 500, -1, 8, out _));
    }
}
