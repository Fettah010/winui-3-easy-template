namespace DevTemWinUi3.Services;

/// <summary>
/// Persists window state (position, size, maximized) across sessions.
/// Backed by <see cref="LocalSettingsStore"/> (JSON file).
/// </summary>
public sealed class WindowStateService
{
    private const string KeyWindowX = "WindowX";
    private const string KeyWindowY = "WindowY";
    private const string KeyWindowWidth = "WindowWidth";
    private const string KeyWindowHeight = "WindowHeight";
    private const string KeyIsMaximized = "WindowIsMaximized";

    public static WindowStateService Current { get; } = new();

    private WindowStateService()
    {
    }

    private static LocalSettingsStore Store => LocalSettingsStore.Shared;

    public double X
    {
        get => Store.Get(KeyWindowX, 100.0);
        set => Store.Set(KeyWindowX, value);
    }

    public double Y
    {
        get => Store.Get(KeyWindowY, 100.0);
        set => Store.Set(KeyWindowY, value);
    }

    public double Width
    {
        get => Store.Get(KeyWindowWidth, 1200.0);
        set => Store.Set(KeyWindowWidth, value);
    }

    public double Height
    {
        get => Store.Get(KeyWindowHeight, 800.0);
        set => Store.Set(KeyWindowHeight, value);
    }

    public bool IsMaximized
    {
        get => Store.Get(KeyIsMaximized, false);
        set => Store.Set(KeyIsMaximized, value);
    }

    public bool HasSavedState => Store.Contains(KeyWindowX);

    public void Clear()
    {
        Store.Remove(KeyWindowX);
        Store.Remove(KeyWindowY);
        Store.Remove(KeyWindowWidth);
        Store.Remove(KeyWindowHeight);
        Store.Remove(KeyIsMaximized);
    }
}
