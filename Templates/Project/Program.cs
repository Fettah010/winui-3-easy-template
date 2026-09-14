using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.Core;
#if (updates == 'velopack')
using Velopack;
#endif
using DevTemWinUi3.Services;
using DevTemWinUi3.Services.Native;

namespace DevTemWinUi3;

public static class Program
{
    /// <summary>
    /// Starts ticking at process entry; used for startup timing diagnostics.
    /// </summary>
    internal static readonly Stopwatch StartupStopwatch = Stopwatch.StartNew();

    private static Mutex? _mutex;
    private static EventWaitHandle? _activateEvent;

    /// <summary>
    /// Deep-link URI this launch was started with (null for normal launches).
    /// Read by <c>App</c> after the main window is ready.
    /// </summary>
    internal static string? PendingProtocolUri { get; private set; }

    [STAThread]
    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            AppLog.Fatal(ex, "Unhandled app-domain exception");
            CrashReportingService.Current.CaptureException(ex, "app-domain");
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            AppLog.Fatal(e.Exception, "Unobserved task exception");
            CrashReportingService.Current.CaptureException(e.Exception, "unobserved-task");
            e.SetObserved();
        };

        try
        {
            Run(args);
        }
        catch (Exception ex)
        {
            AppLog.Fatal(ex, "Application crashed during startup");
            CrashReportingService.Current.CaptureException(ex, "startup");
#if (health)
            ShowStartupCrashDialog();
#endif
            throw;
        }
    }

    /// <summary>
    /// Best-effort fatal-error box with the log-folder path, so a startup
    /// crash is actionable instead of a silent exit. Never throws; the UI
    /// stack may be unusable here, hence the native box (see
    /// <c>CrashDialogNative</c>). Localization is best-effort: the service
    /// may not be initialized yet, so missing keys fall back to English.
    /// Only compiled with the diagnostics feature (needs CrashDialogNative).
    /// </summary>
#if (health)
    internal static void ShowStartupCrashDialog()
    {
        try
        {
            string logDir;
            try { logDir = LoggingService.CurrentLogDirectory; }
            catch { logDir = AppContext.BaseDirectory; }
            string title;
            string body;
            try
            {
                title = LocalizationService.Current.GetString("StartupCrashTitle");
                body = LocalizationService.Current.GetString("StartupCrashBody", logDir);
            }
            catch
            {
                title = "Unexpected error";
                body = BuildStartupCrashMessage(logDir);
            }
            CrashDialogNative.Show(title, body);
        }
        catch { }
    }
#endif

    /// <summary>
    /// English fallback text for the startup-crash dialog (also the shape
    /// the unit tests pin: app name + log directory, never throws).
    /// </summary>
    internal static string BuildStartupCrashMessage(string? logDirectory)
    {
        try
        {
            string dir = string.IsNullOrWhiteSpace(logDirectory) ? "?" : logDirectory;
            return $"The app closed unexpectedly.\n\nLogs: {dir}";
        }
        catch
        {
            return "The app closed unexpectedly.";
        }
    }

    static void Run(string[] args)
    {
        LoggingService.Initialize();
        CrashReportingService.Current.Initialize();

        PendingProtocolUri = ProtocolService.ExtractProtocolUri(args);

        // Single-instance enforcement
        if (!TryEnforceSingleInstance())
        {
            AppLog.Information("Another instance is already running — signaling it and exiting");
            // A deep link aimed at a running app must not die with this
            // process: stash it where the first instance looks on activation.
            if (!string.IsNullOrWhiteSpace(PendingProtocolUri))
                ProtocolService.WritePendingUri(PendingProtocolUri);
            SignalExistingInstance();
            return;
        }

#if (updates == 'velopack')
        VelopackApp.Build()
            .OnFirstRun(_ => AppLog.Information("First run after install"))
            .OnRestarted(_ => AppLog.Information("Restarted after update"))
            .Run();

#endif
        AppLog.Information("{AppName} starting", AppMetadata.AppName);

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
