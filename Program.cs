using System;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Velopack;
using DevTemWinUi3.Services;

namespace DevTemWinUi3;

public static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        LoggingService.Initialize();

        VelopackApp.Build()
            .OnFirstRun(_ => LoggingService.Log.Information("First run after install"))
            .OnRestarted(_ => LoggingService.Log.Information("Restarted after update"))
            .Run();

        LoggingService.Log.Information("DevTem-WinUI 3 starting");

        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(
                DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });
    }
}