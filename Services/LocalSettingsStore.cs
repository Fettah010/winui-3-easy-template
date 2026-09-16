using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace DevTemWinUi3.Services;

/// <summary>
/// File-backed key/value settings store.
/// File location follows <see cref="AppPaths.DataFolder"/> (packaged runs
/// use the package data folder, unpackaged runs use %LocalAppData%):
/// ApplicationData.Current.LocalSettings does not reliably persist without
/// package identity (no settings.dat is ever written), so the file store
/// is used for both distributions: stable across runs, debuggable,
/// and unit-testable.
///
/// Performance plan P0-2: the file is loaded once into an in-memory
/// dictionary; <c>Get</c> never touches disk after warmup, and <c>Set</c>
/// updates memory synchronously while the disk write is debounced and
/// coalesced (one atomic temp+move write per burst). Thread-safe. A corrupt
/// file is backed up next to the original and replaced with defaults.
/// </summary>
public sealed class LocalSettingsStore
{
    private static readonly object _gate = new();
    private static string? _overridePath;
    private static LocalSettingsStore? _shared;

    /// <summary>
    /// Coalescing window for disk writes. Tests set this to
    /// <see cref="TimeSpan.Zero"/> for write-through behavior.
    /// </summary>
    internal static TimeSpan WriteDebounce { get; set; } = TimeSpan.FromMilliseconds(300);

    // Shared serializer options (CA1869): one instance for the process.
    // Invariant-friendly defaults keep the file stable across locales.
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

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
        AppPaths.DataFolder,
        "settings.json");

    /// <summary>
    /// Loads the shared store's file on the calling thread. The launch
    /// path calls this on a background thread during the splash
    /// (<c>App.InitializeServicesAsync</c>) so the first UI-thread
    /// <c>Get</c> is already memory-only. Never throws.
    /// </summary>
    internal static void PreloadShared()
    {
        try
        {
            lock (_gate)
            {
                _shared ??= new LocalSettingsStore(_overridePath ?? DefaultPath);
                _shared.EnsureLoaded();
            }
        }
        catch { }
    }

    /// <summary>
    /// Flushes a pending debounced write of the shared store, if any.
    /// Called on real exit (best-effort). Never throws.
    /// </summary>
    internal static void FlushShared()
    {
        try
        {
            lock (_gate)
            {
                _shared?.Flush();
            }
        }
        catch { }
    }

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
    private bool _dirty;
    private int _saveGeneration;

    /// <summary>Disk writes performed by this instance (tests pin coalescing).</summary>
    internal int CompletedWrites { get; private set; }

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
            ScheduleSave();
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
                ScheduleSave();
        }
    }

    /// <summary>
    /// Writes a pending debounced change to disk now. Call before process
    /// exit (or in tests that assert durability across instances). No-op
    /// when nothing is pending. Never throws.
    /// </summary>
    public void Flush()
    {
        try
        {
            lock (_gate)
            {
                if (!_dirty)
                    return;
                _dirty = false;
                Save();
            }
        }
        catch { }
    }

    /// <summary>
    /// Marks the in-memory state dirty and persists it: immediately when
    /// debouncing is off, otherwise once per burst after
    /// <see cref="WriteDebounce"/>. Must hold <c>_gate</c>.
    /// </summary>
    private void ScheduleSave()
    {
        if (WriteDebounce <= TimeSpan.Zero)
        {
            _dirty = false;
            Save();
            return;
        }
        _dirty = true;
        int generation = ++_saveGeneration;
        TimeSpan delay = WriteDebounce;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delay).ConfigureAwait(false);
            }
            catch
            {
                return;
            }
            try
            {
                lock (_gate)
                {
                    if (generation != _saveGeneration || !_dirty)
                        return;
                    _dirty = false;
                    Save();
                }
            }
            catch { }
        });
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
                var backup = _path + ".corrupt-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
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
                JsonSerializer.Serialize(stream, _data, _jsonOptions);
            }
            File.Move(temp, _path, overwrite: true);
            CompletedWrites++;
        }
        catch { }
    }
}
