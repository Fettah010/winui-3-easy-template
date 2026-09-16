using System;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class LaunchAnimationsTests
{
    [TestMethod]
    public void Scale_PassesThrough_WhenAnimationsOn()
    {
        LaunchAnimations.s_reducedMotionOverride = false;
        LaunchAnimations.s_fastLaunchOverride = false;
        try
        {
            Assert.AreEqual(TimeSpan.FromMilliseconds(350),
                LaunchAnimations.Scale(TimeSpan.FromMilliseconds(350)));
        }
        finally
        {
            LaunchAnimations.s_reducedMotionOverride = null;
            LaunchAnimations.s_fastLaunchOverride = null;
        }
    }

    [TestMethod]
    public void Scale_Collapses_WhenFastLaunch()
    {
        // P0-1: the fast-launch escape hatch removes the animation tax.
        LaunchAnimations.s_reducedMotionOverride = false;
        LaunchAnimations.s_fastLaunchOverride = true;
        try
        {
            Assert.AreEqual(TimeSpan.FromMilliseconds(1),
                LaunchAnimations.Scale(TimeSpan.FromMilliseconds(600)));
        }
        finally
        {
            LaunchAnimations.s_reducedMotionOverride = null;
            LaunchAnimations.s_fastLaunchOverride = null;
        }
    }

    [TestMethod]
    public void Scale_Collapses_WhenReducedMotion()
    {
        LaunchAnimations.s_reducedMotionOverride = true;
        LaunchAnimations.s_fastLaunchOverride = false;
        try
        {
            Assert.AreEqual(TimeSpan.FromMilliseconds(1),
                LaunchAnimations.Scale(TimeSpan.FromMilliseconds(600)));
        }
        finally
        {
            LaunchAnimations.s_reducedMotionOverride = null;
            LaunchAnimations.s_fastLaunchOverride = null;
        }
    }

    [TestMethod]
    public void Scale_NonPositive_PassesThrough()
    {
        Assert.AreEqual(TimeSpan.Zero, LaunchAnimations.Scale(TimeSpan.Zero));
    }
}
