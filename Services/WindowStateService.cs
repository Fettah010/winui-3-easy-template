using System;
using Windows.Storage;

namespace DevTemWinUi3.Services;

/// <summary>
/// Persists window state (position, size, maximized) across sessions.
/// Uses Windows.Storage.ApplicationData for storage.
/// </summary>
public sealed class WindowStateService
{
    private const string KeyWindowX = "WindowX";
    private const string KeyWindowY = "WindowY";
    private const string KeyWindowWidth = "WindowWidth";
    private const string KeyWindowHeight = "WindowHeight";
    private const string KeyIsMaximized = "WindowIsMaximized";

    public static WindowStateService Current { get; } = new();

    private ApplicationDataContainer? _localSettings;
    private bool _initialized;

    private bool TryInit()
    {
        if (_initialized) return _localSettings != null;
        _initialized = true;
        try
        {
            _localSettings = ApplicationData.Current.LocalSettings;
        }
        catch
        {
            // Unpackaged or pre-runtime context — gracefully degrade
        }
        return _localSettings != null;
    }

    private WindowStateService()
    {
    }

    public double X
    {
        get => ReadDouble(KeyWindowX, 100);
        set => WriteDouble(KeyWindowX, value);
    }

    public double Y
    {
        get => ReadDouble(KeyWindowY, 100);
        set => WriteDouble(KeyWindowY, value);
    }

    public double Width
    {
        get => ReadDouble(KeyWindowWidth, 1200);
        set => WriteDouble(KeyWindowWidth, value);
    }

    public double Height
    {
        get => ReadDouble(KeyWindowHeight, 800);
        set => WriteDouble(KeyWindowHeight, value);
    }

    public bool IsMaximized
    {
        get => ReadBool(KeyIsMaximized, false);
        set => WriteBool(KeyIsMaximized, value);
    }

    public bool HasSavedState
    {
        get
        {
            if (!TryInit()) return false;
            try
            {
                return _localSettings!.Values.ContainsKey(KeyWindowX);
            }
            catch { return false; }
        }
    }

    private double ReadDouble(string key, double defaultValue)
    {
        if (!TryInit()) return defaultValue;
        try
        {
            if (_localSettings!.Values.TryGetValue(key, out var obj) && obj is double d)
                return d;
            if (obj is int i)
                return i;
        }
        catch { }
        return defaultValue;
    }

    private void WriteDouble(string key, double value)
    {
        if (!TryInit()) return;
        try { _localSettings!.Values[key] = value; } catch { }
    }

    private bool ReadBool(string key, bool defaultValue)
    {
        if (!TryInit()) return defaultValue;
        try
        {
            if (_localSettings!.Values.TryGetValue(key, out var obj) && obj is bool b)
                return b;
        }
        catch { }
        return defaultValue;
    }

    private void WriteBool(string key, bool value)
    {
        if (!TryInit()) return;
        try { _localSettings!.Values[key] = value; } catch { }
    }

    public void Clear()
    {
        if (!TryInit()) return;
        try
        {
            _localSettings!.Values.Remove(KeyWindowX);
            _localSettings.Values.Remove(KeyWindowY);
            _localSettings.Values.Remove(KeyWindowWidth);
            _localSettings.Values.Remove(KeyWindowHeight);
            _localSettings.Values.Remove(KeyIsMaximized);
        }
        catch { }
    }
}
