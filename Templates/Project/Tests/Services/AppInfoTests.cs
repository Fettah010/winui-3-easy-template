using System;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class AppInfoTests
{
    [TestMethod]
    public void IsPackaged_IsFalse_WhenUnpackaged()
    {
        // Test runs (like dev runs) are never inside an MSIX package.
        // Guards the autostart-toggle gating in Settings.
        Assert.IsFalse(AppInfo.IsPackaged);
    }

    [TestMethod]
    public void Version_IsParseable()
    {
        Assert.IsTrue(Version.TryParse(AppInfo.Current.Version, out _));
    }
}
