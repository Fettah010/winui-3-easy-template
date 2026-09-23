using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace DevTemWinUi3.Services;

/// <summary>
/// SQLite database service for local data persistence.
/// Database file lives under <see cref="AppPaths.DataFolder"/> (writable
/// for both distributions — the packaged install directory is read-only).
/// Performance plan P1-3: WAL journal mode + busy timeout, retryable init
/// (a failed first init no longer latches success), and an ordered
/// idempotent migration runner over a <c>schema_version</c> table.
/// </summary>
public sealed class DatabaseService : IDisposable
{
    /// <summary>Current schema version (highest migration applied to fresh DBs).</summary>
    internal const int CurrentSchemaVersion = 2;

    private const int MaxInitAttempts = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(100);

    private SqliteConnection? _connection;
    private bool _initialized;
    private readonly object _initGate = new();

    public static DatabaseService Current { get; } = new();

    private DatabaseService()
    {
    }

    /// <summary>Test seam: an instance bound to a scratch file.</summary>
    internal DatabaseService(string databasePath)
    {
        DatabasePath = databasePath;
    }

    public string DatabasePath { get; } = Path.Combine(
        AppPaths.DataFolder, "Data", "app.db");

    /// <summary>Whether <see cref="InitializeAsync"/> has succeeded.</summary>
    public bool IsInitialized
    {
        get
        {
            lock (_initGate)
            {
                return _initialized;
            }
        }
    }

