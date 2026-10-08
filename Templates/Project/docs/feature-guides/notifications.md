# Notifications (OS toasts + in-app cards + one activation road)

Every scaffold ships both surfaces; they are not alternatives:

| Surface | Owner | When |
| --- | --- | --- |
| OS Action Center toast | `Services/DesktopToastService.cs` (`AppNotificationManager`) | Events the user must see while the window is hidden (tray minimize, update notices). Falls back to in-app UI when registration fails. |
| In-app card | `Services/NotificationService.cs` (bottom-right host) | Everything while the window is visible. Bursts queue past `ToastPolicy.MaxVisible`. |

Lifetime: `MainWindow` initializes both once on the UI thread
(`NotificationService` with its host, `DesktopToastService.Register()`)
and unregisters on real exit. Toast tags replace instead of stacking.

## One activation road

Toast clicks and deep-link launches land identically:

- A toast may carry a `route` argument (`TryShowRouted`): on click,
  `RouteActivationRequested` fires with the parsed tag and `MainWindow`
  foregrounds the window, then navigates to the tag (unknown tags no-op).
- A plain toast fires `ActivationRequested`: the window foregrounds.
- A `devtem://` launch (command line unpackaged, activation args packaged,
  pending-file second-instance handoff) navigates through the same
  `NavigationService` after the window is visible.

The routing decision (`ResolveActivation` / `TryExtractRoute`) is pure and
headless-tested; only payload construction and display touch the OS.

## File activation (guide-only)

File activation waits for a consumer (same rule as the auth deferral):
no manifest file-type extension and no file-activation args path ship
until a real app needs them. When one does, it rides this same road
(manifest extension + `AppLifecycle` file args + pure parse + tests) —
ask, and it becomes a scoped feature instead of dead code.
