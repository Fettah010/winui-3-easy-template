# DevTem WinUI 3 — Roadmap to v1.0 (Best WinUI 3 Template for .NET 10 Devs)

> Status: **PLAN ONLY — no code changes in this commit.**
> Current baseline: app `0.0.28-beta` / templates `0.4.0` (.NET 10, WindowsAppSDK 1.8.260804001, Velopack 1.2.0, CommunityToolkit.Mvvm 8.4.2).
> Version policy from here: `0.1.0 → 0.2.0 → … → 0.9.0 → 1.0` (MINOR per feature batch, PATCH for fixes). Velopack versions must keep increasing — one version per channel.
> Old plan files proposed for deletion are listed in §8. **Nothing is deleted by this file** — deletion is a separate confirmed step (see §8).

---

## 1. Where DevTem stands today (evidence, not marketing)

Extracted from `README.md`, `AGENTS.md`, `CHANGELOG.md` (0.0.28-beta), `Services/`, `Scripts/`, `Templates/Project/.template.config/template.json`, `docs/template-features.json`.

### 1.1 What ships out of the box (all-on reference scaffold)

| Area | DevTem today |
| --- | --- |
| Target | WinUI 3 / Windows App SDK on **.NET 10** (`net10.0-windows10.0.19041.0`), unpackaged default (`WindowsPackageType=None`, self-contained) |
| Shell | Mica + extended titlebar (`WindowChromeService`), overlay NavigationView rail (`LeftCompact`), native min size 720x540, entrance animation, responsive code-behind (Home/About/Diagnostics/Settings — VSM retired) |
| MVVM | CommunityToolkit.Mvvm (`ObservableProperty`, `RelayCommand`), `PageFactory` ctor injection, `NavigationRegistry` data-driven nav, `NavigationService` + cache policy |
| Pages | Home, Settings, About, Diagnostics, SetupWizard (PipsPager); item templates: `devtem-page`, `devtem-list-details` (`-Kind list`), `devtem-datagrid` (`-Kind grid`, DataGrid 7.1.2) |
| Updates | Velopack default (toasts + native dialogs via `UpdateDialogService`/`UpdateCenterViewModel`, auto-install vs ask-mode, metered guard, 30s timeout, SHA-256 verify for `basic` mode, `basic` zero-dep checker, `none` null-tolerant, `appinstaller`/`store` native for MSIX) |
| Distribution | `portable` (default) / `msix` (Store/sideload, `build-msix.ps1`, `.appinstaller`, Store submit scripts, runtime-adaptive `AppPaths`/`AppFeatures.IsExternallyManaged`) |
| Setup | First-run wizard (location/shortcuts/launch, never auto-opens in app; installers own setup), welcome + versioned whats-new dialogs |
| Tray | Minimize-to-tray, single-instance + protocol handoff (`WindowActivator`), autostart (HKCU Run quoted / StartupTask when packaged), theme-aware icons |
| Deep links | `devtem://` parsing (pure + tested), HKCU self-register, MSIX protocol extension, `register-protocol.ps1` |
| Logging | `AppLog` facade; Serilog default (console + rolling file 14d, JSON sidecar opt, SelfLog), MEL (debugger + EventLog), `none` (buffer only when health on); ring buffer + enrichment + verbose toggle + export bundle (off UI thread) |
| Diagnostics | Diagnostics page (startup time, log level, files, buffer, packaged state, pending update, DB size, nav timings), live tail + pause-on-scroll + regex/exception filters + detail flyout + save-view + bundle-zip (redacted), `StartupBudgets` armed + CI weight gate (`measure-publish-weight.ps1`, baseline ~267.8 MB) |
| Data | SQLite (WAL + busy-timeout, retryable init, `schema_version` migrations, `IRepository<T>`, 30s/2s guards), typed `ApiService` + `ExponentialRetryHandler` + `ApiResult<T>` + cancellation |
| Settings | `SettingsService` (`LocalSettingsStore` JSON, never `ApplicationData.LocalSettings`), `SettingsSchema` v1 validated + migrations, backup export/import (1 MB cap + version), reset-to-defaults |
| Crash | Veneer `CrashReportingService` over `ICrashReporter` (Sentry DSN-gated, off by default, `SendDefaultPii=false`, droppable `--crash false`) |
| Localization | 3 languages (en/es/fr) via dictionaries + `{loc:Loc}` markup, live switch, `LocalizationCoverageTests`, neutral default strings |
| A11y (partial) | Keyboard accelerators (Back/Alt+Left, Ctrl+, Settings, Esc dismiss), `LiveSetting Polite` toast/status + Assertive errors, `x:DataType` compiled bindings, dialog-button language rule, text-scaling audit tests pinned |
| Branding | `init-template.ps1` (rename + `-Validate` identity), `set-app-icon.ps1` (one PNG → ico + Logo set + MSIX tiles at pack), `ProductConfiguration` + `DEVTEM_*` env, `AppMetadata` runtime repo/license resolution |
| Scaffolding | `dotnet new devtem-winui` (identity params + `--tray/--database/--http/--health/--crash/--localization/--tests` + `--updates velopack|basic|none|appinstaller|store` + `--logging serilog|mel|none` + `--distribution portable|msix` + `--setup` + `--publisher`), `add-page.ps1` (route/title/icon, `-Kind page|list|grid`, dirty-guard + rollback), `remove-sample-content.ps1` (inverse), `init-profile.ps1` (minimal/recommended/full) |
| Tests/CI | MSTest headless (~350 tests) + FlaUI smoke (launch/nav/theme/language, auto-retry), `test-mirror-parity.ps1` + 21-combo matrix (Fast subset on PRs, full on main/nightly/tags), `DocsDriftTests`, `RepoHygieneTests`, parallel `gh` release upload, `release.yml` + `store-submit.yml` + `templates-publish.yml`, Dependabot policy |
| Docs | `README.md` + `AGENTS.md` + `GETTING-STARTED.md` + `TEMPLATE-GUIDE.md` + `MAINTAINERS.md` + `WORKFLOW.md` + `STATE.md` + `DECISIONS.md` (append-only) + per-feature guides |

