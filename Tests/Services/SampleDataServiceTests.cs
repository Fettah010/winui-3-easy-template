using System;
using DevTemWinUi3.Services.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class SampleDataServiceTests
{
    [TestMethod]
    public void GetSampleCards_ReturnsTwelveDeterministicCards()
    {
        var cards = SampleDataService.GetSampleCards();
        Assert.HasCount(12, cards);
        Assert.AreEqual("Alpine notebook", cards[0].Title);
        Assert.AreEqual("Lagoon water bottle", cards[11].Title);
    }

    [TestMethod]
    public void Search_NullOrEmpty_ReturnsAll()
    {
        var cards = SampleDataService.GetSampleCards();
        Assert.HasCount(12, SampleDataService.Search(cards, null));
        Assert.HasCount(12, SampleDataService.Search(cards, "  "));
        Assert.HasCount(0, SampleDataService.Search(null, "mug"));
    }

    [TestMethod]
    public void Search_MatchesTitleOrDetail_CaseInsensitive()
    {
        var cards = SampleDataService.GetSampleCards();
        var byTitle = SampleDataService.Search(cards, "MUG");
        Assert.HasCount(1, byTitle);
        Assert.AreEqual("Basalt mug", byTitle[0].Title);
        var byDetail = SampleDataService.Search(cards, "canvas");
        Assert.HasCount(1, byDetail);
        Assert.AreEqual("Harbor backpack", byDetail[0].Title);
    }

    [TestMethod]
    public void Sort_OrdersBothDirections()
    {
        var cards = SampleDataService.GetSampleCards();
        Assert.AreEqual("Lagoon water bottle", SampleDataService.Sort(cards, "Title", ascending: false)[0].Title);
        Assert.AreEqual("Alpine notebook", SampleDataService.Sort(cards, "Title", ascending: true)[0].Title);
    }

    [TestMethod]
    public void Sort_UnknownProperty_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SampleDataService.Sort(SampleDataService.GetSampleCards(), "NoSuchColumn", ascending: true));
    }

    [TestMethod]
    public void Page_SlicesAndClamps()
    {
        var cards = SampleDataService.GetSampleCards();
        var first = SampleDataService.Page(cards, 0, 5);
        Assert.HasCount(5, first);
        Assert.AreEqual("Alpine notebook", first[0].Title);
        var last = SampleDataService.Page(cards, 2, 5);
        Assert.HasCount(2, last);
        Assert.AreEqual("Lagoon water bottle", last[1].Title);
        // Out-of-range clamps instead of throwing.
        Assert.AreEqual(last[0].Title, SampleDataService.Page(cards, 99, 5)[0].Title);
        Assert.AreEqual(first[0].Title, SampleDataService.Page(cards, -3, 5)[0].Title);
    }

    [TestMethod]
    public void PageCount_RoundsUp()
    {
        Assert.AreEqual(3, SampleDataService.PageCount(12, 5));
        Assert.AreEqual(1, SampleDataService.PageCount(0, 5));
        Assert.AreEqual(1, SampleDataService.PageCount(12, 0));
    }
}
