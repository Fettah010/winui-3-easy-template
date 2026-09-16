namespace DevTemWinUi3.Services;

/// <summary>
/// Burst policy for in-app toasts (performance plan P1-2): each toast
/// holds a multi-second delay plus storyboards, so an event burst churns
/// the host panel. At most <see cref="MaxVisible"/> cards show at once;
/// the rest queue (bounded at <see cref="MaxQueued"/>, oldest dropped).
/// Pure and headless-testable; <see cref="NotificationService"/> owns the
/// host lifetime.
/// </summary>
internal static class ToastPolicy
{
    internal const int MaxVisible = 3;
    internal const int MaxQueued = 10;

    internal static bool ShouldShowNow(int visibleCount) => visibleCount < MaxVisible;

    internal static bool ShouldQueue(int queuedCount) => queuedCount < MaxQueued;
}