### 1.2 Known gaps / deferred items (from DECISIONS + ADVANCEMENT-PLAN)

- **B5 manual remainder:** 200% text-scaling + 4 high-contrast themes + EN/ES screenshots at 900px/1920px (needs live desktop).
- **Auth:** evaluated, DEFERRED (MSAL broker vs embedded, token-cache per distribution) — no code until a real consumer asks.
- **IHost:** spiked, DECLINED (~110 ms cold) — question closed.
- **Suspend/resume:** spiked, CLOSED (WinAppSDK has no suspend state) — `Closed` pattern already covers it.
- **PublishTrimmed/AOT:** declined for WinUI; annual re-check due 2027-09.
- **`--winappsdk`/`--net` params, ContentGrid, WebView/Map/Camera:** declined or deferred (matrix cost / weight / app-domain).
- **Per-combo weight ceilings:** deferred to nightly/tag (21 publishes ≈ 84 min).
- **VS dialog screenshots, Narrator pass, per-state screenshots:** manual, still open.

---

## 2. Peer templates researched (Oct 2026)

| # | Template | Channel | What it gives you (extracted) |
| --- | --- | --- | --- |
| T1 | **microsoft/TemplateStudio** (WinUI, ★2.8k, VSIX wizard) | VS New Project → wizard | Project types: Blank / Navigation Pane / Menu Bar. Pattern: MVVM Toolkit. Pages: Blank, Settings, WebView2, ListDetails, ContentGrid, DataGrid. Features: Settings Storage, MSIX Packaging, Theme Selection, Toast/AppNotification. Services: HTTP data + sample data. Tests: MSTest/NUnit/xUnit + UI test. Activation handlers, suspend/resume, deep-link, first-run/whats-new TODOs. Strength: choice breadth. Weakness: no release pipeline, no updater, no tray, generated TODOs instead of working plumbing. |
| T2 | **microsoft/WindowsAppSDK official `dotnet new`** (`winui`, `winui-mvvm`, `winui-navview`, `winui-tabview`, `winui-lib`, `winui-unittest` + item templates) | `dotnet new install Microsoft.WindowsAppSDK.WinUI.CSharp.Templates` | Minimal blank/MVVM/NavView/TabView shells, MSIX single-project, `--dotnet-version net8/9/10`, `dotnet run` loose-MSIX identity. Strength: official, tiny, TabView shell DevTem lacks. Weakness: intentionally blank — no updates/tray/settings/i18n/diagnostics/releases. |
| T3 | **DevWinUI** (successor of WinUICommunity-Templates, VSIX + CLI) | Marketplace `DevWinUITemplates` | NavigationView + custom TitleBar + HomeLandingPage + Settings (theme/backdrop/tint), JSON app settings, dynamic localization, AOT-compatible, auto-startup option, always-latest WASDK/dotnet resolver, Breadcrumb/PinBox/SettingsCard controls, LandingPages. Strength: prettiest settings + control gallery. Weakness: no updater/release/tray/matrix story. |
| T4 | **egvijayanand/winui-templates** (CLI + VSIX, VS2022/2026 x64/ARM64) | `dotnet new winui -mvvm -f net10 -cpm -slnx` | Framework param (net8/9/10/11), MVVM flag, Central Package Mgmt, SLNX, Blazor Hybrid (MAUI embedding), class lib, page/window/usercontrol items. Strength: TF matrix + packaging ergonomics. Weakness: shells only, no app plumbing. |
| T5 | **GabrielePepe/Winui3_Template** (minimal) + generic minimal starters | GitHub clone | Titlebar + NavView + back button + theme SettingsCard (DevWinUI). Strength: learnable in 10 min. Weakness: not production. |
| T6 | **Uno `unoapp` / Avalonia `avalonia.mvvm`** (cross-platform peers, already spike-compared) | CLI wizard | IHost + DI + logging + config + lifetime, presets (Blank/Recommended/Customize), `.resw` localization, auth option, `--remove-view-locator` trimming, `-f/-av` version params, compiled bindings default. Used as divergence checklist — DevTem differs on purpose where noted (bare container, dictionaries <4 langs, no `--profile` symbol, `x:DataType` adopted). |

