using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using DevTemWinUi3.Services.Diagnostics;

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
    string LogDirectory,
    long StartupElapsedMs,
    string LogLevel,
    long LogDirectorySizeBytes,
    int BufferedEventCount,
    string PendingUpdate,
    long DatabaseSizeBytes);

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
                // Source of truth is the logging backend's resolved
                // directory (packaged vs portable aware). The legacy
                // BaseDirectory probe stays as a fallback for trees that
                // wrote logs next to the binary.
                string current;
                try
                {
                    current = LoggingService.CurrentLogDirectory;
                }
                catch
                {
                    current = string.Empty;
                }
                if (!string.IsNullOrWhiteSpace(current) && Directory.Exists(current))
                    return current;
                var legacy = Path.Combine(AppContext.BaseDirectory, LoggingService.LogDirectory);
                if (Directory.Exists(legacy))
                    return legacy;
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
            long startupMs = 0;
            string logLevel = "Debug";
            long logDirBytes = 0;
            int buffered = 0;
            string pending = string.Empty;
            long dbBytes = -1;
            try { theme = SettingsService.Current.Theme; } catch { }
            try { channel = SettingsService.Current.Channel; } catch { }
            try { language = LocalizationService.Current.CurrentLanguage; } catch { }
            try { startupMs = Program.StartupStopwatch.ElapsedMilliseconds; } catch { }
            try { logLevel = LoggingService.MinimumLevel.ToString(); } catch { }
            try { logDirBytes = SumLogFileSizes(); } catch { }
            try { buffered = LoggingService.EventBuffer.Count; } catch { }
            try { pending = SettingsService.Current.PendingVersion ?? string.Empty; } catch { }
            try { dbBytes = GetDatabaseFileSize(); } catch { }
            return new DiagnosticsStatus(
                AppVersion: AppInfo.Current.Version,
                Channel: channel,
                Theme: theme,
                Language: language,
                IsPackaged: AppInfo.IsPackaged,
                SentryEnabled: CrashReportingService.Current.IsEnabled,
                LogFileCount: logCount,
                LogDirectory: LogDirectoryPath,
                StartupElapsedMs: startupMs,
                LogLevel: logLevel,
                LogDirectorySizeBytes: logDirBytes,
                BufferedEventCount: buffered,
                PendingUpdate: pending,
                DatabaseSizeBytes: dbBytes);
        }
        catch
        {
            return new DiagnosticsStatus("?", ChannelResolver.Stable, "System", "en-US", false, false, 0, "?", 0, "Debug", 0, 0, string.Empty, -1);
        }
    }

    private static long SumLogFileSizes()
    {
        long total = 0;
        foreach (var file in GetLogFiles())
        {
            try { total += new FileInfo(file).Length; } catch { }
        }
        return total;
    }

    /// <summary>
    /// SQLite file size in bytes, -1 when there is no database. Resolved
    /// from the real data root (<see cref="AppPaths.DataFolder"/>, P1-3) —
    /// not <c>AppContext.BaseDirectory</c>, which disagrees with it on
    /// packaged runs. Never throws.
    /// </summary>
    private static long GetDatabaseFileSize()
    {
        try
        {
            var path = Path.Combine(AppPaths.DataFolder, "Data", "app.db");
            return File.Exists(path) ? new FileInfo(path).Length : -1;
        }
        catch
        {
            return -1;
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
    /// Newest buffered events (live tail source), capped at
    /// <paramref name="maxEvents"/>. Empty when nothing is buffered or on
    /// any error. Never throws.
    /// </summary>
    public static IReadOnlyList<BufferedLogEvent> GetBufferedEvents(int maxEvents = 200)
    {
        try
        {
            if (maxEvents <= 0)
                return Array.Empty<BufferedLogEvent>();
            var result = new List<BufferedLogEvent>(Math.Min(maxEvents, 256));
            foreach (var e in LoggingService.EventBuffer.SnapshotNewestFirst())
            {
                if (result.Count >= maxEvents)
                    break;
                result.Add(MapBufferedEvent(e));
            }
            return result;
        }
        catch
        {
            return Array.Empty<BufferedLogEvent>();
        }
    }

    private static BufferedLogEvent MapBufferedEvent(LogEntry e)
    {
        try
        {
            string message = e.Message ?? string.Empty;
            string? source = string.IsNullOrWhiteSpace(e.Category) ? null : e.Category;
            string? exceptionText = null;
            var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var kv in e.Properties)
                {
                    try { properties[kv.Key] = Truncate(kv.Value ?? string.Empty, 500); } catch { }
                }
            }
            catch { }
            try
            {
                if (e.Exception is not null)
                    exceptionText = Truncate(e.Exception.ToString(), 2000);
            }
            catch { }
            return new BufferedLogEvent(
                e.Timestamp, e.Level.ToString(), message,
                source, e.Exception is not null, exceptionText, properties);
        }
        catch
        {
            return new BufferedLogEvent(DateTimeOffset.MinValue, "?", string.Empty, null, false, null,
                new Dictionary<string, string>());
        }
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value ?? string.Empty;
        return string.Concat(value.AsSpan(0, maxLength), "…");
    }

    /// <summary>
    /// Last <paramref name="maxLines"/> lines of a log file. Refuses paths
    /// outside the log directory. Empty string on any error. Opens with
    /// <see cref="FileShare.ReadWrite"/> so the Serilog writer lock never
    /// blanks the view, and keeps only the tail in memory.
    /// P1-2: files over <see cref="TailSeekThresholdBytes"/> are read from
    /// a bounded window near the end (early-exit) instead of scanned whole.
    /// </summary>
    public static string ReadLogTail(string path, int maxLines = DefaultTailLines)
    {
        const long TailSeekThresholdBytes = 1024L * 1024;
        const long TailWindowBytes = 256L * 1024;
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
            long offset = 0;
            try
            {
                if (stream.Length > TailSeekThresholdBytes)
                    offset = Math.Max(0, stream.Length - TailWindowBytes);
                stream.Seek(offset, SeekOrigin.Begin);
            }
            catch
            {
                try { stream.Seek(0, SeekOrigin.Begin); } catch { }
                offset = 0;
            }
            using var reader = new StreamReader(stream);
            string? line;
            bool skippedPartial = false;
            while ((line = reader.ReadLine()) is not null)
            {
                // A mid-file seek starts mid-line: drop the fragment.
                if (offset > 0 && !skippedPartial)
                {
                    skippedPartial = true;
                    continue;
                }
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

    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// Writes a diagnostic bundle zip: status snapshot, redacted settings
    /// (see <see cref="SettingsBackupService.Capture"/>), the current
    /// filtered view, and the full current log file. Returns false (never
    /// throws) when anything cannot be written.
    /// </summary>
    public static bool CreateDiagnosticBundle(
        string destinationPath,
        string filteredText,
        string? selectedLogFileName,
        DiagnosticsStatus? status)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(destinationPath))
                return false;
            var dir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            var snapshot = status ?? GetStatus();
            using var zip = new ZipArchive(
                File.Open(destinationPath, FileMode.Create), ZipArchiveMode.Create);
            WriteZipEntry(zip, "status.json",
                JsonSerializer.Serialize(snapshot, s_jsonOptions));
            WriteZipEntry(zip, "settings.json",
                JsonSerializer.Serialize(SettingsBackupService.Capture(), s_jsonOptions));
            WriteZipEntry(zip, "log-filtered.log", ScrubUserPaths(filteredText ?? string.Empty));
            WriteZipEntry(zip, "log-current.log",
                ScrubUserPaths(ReadSelectedLogFullText(selectedLogFileName)));
            return true;
        }
        catch (Exception ex)
        {
            try { AppLog.Error(ex, "Diagnostic bundle export failed"); } catch { }
            return false;
        }
    }

    private static void WriteZipEntry(ZipArchive zip, string name, string content)
    {
        var entry = zip.CreateEntry(name);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content ?? string.Empty);
    }

    /// <summary>
    /// Scrubs machine-specific paths from exported text: log lines embed
    /// absolute paths (<c>C:\Users\Bob\...</c>), and the bundle goes to
    /// support. Replaces the local-app-data root, the user profile root,
    /// and the bare username with stable placeholders. Pure and
    /// headless-testable. Case-insensitive; longest match first so
    /// <c>Users\Bob\AppData\Local</c> does not half-scrub to
    /// <c>Users\&lt;user&gt;\AppData\Local</c>.
    /// </summary>
    internal static string ScrubUserPaths(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text ?? string.Empty;
        string result = text;
        try
        {
            string? user = Environment.UserName;
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var replacements = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrWhiteSpace(localAppData))
                replacements.Add(new KeyValuePair<string, string>(localAppData, "<localappdata>"));
            if (!string.IsNullOrWhiteSpace(profile))
                replacements.Add(new KeyValuePair<string, string>(profile, "<profile>"));
            if (!string.IsNullOrWhiteSpace(user))
                replacements.Add(new KeyValuePair<string, string>(user, "<user>"));
            replacements.Sort((a, b) => b.Key.Length.CompareTo(a.Key.Length));
            foreach (var pair in replacements)
                result = result.Replace(pair.Key, pair.Value, StringComparison.OrdinalIgnoreCase);
        }
        catch { }

        return result;
    }

    /// <summary>
    /// Full text of the selected log file (shared read, 8 MB cap).
    /// Empty string when unresolvable or on any error. Never throws.
    /// </summary>
    internal static string ReadSelectedLogFullText(string? selectedLogFileName)
    {
        const int MaxChars = 8 * 1024 * 1024;
        try
        {
            if (string.IsNullOrEmpty(selectedLogFileName))
                return string.Empty;
            var full = GetLogFiles()
                .FirstOrDefault(p => Path.GetFileName(p) == selectedLogFileName);
            if (full is null || !File.Exists(full))
                return string.Empty;
            using var stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            char[] buffer = new char[64 * 1024];
            var text = new StringBuilder();
            int read;
            while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                int room = MaxChars - text.Length;
                if (room <= 0)
                    break;
                text.Append(buffer, 0, Math.Min(read, room));
            }
            return text.ToString();
        }
        catch
        {
            return string.Empty;
        }
    }
}
