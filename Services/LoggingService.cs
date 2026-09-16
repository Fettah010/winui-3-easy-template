using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using DevTemWinUi3.Services.Diagnostics;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Extensions.Logging;

namespace DevTemWinUi3.Services;

/// <summary>
/// Central logging setup for the app. This copy is the serilog backend:
/// debugger console + daily-rolling files (14 days, 256 MB directory cap)
/// + the shared in-memory <see cref="InMemoryLogSink"/> for the live tail
/// and export.
/// App code logs through the backend-agnostic <see cref="AppLog"/> facade
/// (Microsoft.Extensions.Logging); the public surface here stays
/// backend-neutral (<see cref="LogLevel"/>, not Serilog types) so the
/// diagnostics page and tests compile for every backend.
/// </summary>
public static class LoggingService
{
    public const string LogDirectory = "Logs";
    public const string LogFileName = "applog-.log";

    /// <summary>Scaffold-time backend name (serilog here).</summary>
    public const string BackendName = "serilog";

    /// <summary>Whether this backend writes rolling log files.</summary>
    public static bool HasFileSink => true;

    /// <summary>Whether the enterprise Event Log collection is opted in.</summary>
    public static bool IsEventLogEnabled => EventLogSink.IsEnabledByConfig();

    private const int LogRetentionDays = 14;

    /// <summary>
    /// Byte cap for the log directory (P1-1): count retention alone lets a
    /// verbose 10x-service app grow %LocalAppData% without bound. Oldest
    /// files go first; enforced at init. Never throws.
    /// </summary>
    public const long LogDirectorySizeCapBytes = 256L * 1024 * 1024;

    /// <summary>Opt-in machine-readable sidecar via <c>DEVTEM_JSON_LOGS=1</c>.</summary>
    private const string JsonLogFileName = "applog-json-.log";
    private const int JsonLogRetentionDays = 7;

    private const string OutputTemplate =
        "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}";

    private static readonly LoggingLevelSwitch _levelSwitch = new(DefaultSerilogLevel);

    /// <summary>Compile-time default: Debug in Debug builds, Information in Release (P1-1).</summary>
    internal static LogEventLevel DefaultSerilogLevel =>
        IsDebugBuild ? LogEventLevel.Debug : LogEventLevel.Information;

    /// <summary>Live event buffer (newest ~1 000 events). Never null.</summary>
    public static InMemoryLogSink EventBuffer { get; } = new InMemoryLogSink();