    /// <summary>
    /// Ordered schema migrations (version, SQL). Each entry must be
    /// idempotent (<c>IF NOT EXISTS</c>) so a killed init can safely
    /// re-run. Version 1 is the original Settings table; version 2 adds
    /// the version tracking itself.
    /// </summary>
    internal static readonly (int Version, string Sql)[] Migrations = new[]
    {
        (1, @"
                CREATE TABLE IF NOT EXISTS Settings (
                    Key TEXT PRIMARY KEY,
                    Value TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
                )"),
        (2, @"
                CREATE TABLE IF NOT EXISTS schema_version (
                    Version INTEGER PRIMARY KEY,
                    AppliedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
                )"),
    };

    /// <summary>
    /// Initializes the database connection and migrates the schema.
    /// Retries transient failures (<see cref="MaxInitAttempts"/>); success
    /// is latched only after the migrations commit, so a kill during first
    /// init recovers on the next call. Never throws (logs and reports via
    /// <see cref="IsInitialized"/>).
    /// </summary>
    public async Task InitializeAsync()
    {
        lock (_initGate)
        {
            if (_initialized) return;
        }

        Exception? lastError = null;
        for (int attempt = 1; attempt <= MaxInitAttempts; attempt++)
        {
            try
            {
                await InitializeCoreAsync().ConfigureAwait(false);
                lock (_initGate)
                {
                    _initialized = true;
                }
                AppLog.Information("Database initialized: {Path}", DatabasePath);
                return;
            }
            catch (Exception ex)
            {
                lastError = ex;
                if (attempt < MaxInitAttempts)
                {
                    try { await Task.Delay(RetryDelay).ConfigureAwait(false); } catch { }
                }
            }
        }

        if (lastError is not null)
            AppLog.Error(lastError, "Failed to initialize database");
    }

    private async Task InitializeCoreAsync()
    {
        var dir = Path.GetDirectoryName(DatabasePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var connection = new SqliteConnection($"Data Source={DatabasePath}");
        await connection.OpenAsync().ConfigureAwait(false);

        try
        {
            await ApplyPragmasAsync(connection).ConfigureAwait(false);
            await ApplyMigrationsAsync(connection).ConfigureAwait(false);
        }
        catch
        {
            try { connection.Dispose(); } catch { }
            throw;
        }

        var previous = _connection;
        _connection = connection;
        try
        {
            previous?.Dispose();
        }
        catch { }
    }

    private static async Task ApplyPragmasAsync(SqliteConnection connection)
    {
        // WAL lets readers proceed during writes (the deferred-init and UI
        // threads share this file); a busy timeout turns SQLITE_BUSY into
        // a short wait instead of an instant failure. Best-effort: a
        // database that refuses pragmas still works, just slower.
        string[] pragmas = new string[]
        {
            "PRAGMA journal_mode=WAL;",
            "PRAGMA busy_timeout=5000;",
            "PRAGMA synchronous=NORMAL;",
        };
        foreach (var pragma in pragmas)
        {
            try
            {
                await using var cmd = connection.CreateCommand();
                cmd.CommandText = pragma;
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            catch { }
        }
    }

    /// <summary>
    /// Applies pending migrations in version order and records each in
    /// <c>schema_version</c>. Idempotent: re-running applies nothing.
    /// </summary>
    internal static async Task ApplyMigrationsAsync(SqliteConnection connection)
    {
        // The version table itself is bootstrapped first: stamps below
        // would otherwise fail on fresh databases (no such table).
        await using (var bootstrap = connection.CreateCommand())
        {
            bootstrap.CommandText = @"
                CREATE TABLE IF NOT EXISTS schema_version (
                    Version INTEGER PRIMARY KEY,
                    AppliedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
                )";
            await bootstrap.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
        int applied = await ReadSchemaVersionAsync(connection).ConfigureAwait(false);
        foreach (var (version, sql) in Migrations)
        {
            if (version <= applied)
                continue;
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            await using var stamp = connection.CreateCommand();
            stamp.CommandText = "INSERT OR IGNORE INTO schema_version (Version) VALUES ($v)";
            stamp.Parameters.AddWithValue("$v", version);
            await stamp.ExecuteNonQueryAsync().ConfigureAwait(false);
            applied = version;
        }
    }

    internal static async Task<int> ReadSchemaVersionAsync(SqliteConnection connection)
    {
        try
        {
            await using var check = connection.CreateCommand();
            check.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='schema_version'";
            var exists = await check.ExecuteScalarAsync().ConfigureAwait(false);
            if (exists is null || exists == DBNull.Value)
                return 0;
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COALESCE(MAX(Version), 0) FROM schema_version";
            var result = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
            if (result is null || result == DBNull.Value)
                return 0;
            return Convert.ToInt32(result, CultureInfo.InvariantCulture);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Hard guard for every query (D4): a wedged query fails instead of
    /// hanging the caller forever. Applied to every command below.
    /// </summary>
    internal static TimeSpan QueryTimeout
    {
        get => _queryTimeout;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, TimeSpan.Zero);
            _queryTimeout = value;
        }
    }
    private static TimeSpan _queryTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Slow-query tripwire (D4): anything slower logs a warning with its
    /// elapsed time so regressions surface in Diagnostics instead of
    /// hiding as vague UI jank. Local SQLite should answer in ms.
    /// </summary>
    internal static TimeSpan SlowQueryThreshold
    {
        get => _slowQueryThreshold;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, TimeSpan.Zero);
            _slowQueryThreshold = value;
        }
    }
    private static TimeSpan _slowQueryThreshold = TimeSpan.FromSeconds(2);

    private static async Task<T> WithQueryGuardAsync<T>(string operation, Func<Task<T>> run)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            return await run().ConfigureAwait(false);
        }
        finally
        {
            try
            {
                watch.Stop();
                if (watch.Elapsed >= SlowQueryThreshold)
                    AppLog.Warning("Slow DB query ({Operation}): {ElapsedMs}ms", operation, watch.Elapsed.TotalMilliseconds);
            }
            catch { }
        }
    }

    private static void ApplyTimeout(Microsoft.Data.Sqlite.SqliteCommand cmd)
    {
        try { cmd.CommandTimeout = (int)QueryTimeout.TotalSeconds; } catch { }
    }

    /// <summary>
    /// Executes a SQL command and returns the number of affected rows.
    /// </summary>
    public async Task<int> ExecuteAsync(string sql, params SqliteParameter[] parameters)
    {
        if (_connection is null)
            throw new InvalidOperationException("Database not initialized");

        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        if (parameters.Length > 0)
            cmd.Parameters.AddRange(parameters);
        ApplyTimeout(cmd);

        return await WithQueryGuardAsync("Execute", () => cmd.ExecuteNonQueryAsync());
    }

    /// <summary>
    /// Executes a query and returns a scalar value.
    /// </summary>
    public async Task<T?> ExecuteScalarAsync<T>(string sql, params SqliteParameter[] parameters)
    {
        if (_connection is null)
            throw new InvalidOperationException("Database not initialized");

        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        if (parameters.Length > 0)
            cmd.Parameters.AddRange(parameters);
        ApplyTimeout(cmd);

        var result = await WithQueryGuardAsync("ExecuteScalar", () => cmd.ExecuteScalarAsync());
        if (result is null || result == DBNull.Value)
            return default;

        return (T)Convert.ChangeType(result, typeof(T), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Executes a query and returns all rows via a reader callback.
    /// </summary>
    public async Task<T> QueryAsync<T>(string sql, Func<SqliteDataReader, T> reader,
        params SqliteParameter[] parameters)
    {
        if (_connection is null)
            throw new InvalidOperationException("Database not initialized");

        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        if (parameters.Length > 0)
            cmd.Parameters.AddRange(parameters);
        ApplyTimeout(cmd);

        await using var readerResult = await WithQueryGuardAsync("Query", () => cmd.ExecuteReaderAsync());
        return reader(readerResult);
    }

    /// <summary>
    /// Sets a key-value pair in the Settings table.
    /// </summary>
    public Task SetSettingAsync(string key, string value)
    {
        return ExecuteAsync(@"
            INSERT OR REPLACE INTO Settings (Key, Value, UpdatedAt)
            VALUES ($key, $value, $timestamp)",
            new SqliteParameter("$key", key),
            new SqliteParameter("$value", value),
            new SqliteParameter("$timestamp", DateTime.UtcNow.ToString("O")));
    }

    /// <summary>
    /// Gets a value from the Settings table.
    /// </summary>
    public async Task<string?> GetSettingAsync(string key)
    {
        return await ExecuteScalarAsync<string>(
            "SELECT Value FROM Settings WHERE Key = $key",
            new SqliteParameter("$key", key));
    }

    public void Dispose()
    {
        _connection?.Dispose();
        _connection = null;
    }
}
