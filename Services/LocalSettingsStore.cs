using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DevTemWinUi3.Services;

/// <summary>
/// File-backed key/value settings store for unpackaged apps.
///
/// ApplicationData.Current.LocalSettings does not reliably persist without
/// package identity (no settings.dat is ever written), so every setting
/// silently reverted to its default on next launch. Settings live in
/// %LocalAppData%\DevTemWinUi3\settings.json instead: stable across runs,
/// debuggable, and unit-testable.
///
/// Thread-safe. Writes are atomic (temp file + move). A corrupt file is
/// backed up next to the original and replaced with defaults.
/// </summary>
public sealed class LocalSettingsStore
{
    private static readonly object _gate = new();
    private static string? _overridePath;
    private static LocalSettingsStore? _shared;

    /// <summary>Process-wide shared store at the default path.</summary>
    public static LocalSettingsStore Shared
    {
        get
        {
            lock (_gate)
            {
                _shared ??= new LocalSettingsStore(_overridePath ?? DefaultPath);
                return _shared;
            }
        }
    }

    internal static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppMetadata.AppDataFolder,
        "settings.json");

    /// <summary>
    /// Redirects <see cref="Shared"/> to another file. Tests only.
    /// Pass null to restore the default path.
    /// </summary>
    internal static void SetTestPath(string? path)
    {
        lock (_gate)
        {
            _overridePath = path;
            _shared = null;
        }
    }

    private readonly string _path;
    private Dictionary<string, JsonElement> _data = new();
    private bool _loaded;

    /// <summary>Creates a store bound to a specific file (tests, tools).</summary>
    public LocalSettingsStore(string path)
    {
        _path = path;
    }

    public T Get<T>(string key, T defaultValue)
    {
        lock (_gate)
        {
            EnsureLoaded();
            if (_data.TryGetValue(key, out var element))
            {
                try
                {
                    var value = element.Deserialize<T>();
                    if (value is not null)
                        return value;
                }
                catch { }
            }
            return defaultValue;
        }
    }

    public void Set<T>(string key, T value)
    {
        lock (_gate)
        {
            EnsureLoaded();
            _data[key] = JsonSerializer.SerializeToElement(value);
            Save();
        }
    }

    public bool Contains(string key)
    {
        lock (_gate)
        {
            EnsureLoaded();
            return _data.ContainsKey(key);
        }
    }

    public void Remove(string key)
    {
        lock (_gate)
        {
            EnsureLoaded();
            if (_data.Remove(key))
                Save();
        }
    }

    private void EnsureLoaded()
    {
        if (_loaded)
            return;
        _loaded = true;

        try
        {
            if (File.Exists(_path))
            {
                using var stream = File.OpenRead(_path);
                var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(stream);
                if (data is not null)
                    _data = data;
            }
        }
        catch
        {
            // Corrupt file: back it up for diagnosis, start clean.
            try
            {
                var backup = _path + ".corrupt-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                File.Move(_path, backup);
            }
            catch { }
            _data = new Dictionary<string, JsonElement>();
        }
    }

    private void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var temp = _path + ".tmp";
            using (var stream = File.Create(temp))
            {
                JsonSerializer.Serialize(stream, _data, new JsonSerializerOptions { WriteIndented = true });
            }
            File.Move(temp, _path, overwrite: true);
        }
        catch { }
    }
}
