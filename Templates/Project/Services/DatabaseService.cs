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
/// </summary>
public sealed class DatabaseService : IDisposable
{
    private SqliteConnection? _connection;
    private bool _initialized;

    public static DatabaseService Current { get; } = new();

    private DatabaseService()
    {
    }

    public string DatabasePath { get; } = Path.Combine(
        AppPaths.DataFolder, "Data", "app.db");

    /// <summary>
    /// Initializes the database connection and creates tables if needed.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;

        try
        {
            var dir = Path.GetDirectoryName(DatabasePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            _connection = new SqliteConnection($"Data Source={DatabasePath}");
            await _connection.OpenAsync();

            // Create example settings table
            await ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS Settings (
                    Key TEXT PRIMARY KEY,
                    Value TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
                )");

            AppLog.Information("Database initialized: {Path}", DatabasePath);
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Failed to initialize database");
        }
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

        return await cmd.ExecuteNonQueryAsync();
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

        var result = await cmd.ExecuteScalarAsync();
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

        await using var readerResult = await cmd.ExecuteReaderAsync();
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
