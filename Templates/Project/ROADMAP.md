# Advanced Features Roadmap

Research-backed features for production WinUI 3 apps.

---

## 1. Localization (i18n/l10n) — COMPLETED

**Status:** Implemented  
**Complexity:** High  
**Priority:** Critical for global reach

### Why it's critical
- Any app targeting multiple markets needs this from day one
- Retrofitting localization is extremely painful — every hardcoded string must be found and replaced
- Professional apps must handle string translation, culturally correct formatting, and text expansion

### Implementation details
- `Strings/` folder with `.resw` files per locale
- `x:Uid` directives on XAML elements
- `ResourceLoader` for code-behind strings
- Runtime language switching via `ApplicationLanguages.PrimaryLanguageOverride`
- Composite/format strings with `{0}` placeholders
- Text expansion handling in layouts

---

## 2. System Tray (Minimize-to-Tray) — PLANNED

**Status:** Not started  
**Complexity:** Very High  
**Priority:** Essential UX for utility apps

### Why it's critical
- WinUI 3 has **NO native system tray support**
- Users expect utility apps to live in the tray — closing the window should not kill the app
- Without tray support, apps cannot run background tasks or maintain presence

### Implementation details
- Win32 P/Invoke using `Shell_NotifyIconW` from `shell32.dll`
- `NOTIFYICONDATA` structures for icon, tooltip, callbacks
- `WndProc` message loop for tray events
- Context menu via Win32 `TrackPopupMenu`
- Handle `TaskbarCreated` (Explorer restart) to recreate icon
- `AppWindow.IsShownInSwitchers = false` for hide behavior

### Key challenges
- No managed wrapper exists
- Explorer restart handling is tricky
- Icon lifetime management
- Message pump quirks

---

## 3. Crash Reporting (Sentry) — PLANNED

**Status:** Not started  
**Complexity:** Medium-High  
**Priority:** Essential for production support

### Why it's critical
- Production apps crash — without reporting, zero visibility
- Users don't file bug reports — they uninstall
- Enables data-driven development based on actual user impact

### Implementation details
- Global `UnhandledException` + `TaskScheduler.UnobservedTaskException` handlers
- Sentry SDK initialization (must be before any UI code)
- Privacy consent layer (GDPR compliance)
- CI/CD symbol upload for readable stack traces
- Offline queue for crashes without network

### Key challenges
- Initialization order matters (before UI)
- Privacy layer legally required in many jurisdictions
- Symbol upload in CI/CD frequently forgotten

---

## Features Considered But Not in Top 3

| Feature | Why not top 3 |
|---------|---------------|
| Single Instance | Well-documented, ~50 lines of code |
| Notifications | Good official documentation, straightforward setup |
| Background Services | Niche — not every app needs them |
| Protocol Activation | Niche — specific use cases |
| Auto-start | Simple registry operation |

---

## Implementation Order

1. ✅ Localization (current)
2. System Tray (next)
3. Crash Reporting (after tray)
