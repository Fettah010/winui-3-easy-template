using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class ToastPolicyTests
{
    [TestMethod]
    public void AdmitsUpToMaxVisible()
    {
        Assert.IsTrue(ToastPolicy.ShouldShowNow(0));
        Assert.IsTrue(ToastPolicy.ShouldShowNow(ToastPolicy.MaxVisible - 1));
        Assert.IsFalse(ToastPolicy.ShouldShowNow(ToastPolicy.MaxVisible));
        Assert.IsFalse(ToastPolicy.ShouldShowNow(ToastPolicy.MaxVisible + 5));
    }

    [TestMethod]
    public void Queue_IsBounded()
    {
        // P1-2: a burst of 10 queues instead of churning the host, and the
        // queue itself never grows without bound.
        Assert.IsTrue(ToastPolicy.ShouldQueue(0));
        Assert.IsTrue(ToastPolicy.ShouldQueue(ToastPolicy.MaxQueued - 1));
        Assert.IsFalse(ToastPolicy.ShouldQueue(ToastPolicy.MaxQueued));
    }
}
