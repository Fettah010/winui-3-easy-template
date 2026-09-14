using System.Diagnostics;
using DevTemWinUi3.Services.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class AppTraceTests
{
    [TestMethod]
    public void StartPhase_EmitsActivity_WhenListened()
    {
        string? received = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "DevTem.Startup",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllData,
            ActivityStopped = activity => received = activity.DisplayName,
        };
        ActivitySource.AddActivityListener(listener);

        using (AppTrace.StartPhase("test-span")) { }

        Assert.AreEqual("test-span", received);
    }

    [TestMethod]
    public void StartPhase_WithoutListener_ReturnsNullWithoutThrowing()
    {
        using var activity = AppTrace.StartPhase("unlistened-span");
        Assert.IsNull(activity);
    }
}
