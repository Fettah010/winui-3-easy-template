using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class ServiceLocatorTests
{
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
}
