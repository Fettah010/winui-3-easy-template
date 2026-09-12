using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.Core;
using Velopack;
using DevTemWinUi3.Services;

namespace DevTemWinUi3;

public static class Program
{
    private static Mutex? _mutex;
    private static EventWaitHandle? _activateEvent;

    [STAThread]
    static void Main(string[] args)
    {
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

        // Single-instance enforcement
        if (!TryEnforceSingleInstance())
        {
            LoggingService.Log.Information("Another instance is already running — signaling it and exiting");
            SignalExistingInstance();
            return;
        }

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

    /// <summary>
    /// Attempts to register this process as the single instance.
    /// Returns true if this is the first instance, false if another is already running.
    /// </summary>
    private static bool TryEnforceSingleInstance()
    {
        const string mutexName = "Global\\DevTemWinUi3_SingleInstance_Mutex";
        const string eventName = "Global\\DevTemWinUi3_Activate_Event";

        _mutex = new Mutex(true, mutexName, out bool createdNew);
        if (!createdNew)
            return false;

        _activateEvent = new EventWaitHandle(false, EventResetMode.AutoReset, eventName);
        GC.KeepAlive(_mutex);
        return true;
    }

    /// <summary>
    /// Signals the existing instance's event to bring its window to front.
    /// </summary>
    private static void SignalExistingInstance()
    {
        try
        {
            const string eventName = "Global\\DevTemWinUi3_Activate_Event";
            using var evt = EventWaitHandle.OpenExisting(eventName);
            evt.Set();
        }
        catch
        {
            // Event doesn't exist or other instance died — nothing to do
        }
    }

    /// <summary>
    /// Waits for the activation event from a second instance.
    /// Called on the UI thread after the main window is created.
    /// </summary>
    internal static void StartActivationListener(DispatcherQueue dispatcher, Action onActivate)
    {
        Task.Run(() =>
        {
            while (true)
            {
                try
                {
                    using var evt = EventWaitHandle.OpenExisting(
                        "Global\\DevTemWinUi3_Activate_Event");
                    evt.WaitOne();
                    dispatcher.TryEnqueue(() => onActivate());
                }
                catch
                {
                    // Event closed — app shutting down
                    break;
                }
            }
        });
    }
}
