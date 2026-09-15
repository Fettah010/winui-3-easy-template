namespace DevTemWinUi3.Services;

/// <summary>
/// Scaffold-time feature flags for the app (Distribution Plan P0/P2).
/// This repo copy pins the full portable defaults. The template copy is
/// rendered by replaces (bools become True/False literals,
/// <c>__UPDATES__</c>/<c>__DISTRIBUTION__</c> become the selected values)
/// and parses defensively, so a scaffold still builds the full app even
/// if a placeholder ever came through unreplaced.
/// Gates UI visibility and layout at runtime, which keeps .xaml free of
/// conditionals (the template engine does not evaluate markers there).
/// </summary>
internal static class AppFeatures
{
    public static readonly bool Tray = true;
    public static readonly string UpdateMode = "velopack";
    public static readonly bool Updates = UpdateMode != "none";
    public static readonly bool Database = true;
    public static readonly bool Http = true;
    public static readonly bool Health = true;
    public static readonly bool Crash = true;
    public static readonly bool Localization = true;
    public static readonly bool Tests = true;
    public static readonly string Distribution = "portable";
    public static readonly bool SetupWizard = true;

    /// <summary>Updates managed outside the app (Store / AppInstaller feed).</summary>
    public static bool IsExternalUpdateMode => UpdateMode is "appinstaller" or "store";

    /// <summary>
    /// Updates the app must not drive itself: scaffold-external modes plus
    /// any packaged run (the install dir is read-only there, so even a
    /// compiled-in Velopack engine can never apply). This is what makes one
    /// binary dual-track: unpackaged it follows the scaffold engine,
    /// packaged it takes the slim status surface.
    /// </summary>
    public static bool IsExternallyManaged => IsExternalUpdateMode || AppInfo.IsPackaged;
}