### 2.1 Head-to-head: DevTem vs peers (today)

| Capability | DevTem 0.0.28 | TemplateStudio | Official WASDK | DevWinUI | egvijayanand |
| --- | --- | --- | --- | --- | --- |
| .NET 10 / latest WASDK | ✅ (.NET10, WASDK 1.8) | ⚠️ lags | ✅ (net8/9/10 + selector) | ✅ (auto-latest) | ✅ (`-f` selector) |
| MVVM Toolkit | ✅ | ✅ | ✅ (`winui-mvvm`) | ✅ optional | ✅ flag |
| Nav Pane shell | ✅ (overlay rail) | ✅ | ✅ (`winui-navview`) | ✅ | — |
| TabView shell | ❌ | — | ✅ | — | — |
| MenuBar shell | ❌ (accelerators only) | ✅ | — | — | — |
| ListDetails page | ✅ (`devtem-list-details`) | ✅ | — | — | — |
| DataGrid page | ✅ (`devtem-datagrid`, 7.1.2) | ✅ | — | — | — |
| ContentGrid page | ❌ (declined: list covers it) | ✅ | — | — | — |
| WebView2 sample | ❌ (declined: weight/privacy) | ✅ | — | — | — |
| Settings + theme + backdrop | ✅ (Mica, code-behind responsive) | ✅ | — | ✅ (richest cards) | — |
| JSON settings + backup/migrate | ✅ (schema v1 + backup) | ✅ (storage only) | — | ✅ (json) | — |
| Localization (runtime switch) | ✅ (3 langs, loc ext) | ⚠️ (.resw) | ⚠️ (resw item) | ✅ (dynamic) | — |
| System tray + autostart | ✅ | ❌ | ❌ | ⚠️ (autostart opt) | ❌ |
| Auto-updates (installed) | ✅ (Velopack + basic + native) | ❌ | ❌ | ❌ | ❌ |
| Release pipeline (GH + Store) | ✅ (parallel upload, MSIX, Store submit) | ❌ | ❌ | ❌ | ❌ |
| Setup wizard / OOBE | ✅ (portable PipsPager) | ⚠️ (TODOs) | ❌ | ⚠️ (OOBE example) | ❌ |
| Deep-link protocol + singleton | ✅ | ⚠️ (activation TODO) | ❌ | ❌ | ❌ |
| Logging facade + diagnostics page | ✅ (Serilog/MEL/none + export) | ❌ | ❌ | ❌ | ❌ |
| Crash reporting (opt-in) | ✅ (Sentry veneer) | ⚠️ (AppCenter legacy) | ❌ | ❌ | ❌ |
| Auth (MSAL) | ❌ (evaluated, deferred) | ✅ (MSAL option) | ❌ | ❌ | ❌ |
| AppNotification (Action Center) | ⚠️ (DesktopToast + in-app cards; AppNotificationManager not adopted) | ✅ | — | ❌ | ❌ |
| Tests (unit + UI) | ✅ (MSTest + FlaUI + matrix) | ✅ (multi-framework) | ✅ (unittest tmpl) | ✅ (unit test) | — |
| Scaffold flags / presets | ✅ (13 flags + 5 presets + init-profile) | ✅ (wizard) | ⚠️ (TF only) | ⚠️ (libs picker) | ✅ (TF/CPM/SLNX) |
| Mirror parity + matrix CI | ✅ (parity + 21 combos) | — | — | — | — |
| Perf budgets + weight gate | ✅ (armed + CI) | ❌ | ❌ | ❌ | ❌ |
| A11y (keyboard/SR/contrast/scaling) | ⚠️ (KB+SR done; HC/scaling screenshots manual) | ⚠️ (basics) | — | — | — |
| Docs (start → daily → maintainer) | ✅ (split + drift tests) | ✅ (wizard docs) | ✅ (MS Learn) | ✅ (gallery) | ✅ (readme) |
| Screenshots / VS dialog proof | ⚠️ (home/settings/toast only; VS shots manual) | ✅ | ✅ | ✅ | — |

