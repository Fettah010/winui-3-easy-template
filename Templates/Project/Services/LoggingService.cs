using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using DevTemWinUi3.Services.Diagnostics;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace DevTemWinUi3.Services;

/// <summary>
/// Central logging setup for the app (Serilog). Writes to the debugger
/// console and to daily-rolling files under the Logs/ folder next to the
/// executable, keeping the last 14 days. Every event also lands in the
/// in-memory <see cref="InMemoryLogSink"/> for the live tail and export.
/// </summary>
public static class LoggingService
{
    public const string LogDirectory = "Logs";
    public const string LogFileName = "applog-.log";
    private const int LogRetentionDays = 14;

    /// <summary>Opt-in machine-readable sidecar via <c>DEVTEM_JSON_LOGS=1</c>.</summary>
    private const string JsonLogFileName = "applog-json-.log";
    private const int JsonLogRetentionDays = 7;

    private const string OutputTemplate =
        "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}";

    private static readonly LoggingLevelSwitch _levelSwitch = new(LogEventLevel.Debug);

    public static ILogger Log { get; private set; } = Serilog.Log.Logger;

    /// <summary>Live event buffer (newest ~1 000 events). Never null.</summary>
    public static InMemoryLogSink EventBuffer { get; } = new InMemoryLogSink();

    /// <summary>Runtime level control (verbose toggle). Never throws.</summary>
    public static LoggingLevelSwitch LevelSwitch => _levelSwitch;

    /// <summary>
    /// Effective minimum level (mirrors the Serilog configuration; the
    /// diagnostics page displays it). Defaults to Debug before
    /// <see cref="Initialize"/> runs.
    /// </summary>
    public static LogEventLevel MinimumLevel { get; private set; } = LogEventLevel.Debug;

    /// <summary>
    /// Resolved log directory (set by <see cref="Initialize"/>; defaults to
    /// the primary location so crash paths can report it even when init
    /// never ran). Never throws.
    /// </summary>
    public static string CurrentLogDirectory { get; private set; } =
        Path.Combine(AppContext.BaseDirectory, LogDirectory);

    public static void Initialize()
    {
        // Serilog's own misconfiguration is otherwise silent: route it to
        // the debugger output so a broken pipeline is diagnosable.
        try { Serilog.Debugging.SelfLog.Enable(msg => Debug.WriteLine(msg)); } catch { }

        var logPath = Path.Combine(AppContext.BaseDirectory, LogDirectory);
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
            .WriteTo.Sink(EventBuffer);

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

        Log = config.CreateLogger();

        Serilog.Log.Logger = Log;
        Log.Information("Logging initialized. Log path: {LogPath}", logPath);
    }

    /// <summary>
    /// Switches between Debug (default) and Verbose minimum levels at
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
        var level = verbose ? LogEventLevel.Verbose : LogEventLevel.Debug;
        _levelSwitch.MinimumLevel = level;
        MinimumLevel = level;
    }
}
