using System;
using System.IO;
using DevTemWinUi3.Services;
using DevTemWinUi3.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class ServiceLocatorTests
{
    private string _storePath = string.Empty;

    [TestInitialize]
    public void Init()
    {
        _storePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        LocalSettingsStore.SetTestPath(_storePath);
    }

    [TestCleanup]
    public void Cleanup()
    {
        LocalSettingsStore.SetTestPath(null);
        try { File.Delete(_storePath); } catch { }
    }
    [TestMethod]
    public void Initialize_DoesNotThrow()
    {
        ServiceLocator.Initialize();
    }

    [TestMethod]
    public void GetService_ReturnsNonNull()
    {
        ServiceLocator.Initialize();
        var db = ServiceLocator.GetService<DatabaseService>();
        Assert.IsNotNull(db);
    }

    [TestMethod]
    public void GetRequiredService_ReturnsNonNull()
    {
        ServiceLocator.Initialize();
        var db = ServiceLocator.GetRequiredService<DatabaseService>();
        Assert.IsNotNull(db);
    }

    [TestMethod]
    public void Services_Property_IsAccessible()
    {
        ServiceLocator.Initialize();
        var services = ServiceLocator.Services;
        Assert.IsNotNull(services);
    }

    [TestMethod]
    public void Singletons_ResolveToProcessInstances()
    {
        ServiceLocator.Initialize();
        Assert.AreSame(UpdateService.Current, ServiceLocator.GetRequiredService<UpdateService>());
        Assert.AreSame(NavigationService.Current, ServiceLocator.GetRequiredService<NavigationService>());
        Assert.AreSame(ThemeService.Current, ServiceLocator.GetRequiredService<ThemeService>());
        Assert.AreSame(AppInfo.Current, ServiceLocator.GetRequiredService<AppInfo>());
    }

    [TestMethod]
    public void ViewModels_ResolveTransient()
    {
        // DI gate (pain-log #16): a dropped AddTransient line compiles
        // green and only explodes on user click. Every registered VM must
        // resolve here, so the missing-registration class of bug fails
        // this test instead of a navigation.
        ServiceLocator.Initialize();
        var first = ServiceLocator.GetRequiredService<SettingsPageViewModel>();
        var second = ServiceLocator.GetRequiredService<SettingsPageViewModel>();
        Assert.IsNotNull(first);
        Assert.IsNotNull(second);
        Assert.AreNotSame(first, second);

        var diag = ServiceLocator.GetRequiredService<DiagnosticsPageViewModel>();
        Assert.IsNotNull(diag);

        var updates = ServiceLocator.GetRequiredService<UpdateCenterViewModel>();
        Assert.IsNotNull(updates);

        var wizard = ServiceLocator.GetRequiredService<SetupWizardViewModel>();
        Assert.IsNotNull(wizard);
    }

    [TestMethod]
    public void Shutdown_ResetsContainer_AndReinitializeWorks()
    {
        // P2-1 ownership rule: Shutdown tears down the provider (container-
        // owned services go with it); process instances behind Current
        // survive and re-register on the next Initialize.
        ServiceLocator.Initialize();
        Assert.IsNotNull(ServiceLocator.GetService<NavigationService>());
        ServiceLocator.Shutdown();
        try
        {
            _ = ServiceLocator.Services;
            Assert.Fail("Services should throw after Shutdown.");
        }
        catch (InvalidOperationException) { }
        ServiceLocator.Initialize();
        Assert.AreSame(NavigationService.Current,
            ServiceLocator.GetRequiredService<NavigationService>());
    }

    [TestMethod]
    public void Modules_ResolveOneInstancePerService()
    {
        // P2-1: every module registration resolves (adding a service
        // touches exactly one Add* module plus this list).
        ServiceLocator.Initialize();
        Assert.AreSame(DatabaseService.Current,
            ServiceLocator.GetRequiredService<DatabaseService>());
        Assert.AreSame(UpdateService.Current,
            ServiceLocator.GetRequiredService<IUpdateService>());
        Assert.AreSame(SystemTrayService.Current,
            ServiceLocator.GetRequiredService<SystemTrayService>());
        Assert.IsNotNull(ServiceLocator.GetService<IFilePickerService>());
    }
}
