using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DevTemWinUi3.Services;

/// <summary>Point-in-time app health for the diagnostics page.</summary>
public sealed record DiagnosticsStatus(
    string AppVersion,
    string Channel,
    string Theme,
    string Language,
    bool IsPackaged,
    bool SentryEnabled,
    int LogFileCount,
    string LogDirectory);

/// <summary>
/// Reads app health + rolling log files for the in-app diagnostics page.
/// All file access stays inside the log directory (paths escaping it are
/// refused). Never throws.
/// </summary>
public static class DiagnosticsService
{
    public const int DefaultTailLines = 200;

    /// <summary>Where <see cref="LoggingService"/> writes (mirrors its fallback).</summary>
    public static string LogDirectoryPath
    {
        get
        {
            try
            {
                var primary = Path.Combine(AppContext.BaseDirectory, LoggingService.LogDirectory);
                if (Directory.Exists(primary))
                    return primary;
                return Path.Combine(Path.GetTempPath(), AppMetadata.AppDataFolder, LoggingService.LogDirectory);
            }
            catch
            {
                return LoggingService.LogDirectory;
            }
        }
    }

    public static DiagnosticsStatus GetStatus()
    {
        try
        {
            int logCount = 0;
            try { logCount = GetLogFiles().Count; } catch { }
            string theme = "System";
            string channel = ChannelResolver.Stable;
            string language = "en-US";
            try { theme = SettingsService.Current.Theme; } catch { }
            try { channel = SettingsService.Current.Channel; } catch { }
            try { language = LocalizationService.Current.CurrentLanguage; } catch { }
            return new DiagnosticsStatus(
                AppVersion: AppInfo.Current.Version,
                Channel: channel,
                Theme: theme,
                Language: language,
                IsPackaged: AppInfo.IsPackaged,
                SentryEnabled: CrashReportingService.Current.IsEnabled,
                LogFileCount: logCount,
                LogDirectory: LogDirectoryPath);
        }
        catch
        {
            return new DiagnosticsStatus("?", ChannelResolver.Stable, "System", "en-US", false, false, 0, "?");
        }
    }

    /// <summary>Full paths of log files, newest first. Empty when none.</summary>
    public static IReadOnlyList<string> GetLogFiles()
    {
        try
        {
            var dir = LogDirectoryPath;
            if (!Directory.Exists(dir))
                return Array.Empty<string>();
            return Directory.GetFiles(dir, "applog-*.log")
                .OrderByDescending(f => f, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Last <paramref name="maxLines"/> lines of a log file. Refuses paths
    /// outside the log directory. Empty string on any error. Opens with
    /// <see cref="FileShare.ReadWrite"/> so the Serilog writer lock never
    /// blanks the view, and keeps only the tail in memory.
    /// </summary>
    public static string ReadLogTail(string path, int maxLines = DefaultTailLines)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || maxLines <= 0)
                return string.Empty;
            var full = Path.GetFullPath(path);
            var dir = Path.GetFullPath(LogDirectoryPath);
            if (!full.StartsWith(dir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(full, dir, StringComparison.OrdinalIgnoreCase))
                return string.Empty;
            if (!File.Exists(full))
                return string.Empty;
            var tail = new Queue<string>(Math.Min(maxLines, 1024));
            using var stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                if (tail.Count == maxLines)
                    tail.Dequeue();
                tail.Enqueue(line);
            }
            return string.Join(Environment.NewLine, tail);
        }
        catch
        {
            return string.Empty;
        }
    }
}
