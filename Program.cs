using System;
using System.Threading;
using System.Threading.Tasks;
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
        // Last line of defence: log any exception that escaped every other
        // handler so crash details always end up in the log files.
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            LoggingService.Log.Fatal(e.ExceptionObject as Exception, "Unhandled app-domain exception");
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            LoggingService.Log.Fatal(e.Exception, "Unobserved task exception");
            e.SetObserved();
        };

        try
        {
            Run(args);
        }
        catch (Exception ex)
        {
            LoggingService.Log.Fatal(ex, "Application crashed during startup");
            throw;
        }
    }

    static void Run(string[] args)
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