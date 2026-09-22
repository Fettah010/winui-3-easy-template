using System;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

/// <summary>
/// Advancement C1: route-to-factory registry mechanics. Factory invocation
/// constructs a <c>Page</c> (UI thread only), so these tests pin the
/// registry — registration, resolution WITHOUT invoking, fallback for
/// unknown routes — while page construction itself stays covered by the
/// build + the scaffold matrix (house rule: UI-bound paths are not
/// headless-tested).
/// </summary>
[TestClass]
public class PageFactoryTests
{
    [TestInitialize]
    public void Init() => PageFactory.ResetForTests();

    [TestCleanup]
    public void Cleanup() => PageFactory.ResetForTests();

    [TestMethod]
    public void UnknownRoute_IsNotRegistered()
    {
        Assert.IsFalse(PageFactory.IsRegistered("nope"));
        Assert.IsFalse(PageFactory.TryGetFactory("nope", out var factory));
        Assert.IsNull(factory);
        Assert.IsFalse(PageFactory.TryCreate("nope", out var page));
        Assert.IsNull(page);
    }

    [TestMethod]
    public void BlankRoute_IsRejected()
    {
        PageFactory.Register(string.Empty, () => null!);
        PageFactory.Register("   ", () => null!);
        Assert.IsFalse(PageFactory.IsRegistered(string.Empty));
    }

    [TestMethod]
    public void RegisteredRoute_ResolvesItsFactory()
    {
        static Microsoft.UI.Xaml.Controls.Page Make() => throw new InvalidOperationException("must not run headless");
        PageFactory.Register("r1", Make);

        Assert.IsTrue(PageFactory.IsRegistered("r1"));
        Assert.IsTrue(PageFactory.TryGetFactory("r1", out var factory));
        Assert.AreEqual((object)Make, factory!);
    }

    [TestMethod]
    public void Reregistering_ReplacesTheFactory()
    {
        static Microsoft.UI.Xaml.Controls.Page First() => throw new InvalidOperationException("a");
        static Microsoft.UI.Xaml.Controls.Page Second() => throw new InvalidOperationException("b");
        PageFactory.Register("r", First);
        PageFactory.Register("r", Second);

        Assert.IsTrue(PageFactory.TryGetFactory("r", out var factory));
        Assert.AreEqual((object)Second, factory!);
    }

    [TestMethod]
    public void ThrowingFactory_FallsBackFalse()
    {
        static Microsoft.UI.Xaml.Controls.Page Boom() => throw new InvalidOperationException("boom");
        PageFactory.Register("bad", Boom);

        Assert.IsFalse(PageFactory.TryCreate("bad", out var page));
        Assert.IsNull(page);
    }

    [TestMethod]
    public void ServiceLocator_RegistersMigratedRoutes()
    {
        // The C1 migration set: every factory below constructs its page
        // with the view model injected (no ServiceLocator-at-click).
        // Relational across scaffold modes: flag-dropped pages register
        // no factory (matrix runs every combo).
        ServiceLocator.Initialize();

        Assert.IsTrue(PageFactory.IsRegistered("settings"));
        Assert.AreEqual(AppFeatures.SetupWizard, PageFactory.IsRegistered("setupwizard"));
        Assert.AreEqual(AppFeatures.Health, PageFactory.IsRegistered("diagnostics"));
        Assert.IsFalse(PageFactory.IsRegistered("home"));
    }
}
