using Windows.System;

namespace DevTemWinUi3.Services;

/// <summary>
/// App-wide keyboard actions. The rail-first UI stays mouse-first; these
/// accelerators fill the gap (Windows conventions, not inventions).
/// </summary>
public enum KeyboardAction
{
    None,
    GoBack,
    GoForward,
    OpenSettings,
    DismissUpdateFlow,
}

/// <summary>
/// Pure key-to-action map for the main window (see
/// <c>MainWindow.RootGrid_KeyDown</c>). Reservation list — new nav items
/// from <c>add-page.ps1</c> get NO accelerator by default (avoid
/// collisions); claim a new one here first, then wire it:
/// <list type="bullet">
/// <item>Alt+Left / Alt+Right (mouse X1/X2): back / forward.</item>
/// <item>Ctrl+, (comma): Settings.</item>
/// <item>Escape: dismiss the update flow (check cancel, dialog, legacy popup).</item>
/// </list>
/// Pure so it unit-tests headless; the window owns the thin event wiring.
/// </summary>
public static class KeyboardShortcuts
{
    /// <summary>
    /// The comma key. <see cref="VirtualKey"/> names no OEM keys, so this is
    /// the Win32 VK_OEM_COMMA value (0xBC) cast to the enum — the value
    /// <c>KeyRoutedEventArgs.Key</c> carries for a comma press.
    /// </summary>
    internal const VirtualKey CommaKey = (VirtualKey)0xBC;

    /// <summary>
    /// Maps a key + modifier state to an action. Shift is ignored (no
    /// shift-chord exists); anything outside the reservation list is None.
    /// Never throws.
    /// </summary>
    public static KeyboardAction Resolve(VirtualKey key, bool ctrl, bool alt)
    {
        try
        {
            if (alt && !ctrl)
            {
                if (key == VirtualKey.Left)
                    return KeyboardAction.GoBack;
                if (key == VirtualKey.Right)
                    return KeyboardAction.GoForward;
                return KeyboardAction.None;
            }
            if (ctrl && !alt)
            {
                if (key == CommaKey)
                    return KeyboardAction.OpenSettings;
                return KeyboardAction.None;
            }
            if (!ctrl && !alt && key == VirtualKey.Escape)
                return KeyboardAction.DismissUpdateFlow;
            return KeyboardAction.None;
        }
        catch
        {
            return KeyboardAction.None;
        }
    }
}