    /// Whether this is a Debug build (P1-1 level defaults). Detected at
    /// runtime via <c>DebuggableAttribute</c>: a compile-time DEBUG
    /// conditional would be evaluated at scaffold time (freezing the wrong
    /// level into scaffolds), so the app and its template share this
    /// runtime check. Never throws.
    internal static bool IsDebugBuild
    {
        get
        {
            try
            {
                var attribute = typeof(LoggingService).Assembly
                    .GetCustomAttribute<System.Diagnostics.DebuggableAttribute>();
                return attribute is not null && attribute.IsJITOptimizerDisabled;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>Compile-time default mirroring <see cref="DefaultSerilogLevel"/>.</summary>
    internal static LogLevel DefaultMinimumLevel =>
        IsDebugBuild ? LogLevel.Debug : LogLevel.Information;

    /// <summary>
    /// Effective minimum level (mirrors the Serilog configuration; the
    /// diagnostics page displays it). Defaults to Debug in Debug builds
    /// and Information in Release before <see cref="Initialize"/> runs.
    /// </summary>
    public static LogLevel MinimumLevel { get; private set; } = DefaultMinimumLevel;

    /// <summary>
    /// Resolved log directory (set by <see cref="Initialize"/>; defaults to
    /// the primary location so crash paths can report it even when init
    /// never ran). Never throws.
    /// </summary>
    public static string CurrentLogDirectory { get; private set; } =
        Path.Combine(AppPaths.DataFolder, LogDirectory);

    public static void Initialize()
    {
        // Serilog's own misconfiguration is otherwise silent: route it to
        // the debugger output so a broken pipeline is diagnosable.
        try { Serilog.Debugging.SelfLog.Enable(msg => Debug.WriteLine(msg)); } catch { }

        var logPath = Path.Combine(AppPaths.DataFolder, LogDirectory);
        try
        {
            Directory.CreateDirectory(logPath);
        }
        catch
        {
            logPath = Path.Combine(Path.GetTempPath(), AppMetadata.AppDataFolder, LogDirectory);
            Directory.CreateDirectory(logPath);
        }

        CurrentLogDirectory = logPath;

        bool verbose = false;
        try { verbose = SettingsService.Current.VerboseLogging; } catch { }
        ApplyLevel(verbose);

        string appVersion = "?";
        try { appVersion = AppInfo.Current.Version; } catch { }

        var config = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(_levelSwitch)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.With(new ThreadIdEnricher())
            .Enrich.WithProperty("AppVersion", appVersion)
            // Invariant format provider: log files stay parseable regardless
            // of the machine's locale (CA1305).
            .WriteTo.Console(outputTemplate: OutputTemplate, formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.File(
                path: Path.Combine(logPath, LogFileName),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: LogRetentionDays,
                shared: true,
                outputTemplate: OutputTemplate,
                formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.Sink(new SerilogLogEntrySink(EventBuffer));

        // Enterprise collection: Error/Fatal also go to the Windows
        // Application log when the deployer opts in (DEVTEM_EVENT_LOG=1).
        // The sink self-gates on write permission; never throws.
        if (EventLogSink.IsEnabledByConfig())
            config.WriteTo.Sink(new EventLogSink(), LogEventLevel.Error);

        if (string.Equals(Environment.GetEnvironmentVariable("DEVTEM_JSON_LOGS"),
                "1", StringComparison.Ordinal))
        {
            config.WriteTo.File(
                new JsonFormatter(),
                path: Path.Combine(logPath, JsonLogFileName),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: JsonLogRetentionDays,
                shared: true);
        }

        var log = config.CreateLogger();

        Serilog.Log.Logger = log;
        // Route the AppLog facade (Microsoft.Extensions.Logging) into this
        // pipeline. Phase 2 swaps this factory for a native MEL one; call
        // sites stay untouched.
        try { AppLog.Initialize(new SerilogLoggerFactory(log, dispose: false)); } catch { }
        try { EnforceDirectoryQuota(logPath, LogDirectorySizeCapBytes); } catch { }
        AppLog.Information("Logging initialized. Log path: {LogPath}", logPath);
    }

    /// <summary>
    /// Deletes oldest <c>applog-*.log</c> files while the directory exceeds
    /// <paramref name="capBytes"/>. Pure enough for headless tests (real
    /// directory, fake files). Never throws.
    /// </summary>
    internal static void EnforceDirectoryQuota(string directory, long capBytes)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(directory) || capBytes <= 0)
                return;
            var dir = new DirectoryInfo(directory);
            if (!dir.Exists)
                return;
            var files = dir.GetFiles("applog-*.log");
            long total = 0;
            foreach (var file in files)
            {
                try { total += file.Length; } catch { }
            }
            if (total <= capBytes)
                return;
            Array.Sort(files, static (left, right) =>
            {
                int byTime = left.LastWriteTimeUtc.CompareTo(right.LastWriteTimeUtc);
                if (byTime != 0)
                    return byTime;
                return string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
            });
            foreach (var file in files)
            {
                if (total <= capBytes)
                    break;
                try
                {
                    long length = file.Length;
                    file.Delete();
                    total -= length;
                }
                catch { }
            }
        }
        catch { }
    }

    /// <summary>
    /// Switches between the compile-time default (Debug in Debug builds,
    /// Information in Release) and Trace minimum levels at
    /// runtime (verbose toggle). Never throws.
    /// </summary>
    public static void SetVerbose(bool verbose)
    {
        try
        {
            ApplyLevel(verbose);
        }
        catch { }
    }

    private static void ApplyLevel(bool verbose)
    {
        _levelSwitch.MinimumLevel = verbose ? LogEventLevel.Verbose : DefaultSerilogLevel;
        MinimumLevel = verbose ? LogLevel.Trace : DefaultMinimumLevel;
    }
}
