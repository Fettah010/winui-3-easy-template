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
#if (http)
        var api = ServiceLocator.GetService<ApiService>();
        Assert.IsNotNull(api);
#endif
#if (database)
        var db = ServiceLocator.GetService<DatabaseService>();
        Assert.IsNotNull(db);
#else
        var nav = ServiceLocator.GetService<NavigationService>();
        Assert.IsNotNull(nav);
#endif
    }

    [TestMethod]
    public void GetRequiredService_ReturnsNonNull()
    {
        ServiceLocator.Initialize();
#if (http)
        var api = ServiceLocator.GetRequiredService<ApiService>();
        Assert.IsNotNull(api);
#endif
#if (database)
        var db = ServiceLocator.GetRequiredService<DatabaseService>();
        Assert.IsNotNull(db);
#else
        var nav = ServiceLocator.GetRequiredService<NavigationService>();
        Assert.IsNotNull(nav);
#endif
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
#if (updates)
        Assert.AreSame(UpdateService.Current, ServiceLocator.GetRequiredService<UpdateService>());
#endif
        Assert.AreSame(NavigationService.Current, ServiceLocator.GetRequiredService<NavigationService>());
        Assert.AreSame(ThemeService.Current, ServiceLocator.GetRequiredService<ThemeService>());
        Assert.AreSame(AppInfo.Current, ServiceLocator.GetRequiredService<AppInfo>());
    }

    [TestMethod]
    public void ViewModels_ResolveTransient()
    {
        ServiceLocator.Initialize();
        var first = ServiceLocator.GetRequiredService<SettingsPageViewModel>();
        var second = ServiceLocator.GetRequiredService<SettingsPageViewModel>();
        Assert.IsNotNull(first);
        Assert.IsNotNull(second);
        Assert.AreNotSame(first, second);

#if (health)
        var diag = ServiceLocator.GetRequiredService<DiagnosticsPageViewModel>();
        Assert.IsNotNull(diag);
#endif
    }
}