**Reading:** DevTem already leads on *shipping plumbing* (updates, tray, diagnostics, releases, matrix). Peers lead on *shell/pages breadth* (TabView, MenuBar, ContentGrid, WebView2), *settings beauty* (DevWinUI cards), *packaging ergonomics* (TF selector, CPM, SLNX), and *auth/notifications* (TemplateStudio MSAL + AppNotificationManager).

---

## 3. v1.0 definition of done — "best for WinUI 3 .NET 10 devs"

1. **Starts clean, stays safe:** `dotnet new devtem-winui` → builds 0/0 → `dotnet test` green → `remove-sample-content` green; secret scan + quoting pins + checksum verify all green.
2. **Shells for real apps:** rail (today) + TabView + MenuBar-command patterns documented; every shell honors theme/backdrop/HC/scaling + keyboard + Narrator.
3. **Pages for real apps:** blank + list-details + datagrid (today) + content-grid + tabbed/master + settings-section guidance; `add-page -Kind` covers all, matrix proves all.
4. **Updates/distribution solved:** Velopack + basic + AppInstaller + Store truth table proven live (installed-app video per mode); Store submit one-command; weight trend per release.
5. **Desktop citizenship:** tray + singleton + protocol + file activation + autostart + OS notifications (AppNotificationManager where packaged) + offline/error/empty states everywhere.
6. **Data story:** SQLite (today) + optional EF Core + sample-data service + paged/sorted/filtered list guidance; DB guards + migration tests.
7. **Identity when needed:** opt-in `--auth` (MSAL broker-first, DPAPI cache per distribution) — off by default, zero weight when off.
8. **Quality bars:** 0 warnings, armed startup + weight budgets, per-combo ceilings nightly, FlaUI green without re-runs, HC + 200% screenshots EN/ES, Narrator pass notes.
9. **DevEx:** VS dialog renders + `dotnet new update` clean, `init-profile` + TF/CPM/SLNX ergonomics, `bump-version` one-command reset, encoding discipline never regresses.
10. **Docs that sell:** README answers "why DevTem" in 60s with 6 screenshots + GIF, GETTING-STARTED 15-min green, TEMPLATE-GUIDE daily, MAINTAINERS release; `why-devtem.md` + site/wiki + announcement kit (marketplace post + awesome-winui PR + video).

---

## 4. Version plan (spread evenly — each MINOR is shippable + demoable)

> Convention: each version lists **Goal → Ships → Proof → README/screenshots delta**. Verification tiers per `docs/WORKFLOW.md` (Fast always; Matrix on `Templates/**`; Live UI on XAML; Pack on manifest/assets).

