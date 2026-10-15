# Video shot-list draft (v1.0 — MP4 render deferred)

> Status: shot-list only. `docs/screenshots/demo-tour.png` (8-panel filmstrip,
> 216KB, captured at 1.0.0) IS the shipped tour asset. Correction record:
> v0.9.0's `demo.gif` was a single static frame, not 8 frames — `System.Drawing`
> GIF `SaveAdd` encodes single-frame only on this stack (proven in isolation),
> and the filmstrip composer that replaced it first shipped with a banker's-
> rounding row bug (fixed with `Floor`). Animated GIF/MP4 stays deferred: no
> encoder on this machine. Nothing here auto-renders.

## Sequence (~60s at 1080p, EN dark, maximized)

1. Terminal: `dotnet new install DevTem.Templates` → `dotnet new devtem-winui -n AcmeDesk` (5s).
2. `dotnet build -c Debug -p:Platform=x64` → 0 warnings, 0 errors (5s).
3. Launch: Home page, welcome card (5s).
4. Settings: theme combo → Dark; language stays English (8s).
5. Check for updates → resolves (toast/dialog), page stays alive (8s).
6. Test toast → in-app card appears (5s).
7. Diagnostics: status + metrics + live log tail + export bundle (8s).
8. About: version 1.0.0, stack, links (5s).
9. Tray: minimize-to-tray, restore via tray icon (6s).
10. End card: install command + repo URL (5s).

## Render notes

- Capture with the app maximized (see v0.9.0 media lessons in
  `docs/DECISIONS.md`: crop-to-rect, settle pages, minimize-others).
- Keep under ~25MB for GitHub release attachments.
- File alongside `demo-tour.png` in `docs/screenshots/` when rendered; wire into
  README next to the tour strip.
