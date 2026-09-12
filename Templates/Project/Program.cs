using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.Core;
#if (updates)
using Velopack;
#endif
using DevTemWinUi3.Services;

namespace DevTemWinUi3;

public static class Program
{
    /// <summary>
    /// Starts ticking at process entry; used for startup timing diagnostics.
    /// </summary>
    internal static readonly Stopwatch StartupStopwatch = Stopwatch.StartNew();

    private static Mutex? _mutex;
    private static EventWaitHandle? _activateEvent;

    [STAThread]
    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            LoggingService.Log.Fatal(ex, "Unhandled app-domain exception");
            CrashReportingService.Current.CaptureException(ex, "app-domain");
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            LoggingService.Log.Fatal(e.Exception, "Unobserved task exception");
            CrashReportingService.Current.CaptureException(e.Exception, "unobserved-task");
            e.SetObserved();
        };

        try
        {
            Run(args);
        }
        catch (Exception ex)
        {
            LoggingService.Log.Fatal(ex, "Application crashed during startup");
            CrashReportingService.Current.CaptureException(ex, "startup");
            throw;
        }
    }

    static void Run(string[] args)
    {
        LoggingService.Initialize();
        CrashReportingService.Current.Initialize();

        // Single-instance enforcement
        if (!TryEnforceSingleInstance())
        {
            LoggingService.Log.Information("Another instance is already running — signaling it and exiting");
            SignalExistingInstance();
            return;
        }

#if (updates)
        VelopackApp.Build()
            .OnFirstRun(_ => LoggingService.Log.Information("First run after install"))
            .OnRestarted(_ => LoggingService.Log.Information("Restarted after update"))
            .Run();

#endif
        LoggingService.Log.Information("{AppName} starting", AppMetadata.AppName);

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
        var mutexName = AppMetadata.SingleInstanceMutexName;
        var eventName = AppMetadata.SingleInstanceEventName;

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
            using var evt = EventWaitHandle.OpenExisting(AppMetadata.SingleInstanceEventName);
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
    /// Uses one dedicated background thread blocked in WaitOne (zero CPU)
    /// instead of occupying a threadpool thread, and retries with backoff
    /// instead of dying silently if the event is momentarily unavailable.
    /// </summary>
    internal static void StartActivationListener(DispatcherQueue dispatcher, Action onActivate)
    {
        var thread = new Thread(() =>
        {
            while (true)
            {
                try
                {
                    using var evt = EventWaitHandle.OpenExisting(
                        AppMetadata.SingleInstanceEventName);
                    evt.WaitOne();
                    dispatcher.TryEnqueue(() => { try { onActivate(); } catch { } });
                }
                catch
                {
                    // Event missing (or app shutting down): back off and retry
                    // rather than spinning or abandoning the listener.
                    Thread.Sleep(5000);
                }
            }
        })
        {
            IsBackground = true,
            Name = "SingleInstanceActivation",
        };
        thread.Start();
    }
}
