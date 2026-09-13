using System;
using System.Globalization;
using System.IO;
using Serilog;
using Serilog.Events;

namespace DevTemWinUi3.Services;

/// <summary>
/// Central logging setup for the app (Serilog). Writes to the debugger
/// console and to daily-rolling files under the Logs/ folder next to the
/// executable, keeping the last 14 days.
/// </summary>
public static class LoggingService
{
    public const string LogDirectory = "Logs";
    public const string LogFileName = "applog-.log";
    private const int LogRetentionDays = 14;

    private const string OutputTemplate =
        "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}";

    public static ILogger Log { get; private set; } = Serilog.Log.Logger;

    public static void Initialize()
    {
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

        Log = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
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
            .CreateLogger();

        Serilog.Log.Logger = Log;
        Log.Information("Logging initialized. Log path: {LogPath}", logPath);
    }
}