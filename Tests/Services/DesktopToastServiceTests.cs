using System;
using System.IO;
using System.Reflection;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class DesktopToastServiceTests
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
        LocalizationService.Current.SetLanguage("en-US");
        LocalSettingsStore.SetTestPath(null);
        try { File.Delete(_storePath); } catch { }
    }
    [TestMethod]
    public void TryShow_WithoutInitialize_ReturnsFalseWithoutThrowing()
    {
        // Registration never happened in the test host: must fail gracefully.
        Assert.IsFalse(DesktopToastService.Current.TryShowMinimized());
        Assert.IsFalse(DesktopToastService.Current.TryShowNotInstalled());
    }

    [TestMethod]
    public void Shutdown_WithoutInitialize_DoesNotThrow()
    {
        DesktopToastService.Current.Shutdown();
    }

    [TestMethod]
    public void ActivationHandler_RaisesActivationRequested()
    {
        // The OS -> NotificationInvoked link is platform code; this pins our
        // handler logic: forward to ActivationRequested, never throw.
        var svc = DesktopToastService.Current;
        bool raised = false;
        void Handler(object? s, EventArgs e) => raised = true;
        svc.ActivationRequested += Handler;
        try
        {
            var method = typeof(DesktopToastService).GetMethod(
                "OnNotificationInvoked", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method);
            method.Invoke(svc, new object[] { svc, new object() });
            Assert.IsTrue(raised);
        }
        finally
        {
            svc.ActivationRequested -= Handler;
        }
    }

    [TestMethod]
    public void ToastStrings_AreTranslated()
    {
        var loc = LocalizationService.Current;
        try
        {
            loc.SetLanguage("es-ES");
            Assert.AreEqual("Sigue en ejecución en la bandeja", loc.GetString("TrayMinTitle"));

            loc.SetLanguage("fr-FR");
            Assert.AreEqual("Toujours actif dans la zone", loc.GetString("TrayMinTitle"));
        }
        finally
        {
            loc.SetLanguage("en-US");
        }
    }
}
