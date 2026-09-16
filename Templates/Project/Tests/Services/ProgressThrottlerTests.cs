using System;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class ProgressThrottlerTests
{
    [TestMethod]
    public void FirstReport_AlwaysForwards()
    {
        var throttler = new ProgressThrottler();
        Assert.IsTrue(throttler.ShouldReport(0, DateTimeOffset.UtcNow));
    }

    [TestMethod]
    public void HundredCallbacksPerSecond_CoalesceToAFew()
    {
        // P1-2: a 100 callbacks/s burst forwards a handful, not hundreds.
        var throttler = new ProgressThrottler(minDeltaPercent: 25, minInterval: TimeSpan.FromMilliseconds(100));
        var start = DateTimeOffset.UtcNow;
        int forwarded = 0;
        for (int i = 0; i <= 100; i++)
        {
            var now = start.AddMilliseconds(i * 10);
            if (throttler.ShouldReport(i, now))
                forwarded++;
        }
        Assert.IsGreaterThanOrEqualTo(3, forwarded, $"Too few forwards: {forwarded}");
        Assert.IsLessThanOrEqualTo(15, forwarded, $"Too many forwards: {forwarded}");
    }

    [TestMethod]
    public void Completion_AlwaysForwards()
    {
        var throttler = new ProgressThrottler(minDeltaPercent: 50, minInterval: TimeSpan.FromHours(1));
        var now = DateTimeOffset.UtcNow;
        Assert.IsTrue(throttler.ShouldReport(0, now));
        Assert.IsFalse(throttler.ShouldReport(10, now));
        Assert.IsTrue(throttler.ShouldReport(100, now));
        Assert.IsFalse(throttler.ShouldReport(100, now.AddMilliseconds(1)));
    }

    [TestMethod]
    public void Reset_RestartsBurst()
    {
        var throttler = new ProgressThrottler(minDeltaPercent: 50, minInterval: TimeSpan.FromHours(1));
        var now = DateTimeOffset.UtcNow;
        Assert.IsTrue(throttler.ShouldReport(10, now));
        throttler.Reset();
        Assert.IsTrue(throttler.ShouldReport(10, now));
    }

    [TestMethod]
    public void OutOfRange_Clamps()
    {
        var throttler = new ProgressThrottler();
        var now = DateTimeOffset.UtcNow;
        Assert.IsTrue(throttler.ShouldReport(-5, now));
        Assert.IsTrue(throttler.ShouldReport(100, now.AddMilliseconds(200)));
    }
}
