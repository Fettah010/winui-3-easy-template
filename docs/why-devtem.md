# Why DevTem?

DevTem is a production-ready **WinUI 3 starter template** for Windows desktop
apps built with .NET 10 and the Windows App SDK. It gives a team a working
application foundation instead of a blank window: MVVM structure, a Mica
shell (rail + TabView + MenuBar-command patterns), system-tray activation,
auto-updates (Velopack default; `basic`/`none`/`appinstaller`/`store`
per distribution), first-run setup wizard, localization (en/es/fr, live
switch), persisted settings (schema-versioned + backup/restore), SQLite
data layer, logging facade + Diagnostics page, crash-reporting veneer,
deep-link protocol + single instance, and a release pipeline (GitHub +
Store submit) — each behind a scaffold flag, proven by a 22-combo matrix.

## Head-to-head (summary of `docs/ROADMAP.md` §2, Oct 2026)

| Capability | DevTem | TemplateStudio | Official WASDK | DevWinUI | egvijayanand |
| --- | --- | --- | --- | --- | --- |
| .NET 10 / latest WASDK | ✅ | ⚠️ lags | ✅ | ✅ | ✅ |
| Nav pane + TabView shells | ✅ | ✅ pane only | ✅ | ✅ pane only | — |
| ListDetails + DataGrid pages | ✅ | ✅ | — | — | — |
| Settings + theme + JSON backup/migrate | ✅ | storage only | — | ✅ json | — |
| Runtime localization switch | ✅ (3 langs) | ⚠️ resw | ⚠️ resw item | ✅ | — |
| Tray + autostart + singleton + protocol | ✅ | ❌ | ❌ | ⚠️ autostart opt | ❌ |
| Auto-updates (installed) | ✅ | ❌ | ❌ | ❌ | ❌ |
| Release pipeline (GH + Store) | ✅ | ❌ | ❌ | ❌ | — |
| Setup wizard / OOBE | ✅ | ⚠️ TODOs | ❌ | ⚠️ example | ❌ |
| Logging facade + Diagnostics page | ✅ | ❌ | ❌ | ❌ | — |
| Opt-in crash reporting | ✅ | ⚠️ legacy | ❌ | — | — |
| Tests (unit + UI + matrix) | ✅ | ✅ | ✅ tmpl | ✅ | — |
| Perf budgets + weight gate | ✅ | ❌ | ❌ | ❌ | — |
| Auth (MSAL) | ⚠️ opt-in flag | ✅ option | ❌ | — | — |
| AppNotification (Action Center) | ⚠️ packaged native / unpackaged toast | ✅ | — | ❌ | — |
| ContentGrid / WebView2 | guide-only | ✅ | — | — | — |

Reading: DevTem leads on **shipping plumbing** (updates, tray, diagnostics,
releases, matrix, budgets). Peers lead on shell/page breadth and on
settings-gallery beauty — DevTem adopts those shapes only when they beat the
current answer on measured grounds (see `docs/DECISIONS.md`).

## When DevTem is not the answer

- Learning WinUI or building every service yourself → the official blank
  templates (`winui`, `winui-mvvm`, `winui-navview`, `winui-tabview`).
- Wizard-driven choice breadth (Blank/NavPane/MenuBar × MVVM × pages ×
  MSAL/Auth) with TODOs you will wire yourself → TemplateStudio.
- Cross-platform (Windows + macOS + Linux + mobile) → Uno or Avalonia.
- A gallery of controls to browse → DevWinUI.

## Try it

```powershell
dotnet new install DevTem.Templates
dotnet new devtem-winui -n AcmeDesk
dotnet build -c Debug -p:Platform=x64   # 0 warnings, 0 errors
dotnet test                             # green
```

The source repository is a complete sample app: read, test, and adapt the
generated structure before it becomes a production codebase. Start at
`docs/GETTING-STARTED.md` (15 minutes to a renamed, running app).