### v0.1.0 — Foundation reset + trust baseline
**Goal:** close the 0.0.x era honestly; make the tree the clean base every later version builds on.
- Renumber `0.0.28-beta` → `0.1.0-beta` (csproj quartet + CITATION + CHANGELOG section; keep `-beta` channel suffix; Velopack-increasing).
- Delete retired plan files (see §8 list — requires human confirm; history stays in CHANGELOG + DECISIONS).
- Finish **B5 static + schedule live pass**: keep `TextScalingAuditTests`; file the live screenshot matrix as v0.1.0 checklist (200%, 4 HC themes, EN/ES, 900/1920px) — land what a quiet desktop allows, mark the rest `manual`.
- Baseline hygiene: `DocsDriftTests` + `RepoHygieneTests` + parity + Fast matrix green; re-pin publish weight (`measure-publish-weight.ps1`) + cold flame in STATE.
- **README/screenshots (v0.1.0):** refresh "What's new in 0.1.0" (reset story + what's next to 1.0 link), verify `docs/screenshots/home.png`, `settings.png`, `toast.png` match current pixels (ABA re-capture if drifted); add `docs/screenshots/about.png` (About + recall entry) if missing.
- Proof: build 0/0, tests green, parity OK, matrix Fast green, 4 screenshots current.

### v0.2.0 — Shell choice (answer TemplateStudio + official TabView)
**Goal:** no one picks another template just for a shell shape.
- Add **`--shell rail|tabs`** scaffold dimension? Cheaper first step (preferred): keep rail default + ship a **`devtem-tabview` item template** + `add-page -Kind tab` guidance reusing `NavigationRegistry` (no fork of `MainWindow`). Decide rail-vs-tabs doc (when to use which).
- Adopt **MenuBar-command pattern** without a second shell: document + sample (`KeyboardShortcuts` reservation list + MenuBar sample page behind `add-page -Kind menubar` or guide-only if weight argues).
- Settings beauty pass (close DevWinUI gap without new deps): `SettingsCard`-style grouping, section headers `BodyStrong`, max-width 1000–1100px scroll layout per MS guidelines; keep code-behind responsive rule.
- **README/screenshots:** add `shell-tabs.png` + `settings-cards.png`; README "Shells" table (rail vs tabs vs menubar-commands).
- Proof: Matrix + Live UI (nav + theme + pane-toggle still green), new template resolves from nupkg.

### v0.3.0 — Pages + data (answer ListDetails/ContentGrid/WebView questions once)
**Goal:** every common page is one command; data guidance exists beyond `IRepository<T>`.
- Add **`devtem-contentgrid`** (reflowing grid over the list VM — proves "list covers grid" or replaces the decline with a real shape) + `add-page -Kind grid|contentgrid` unified; settings-section snippet guide.
- **WebView2:** ship as *opt-in guide + sample* (not default scaffold) with weight + privacy notes (answers TemplateStudio without paying the cost by default).
- Data: **sample-data service** (`SampleDataService` behind `IBackgroundTask`?) + paged/sorted/filtered list example on the grid template; optional **EF Core** spike (keep `DatabaseService` default; document when EF wins).
- **README/screenshots:** `list-details.png`, `datagrid.png`, `contentgrid.png`; "Pages" matrix (kind → when → command).
- Proof: per-kind matrix combos, `ApplySort`/paging tests, weight delta recorded.

### v0.4.0 — Identity + notifications (answer MSAL + AppNotification)
**Goal:** close the two biggest "TemplateStudio has it" gaps, flag-gated so defaults stay lean.
- **`--auth` flag (MSAL):** broker-first + embedded fallback, DPAPI cache under `AppPaths` per distribution, login/logout UI + `AuthService`, excluded cleanly when off (no identity SDK in default). This executes the deferred C6 evaluation — revisit with a real tenant spike first.
- **Notifications:** adopt `AppNotificationManager` for packaged runs (keep `DesktopToastService` unpackaged + in-app cards everywhere); toast-click activation + protocol handoff unified; document which API for which app type (MS Learn table).
- File activation (waits-for-consumer per F2): add as part of auth-adjacent activation work if a consumer exists; otherwise guide-only.
- **README/screenshots:** `auth.png` (login state), `notification.png` (Action Center + in-app card); FEATURES + flag table updated.
- Proof: `auth` on/off matrix combos, DSN/auth secret-scan green, packaged notification proof (manual kit).

### v0.5.0 — Distribution + Store GA (answer "can I actually ship?")
**Goal:** every distribution cell is proven, not just documented.
- Harden the **updates × distribution truth table** (`distribution-dual.md`): Velopack/basic/none × portable + AppInstaller/Store/none × MSIX — each valid cell gets a live proof note (installed-app video/GIF), invalid cells keep MSBuild guards + `badupd` combo.
- **Store submit GA:** `publish-store.ps1` + `submit-store.ps1` + `store-submit.yml` end-to-end on a real listing (WACK + Partner Center); `new-store-listing.ps1` output reviewed; signing docs (`--signParams`, Azure Trusted Signing) in MAINTAINERS.
- Per-combo weight ceilings nightly (execute deferred D2): single-config gate stays on PRs; nightly publishes all 21 + asserts ceilings.
- **README/screenshots:** `store-listing.png` (draft), `updates-table.png` (or ASCII table in README); release notes template filled.
- Proof: nightly weight job green, Store draft exists, two back-to-back update-test releases green.

### v0.6.0 — DevEx + packaging ergonomics (answer egvijayanand + VS polish)
**Goal:** scaffolding feels modern in CLI *and* VS.
- Adopt the **shape** (not the fork): `--framework net10` selector? Only if matrix cost is bounded — otherwise document TF edit + prove net10 default. Evaluate **CPM** (`-cpm`) and **SLNX** opt behind flags (egvijayanand parity) with matrix spots.
- **VS presentation:** `PackageReleaseNotes` filled per release, icon/tags verified, VS New Project dialog screenshots per release PR (execute deferred E5 manual), cache-troubleshooting verified on VS 2026.
- `init-profile.ps1` + `init-template -Validate` v2 (repo/publisher/scheme/URL checks in scaffold README first-steps).
- **README/screenshots:** `vs-dialog.png`, `scaffold-help.png` (`dotnet new devtem-winui --help` capture); presets table re-verified.
- Proof: nupkg audit (entry count + 4 templates resolve + `dotnet new update` clean), VS shots in release PR.

### v0.7.0 — Quality + accessibility RC (answer "polished vs works")
**Goal:** a11y + empty/error/loading + perf are proven, not asserted.
- **Live a11y pass:** Narrator run (toast host Polite, errors Assertive, log tail deliberately silent), keyboard map doc, HC + 200% screenshot matrix completed (closes B5), per-state screenshots (Home offline/error, Settings failure-retains-last-good, wizard validation, download re-arm).
- Empty/error/loading gallery: every page demonstrates its states with loc keys; `DialogButtonLanguageTests`-class coverage for new surfaces.
- Perf: cold flame re-recorded, `StartupBudgets` + weight ceilings hold, DB/tail/image guards re-proven; ARM64 smoke spot.
- **README/screenshots:** `hc-light.png`, `hc-dark.png`, `scaling-200.png`, `empty-state.png`, `error-state.png` (thumbnails in README, full set in `docs/screenshots/`).
- Proof: Live UI tier + screenshot matrix filed in PR, Narrator notes in DECISIONS.

### v0.8.0 — Ecosystem + control polish (cherry-picked, not cloned)
**Goal:** adopt peer controls only where they beat the current answer (house rule).
- Evaluate **DevWinUI/CommunityToolkit SettingsCard/Expander + LandingPages** for Home/About/Settings (adopt if zero-dep and weight-neutral; otherwise keep hand-rolled + document why).
- Evaluate **Win2D** (splash art? icon pipeline?) — likely guide-only; **Map/Camera** stay declined (app-domain).
- **AOT annual check** (due 2027-09 — early pull-in if WinAppSDK line moves); `.resw` re-evaluation trigger stays "4th language request".
- Telemetry opt-in? Only as *sample* (never default-on): document AppCenter/Sentry envelope choice.
- **README/screenshots:** `controls.png` (before/after if adopted); DECISIONS entries per adopt/decline.
- Proof: weight delta + matrix + Live UI for any adopted control.

### v0.9.0 — Hardening + docs IA v2 (release candidate)
**Goal:** v1.0 is a date, not a hope.
- Security: basic-updater tamper/timeout fixtures, import-cap fixtures, registry-quoting pins, secret-scan, Sentry PII posture re-pinned, `basic` release checksum CI check.
- Reliability: smoke-flake hunt (quiet-desktop note → CI display proof, both-attempts evidence), `badupd`/`nodiag`/`nosetup` combos green, backup/restore round-trips.
- Docs IA: GETTING-STARTED (15-min) re-timed on a fresh machine, TEMPLATE-GUIDE daily-only, MAINTAINERS release-accurate, `why-devtem.md` rewritten from §2 table, wiki/site draft.
- **README/screenshots:** full 6-shot refresh at frozen version (see §5), GIF (30s: scaffold → run → theme → update check → tray).
- Proof: full matrix + Pack + Live UI + Workflow tiers green on a tag candidate; CHANGELOG v1.0 draft.

### v1.0 — Stable (the "best for WinUI 3 .NET 10 devs" release)
**Goal:** API/flag freeze + LTS posture + announcement.
- Freeze template symbols/values (renames = new values, never renames — standing policy); `template.json` constraints documented; version-reset stays one command (`bump-version -Version 0.0.1`) unless engine gains post-actions.
- Templates `1.0.0` on NuGet + app `1.0.0` stable channel (`stable` pointer moved); `beta` stays for next cycle.
- Announcement kit: GitHub release notes (from CHANGELOG), NuGet release notes, marketplace post draft (`docs/discovery/` successors), awesome-winui PR, video/GIF, social preview refresh.
- Post-1.0 tracks opened (not started): AOT annual, 4th-language trigger, file-activation consumer, EF-Core demand, `--winappsdk` demand — each with a revisit trigger, not a promise.
- Proof: install-from-NuGet → scaffold all presets → build/test/matrix/smoke green on a clean VM; 6 screenshots + GIF current.

---

## 5. README + screenshots plan (per version — DO NOT execute yet)

> This section is the *plan* for doc updates. No README/screenshot writes happen until each version ships.

| Version | README delta | Screenshots to (re)capture |
| --- | --- | --- |
| v0.1.0 | "What's new in 0.1.0" + v1.0-roadmap link; version-reset one-liner re-verified | Re-verify `home.png`, `settings.png`, `toast.png`; add `about.png` |
| v0.2.0 | "Shells" table (rail/tabs/menubar) | `shell-tabs.png`, `settings-cards.png` |
| v0.3.0 | "Pages" matrix (kind → command) | `list-details.png`, `datagrid.png`, `contentgrid.png` |
| v0.4.0 | Flags table (`--auth`, notifications) + privacy posture | `auth.png`, `notification.png` |
| v0.5.0 | Distribution truth table (valid/invalid cells) | `store-listing.png`, installed-update GIF |
| v0.6.0 | Presets + VS section | `vs-dialog.png`, `scaffold-help.png` |
| v0.7.0 | A11y statement + state gallery | `hc-light.png`, `hc-dark.png`, `scaling-200.png`, `empty-state.png`, `error-state.png` |
| v0.8.0 | Controls adopted/declined table | `controls.png` |
| v0.9.0 | Full refresh + 30s GIF | All 6 frozen + `demo.gif` |
| v1.0 | Stable banner + NuGet/VS badges + showcase link | `social-preview.png` refresh + announcement assets |

Screenshot discipline (carried forward): ABA (before/after) captures, EN/ES for user-visible strings, 900px + 1920px widths, HC + 200% for layout proof, filed in the version PR.

---

## 6. What we deliberately will NOT do (with reasons)

| Item | Verdict | Reason |
| --- | --- | --- |
| VSIX distribution | Declined | NuGet IS the VS channel (engine runs in New Project dialog) |
| MAUI/Avalonia ports | Declined | Different dialects; Uno owns reuse |
| WebView2/Map/Camera in default scaffold | Declined (guide/sample only) | Weight + privacy surface; app-domain |
| Polly | Declined until circuit-breaking needed | In-repo `ExponentialRetryHandler` suffices |
| MVVM-toolkit switch / test-framework switch | Deferred | Forks every VM/test for preference; revisit on demand |
| Custom post-scaffold runners | Declined | Worse UX than flags; engine runs no post-actions by design |
| New template booleans without a consumer | Declined | Flag discipline (P3-4); C6-rule applies |
| `PublishTrimmed`/`PublishAot` default | Declined; annual track | WinUI-unsupported (XAML + WinRT projections) |
| Telemetry default-on | Declined | Sample-only; privacy posture in crash guide |

---

## 7. How to work this roadmap (process, not code)

1. One version at a time, top-down. Phases overlap only when blocked (note the blocker on the box, like B5).
2. Verification per `docs/WORKFLOW.md` tiers; `Templates/**` changes always run parity + matrix subset (full nightly/pre-tag).
3. Check the box in the same commit as the work; refresh `docs/STATE.md` at session end; append to `docs/DECISIONS.md` (append-only).
4. Template MINOR for new symbols/values, PATCH for content (standing policy); app follows patch/beta (Velopack must keep increasing).
5. Never commit/push/tag without a direct human request (AGENTS.md rule). Releases are prepared + handed over as exact commands.
6. Encoding discipline: UTF-16LE CRLF for `.ps1`/`.csproj`/`.xaml`/`.json`; UTF-8 for `.md`; convert back + probe-scaffold after touching UTF-16.

---

## 8. Old plan/doc files — deletion proposal (CONFIRM BEFORE DELETING)

> The request was "delete all old plans/docs .md files" with "don't do any changes yet". Deletion is destructive, so **this file deletes nothing**. Confirm the list below, then delete in one commit (history stays in git + CHANGELOG + DECISIONS).

| File | Status | Proposal |
| --- | --- | --- |
| `docs/ADVANCEMENT-PLAN.md` | Phases A–F shipped (only B5-live + E5/B9 screenshot remainders manual) | **Delete** (self-instructs deletion when phases ship) — migrate B5/E5/B9 remainders into v0.1.0/v0.7.0 boxes above first |
| `Templates/Project/docs/ADVANCEMENT-PLAN.md` | Mirror of above | **Delete with it** (parity manifest update) |
| `docs/discovery/**` (`phase-c-ready/*.md`, `measurement.md`) | Launch/marketing drafts (articles, reddit-launch, marketplace, screencast, stack-overflow, awesome-winui-pr, measurement) | **Delete** OR move to `docs/archive/discovery/` if announcement kit wants them for v1.0 — decide at v0.1.0 |
| `docs/archive/updates-legacy/` | Retired UpdatesPage reference (excluded from build) | **Keep** (explicit reading-reference exception in AGENTS.md gotcha 9) |
| `docs/STATE.md`, `docs/WORKFLOW.md`, `docs/DECISIONS.md`, `docs/GETTING-STARTED.md`, `docs/TEMPLATE-GUIDE.md`, `docs/MAINTAINERS.md`, `docs/diagnostics.md`, `docs/template-contract.md`, `docs/why-devtem.md`, `CHANGELOG.md`, `README.md`, `AGENTS.md`, `CONTRIBUTING.md`, `SECURITY.md` | Live docs (bootstrap order + guides + policies) | **Keep** — refresh per §5, add drift guards, never bulk-delete |
| `Templates/Project/docs/*` mirrors + `docs/feature-guides/*` | Scaffold-time docs (excluded per flag) | **Keep** — matrix + parity depend on them |
| Root `ROADMAP.md` / `*-PLAN.md` (if any remain) | Already deleted per 0.0.10 notes | Confirm absent; if found, delete |

Suggested deletion command (run ONLY after confirming §8 table with the maintainer):

```powershell
git rm docs/ADVANCEMENT-PLAN.md Templates/Project/docs/ADVANCEMENT-PLAN.md
git rm -r docs/discovery
git status; git log --oneline -5
```

---

## Appendix — Sources

- Repo: `README.md`, `AGENTS.md`, `CHANGELOG.md` (0.0.28-beta), `DevTemWinUi3.csproj` (v0.0.28), `docs/STATE.md`, `docs/DECISIONS.md`, `docs/ADVANCEMENT-PLAN.md`, `docs/GETTING-STARTED.md`, `docs/MAINTAINERS.md`, `docs/WORKFLOW.md`, `docs/template-features.json`, `Services/` (48 files), `Scripts/` (22 scripts), `Templates/` (Project/Page/ListDetails/DataGrid).
- Peers: `microsoft/TemplateStudio` (wizard: Blank/NavPane/MenuBar; MVVM Toolkit; pages Blank/Settings/WebView/ListDetails/ContentGrid/DataGrid; Settings Storage/MSIX/Theme/AppNotification/MSAL/tests), `microsoft/WindowsAppSDK` `dev/Templates/Dotnet` (`winui`/`winui-mvvm`/`winui-navview`/`winui-tabview`/`winui-lib`/`winui-unittest` + item templates + `--dotnet-version`), `egvijayanand/winui-templates` (`-f`/`-mvvm`/`-cpm`/`-slnx`, Blazor hybrid), `DevWinUI`/`WinUICommunity-Templates` (NavView/TitleBar/HomeLanding/Settings-theme/JSON settings/dynamic loc/AOT/autostart), `GabrielePepe/Winui3_Template` (minimal), Uno `unoapp` + Avalonia `avalonia.mvvm` (IHost/presets/.resw/auth — prior spikes).
- Docs: MS Learn (WinUI templates, MVVM Toolkit tutorial, AppNotifications quickstart, settings guidelines, `dotnet new` WinUI templates Apr–May 2026 highlights).
