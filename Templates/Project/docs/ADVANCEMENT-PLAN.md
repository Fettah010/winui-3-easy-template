# Advancement Plan - template quality, security, performance, UX

Goal: make DevTem the obvious WinUI 3 starting point for shipping desktop
apps - a scaffold that starts clean (see the Formixa cleanup pass,
0.0.27-beta) AND stays safe, fast, and accessible as the app grows.
Every item below is grounded in repo evidence (file:line) or a named peer
template. Work it phase by phase; check a box only in the same commit as
the work (house rule from docs/WORKFLOW.md). Delete this file when all
phases ship (precedent: DIAGNOSTICS/FLEXIBILITY/DISTRIBUTION plans);
history stays in CHANGELOG.md + docs/DECISIONS.md.

Peer sources: microsoft/TemplateStudio (WinUI: ActivationService,
PageService, ShellPage, feature/page/service/testing templates,
AppNotification, Settings Storage), Uno Platform `unoapp` (IHost +
Microsoft.Extensions, presets Blank/Recommended/Customize,
.resw localization, auth option), Avalonia `avalonia.mvvm`
(compiled bindings by default, `--mvvm` toolkit choice,
`--remove-view-locator` trimming flag, `-f`/`-av` version params),
community AvaloniaProject (ViewModel-first navigation via DI,
`ScreenPage` lifecycle, AOT-safe view location). DevTem's answers
differ on purpose where noted - divergence is documented, not accidental.

Verification tiers come from docs/WORKFLOW.md (Fast / Matrix / Pack /
Live UI / Workflow). Anything touching `Templates/**` needs
`test-mirror-parity.ps1` + the scaffold matrix spots below.

---

## Phase A - Trust and security (do first; a template ships its threat model)

A template's security bugs replicate into every scaffolded app. Ordered by
severity as found 2026-09-22.

- [x] **A1. Verify `basic`-mode downloads before launching them.**
  `Services/BasicGithubUpdateService.cs` downloads a Setup `.exe` over
  HTTPS and runs it via `Process.Start` (`ApplyPendingUpdateAndRestart`)
  with no integrity check, and the download races no timeout
  (`Http.GetAsync(pending.DownloadUrl, CancellationToken.None)`).
  Velopack verifies its own payloads; `basic` is raw. Ship: SHA-256
  checksum file alongside the asset (release-workflow change, see A6) +
  verify-before-launch + download timeout + progress-cancel. Tests:
  tampered-byte fails closed, timeout degrades to cancel. (Fast + one
  scaffold-matrix `basic` combo.)
- [x] **A2. Cap settings-backup imports.**
  `Services/SettingsBackupService.cs` `ImportFromFile` parses arbitrary
  user files with no size cap (multi-GB file = OOM) and no schema
  version. Ship: length cap (e.g. 1 MB, settings are ~1 KB) + top-level
  `"version"` field (ignore unknown future keys - already the rule;
  reject unknown MAJOR). Tests: oversized + future-version fixtures.
- [x] **A3. Prove protocol/registry writes are hijack-safe.**
  `Services/ProtocolService.cs` parsing is pure + tested (good), and
  `Services/AutoStartService.cs` quotes the exe path (good). Missing:
  a test pinning the quoted `HKCU\...\Run` value with a spaced path,
  and the same quoting proof for the `HKCU\Software\Classes\<scheme>`
  command value in `Scripts/register-protocol.ps1` +
  `ProtocolService.EnsureRegistered`. Add both tests; never build a
  command line by concatenating an unquoted path again.
- [x] **A4. Lock in secret hygiene with a test.**
  Today this holds by inspection only: `CrashReportingService` never logs
  the DSN value (presence/absence + environment only), and the export
  bundle redacts settings. Ship: extend `Tests/Services/RepoHygieneTests.cs`
  (added 0.0.27-beta) with a source scan asserting no `AppLog.*(...DSN...)`,
  `...Token...`, `...Password...`, `...ApiKey...` call sites outside the
  redaction seam, plus a test that `UpdateFeedUrl` with an embedded SAS
  never reaches log output. (Peers: Uno keeps secrets in
  appsettings/user-secrets, never in code - our `DEVTEM_*` env layer is
  the equivalent; the test is what makes it stick.)
- [x] **A5. Audit Sentry PII defaults.**
  `Services/SentryCrashReporter.cs` is DSN-gated and off by default
  (good). Verify and test: default `SendDefaultPii = false`,
  breadcrumbs carry no file paths with usernames (or scrub `%USERNAME%`
  - placeholder), log-file attachments excluded from envelopes unless
  the user opts in. Document the PII posture in
  `docs/feature-guides/` (crash) so scaffold owners can answer the
  question without reading SDK docs.
- [x] **A6. Give `basic`-mode scaffolds a release path.**
  `Templates/Project/.template.config/template.json` excludes
  `release.yml` + `build-and-release.ps1` for `updates != 'velopack'`,
  so `basic` scaffolds have NO release workflow - yet `basic` NEEDS one
  (attach the Setup `.exe` + checksum file per release; feeds A1).
  Ship: a slim `release-basic.yml` (tag -> build Setup.exe -> attach
  asset + `.sha256`) or a documented manual flow + CI check that the
  checksum exists. (Peers: Template Studio ships per-feature release
  notes; our FEATURES.md should link the basic flow.)
- [x] **A7. Validate scaffold-time identity that the engine cannot.**
  `template.json` `constraints` only cover the host, so a bad `--scheme`
  (uppercase, missing `://`) or `--publisher` placeholder scaffolds fine
  and breaks the manifest/MSIX later (same class as the fixed
  distribution-combo problem, now an MSBuild error). Ship:
  `Scripts/init-template.ps1 -Validate` (validates current identity:
  scheme shape, repo shape, publisher non-placeholder, URLs reachable
  HEAD-check optional) + call it from `test-templates.ps1` per scaffold
  + document in the scaffold README first-steps.
- [x] **A8. Dependency-currency policy.**
  `.github/dependabot.yml` (nuget, weekly, limit 5) exists - confirm PRs
  actually flow and who merges them; add Velopack/WindowsAppSDK major
  bumps to the release checklist (major SDK bumps change retail
  behavior: notifications, packaging, trim). Record the policy in
  `docs/WORKFLOW.md` (one bullet, not a process).

---

## Phase B - UX, accessibility, and UI consistency

Findings from reading every page/shell/dialog 2026-09-22. Nothing here
is broken; everything here is what separates "works" from "polished".

- [x] **B1. Unify the responsive-layout mechanism.**
  `Pages/SettingsPage.xaml` drives padding from `VisualStateManager`
  + `AdaptiveTrigger`, while Home/About/Diagnostics drive everything
  from code-behind (`UpdateResponsiveLayout`, because VSM setters cannot
  retarget Grid rows/columns). Two mechanisms = two bug surfaces; a new
  page author copies the wrong one. Decide: code-behind everywhere
  (recommend - it already handles the hard cases) and convert Settings;
  delete the VSM block; note the decision in TEMPLATE-GUIDE section 2.
- [x] **B2. Keyboard support.**
  Zero `KeyboardAccelerators` repo-wide. Ship at minimum: Back
  (Alt+Left / BrowserBack -> `NavigationService.GoBack`), Ctrl+, ->
  Settings (Windows convention), Escape dismisses the update popup
  flow. Every new nav item from `add-page.ps1` should get no accelerator
  by default (avoid collisions) - document the reservation list.
  (Peers: Template Studio MenuBar project type exists for exactly this
  crowding problem; we stay rail-first, accelerators fill the gap.)
- [x] **B3. Screen-reader announcements for status.**
  Zero `AutomationProperties.LiveSetting` repo-wide: update-check
  results, toast cards, and the Diagnostics tail are silent to Narrator.
  Ship: `LiveSetting="Polite"` on the in-app toast host + the Settings
  update-status card; assertive only for failures. Verify with Narrator
  (Live UI tier), not just automation IDs.
- [x] **B4. Compile-time bindings in item templates.**
  Repo XAML is 58 `x:Bind` vs 4 `{Binding}`; the 4 live in
  `Pages/DiagnosticsPage.xaml` log-line `DataTemplate`s (lines ~302-307).
  Add `x:DataType` so mistyped paths fail the build (same class as the
  `Symbol="X"` runtime crash we now audit). Do the same in the
  `devtem-page` item template's list content.
- [ ] **B5. High-contrast + text-scaling pass.**
  Blocker 2026-09-22: static half shipped (fixed-height/hardcoded-color
  audit found no defects; `TextScalingAuditTests` pins the invariants).
  The screenshot matrix (200%, all four HC themes, EN/ES, 900px/1920px)
  needs a Live-UI desktop and stays manual.
  Fixed heights abound (48px title bar, rail metrics, 22px status ring,
  `MaxWidth` caps). Ship a measured pass: 200% text scaling + all four
  high-contrast themes, screenshots EN/ES at 900px and 1920px (the
  existing ABA discipline), filed as the layout proof in the PR.
  Known risks: custom title-bar content clipping, drawer-over-Mica
  background (open note since 0.0.26-beta), tray-button hit target.
- [x] **B6. More item-template shapes.**
  Peers ship ListDetails/ContentGrid/DataGrid/Settings page templates;
  we ship one generic page. Add `devtem-listpage` (list/details with
  selection + empty state) and `devtem-settingspage-section` guidance?
  Cheaper first step: one `devtem-list-details` item template reusing
  the add-page wiring (route + VM + tests + strings). Each new shape
  extends `add-page.ps1` with `-Kind page|list` and the matrix.
- [x] **B7. Dialog-button language consistency.**
  Audit every `ContentDialog` + `UpdatePopup` button row: Primary =
  verb, Close = "Close/Cancel/Later" per state, never two verbs that
  both dismiss. Codify the rule in TEMPLATE-GUIDE (one paragraph) and
  cover the FirstRun/whats-new/update trio with a headless content test
  (titles + button labels per language, like `LocalizationCoverageTests`).
- [x] **B8. FirstRun/whats-new UX.**
  A version bump triggers the modal whats-new (harness-dismissed in
  tests; real users hit it). Ship: whats-new shows at most once per
  version, "What's new" entry in About for recall, and release notes
  capped in length with a "full changelog" link (long notes in a modal
  are the current behavior). Respect quiet hours? No (decided: security
  updates prompt regardless) - keep, just link the decision.
- [x] **B9. Empty/error/loading states audit.**
  Diagnostics has them (`DiagnosticsNoLogs`, `DiagnosticsNoMatch` -
  good). Extend the pattern: Home offline/error card states, Settings
  check-failure retention (keep last-good status, don't blank it),
  SetupWizard validation messages per step. One checklist in the plan
  PR, screenshots per state.

---

## Phase C - Architecture for template consumers

Bigger refactors; each must land with its tests + guide updates + matrix.
Default stance: small, stable seams over rewrites (house precedent).

- [x] **C1. Constructor injection for pages (retire ServiceLocator-at-click).**
  Pages resolve VMs via `ServiceLocator.GetRequiredService<T>()`
  (service-locator pattern; peers inject constructors). WinUI
  instantiates pages via `Activator`, so ship a `PageFactory`
  (route -> factory registered in `ServiceLocator`, `NavigationService`
  resolves through it) and migrate pages one by one; VMs keep
  `[ObservableProperty]`/`[RelayCommand]`. The DI-gate test
  (`ServiceLocatorTests.ViewModels_ResolveTransient`) becomes a
  factory-resolution test. Phase across two betas; never a flag day.
  Shipped 2026-09-22 in one pass (the planned second beta proved vacuous:
  Home/About carry no view model, so no VM-bearing page remains on the
  locator). Frame journal retired; per-page cache honors
  `NavigationCacheMode`; item templates/add-page emit factories.
  Pages resolve VMs via `ServiceLocator.GetRequiredService<T>()`
  (service-locator pattern; peers inject constructors). WinUI
  instantiates pages via `Activator`, so ship a `PageFactory`
  (route -> factory registered in `ServiceLocator`, `NavigationService`
  resolves through it) and migrate pages one by one; VMs keep
  `[ObservableProperty]`/`[RelayCommand]`. The DI-gate test
  (`ServiceLocatorTests.ViewModels_ResolveTransient`) becomes a
  factory-resolution test. Phase across two betas; never a flag day.
- [x] **C2. Data-driven navigation registry (finishes pain-log #15).**
  `MainWindow.xaml` + `MainWindow.xaml.cs` + `add-page.ps1` currently
  share the nav contract by convention (Tag == route, selection sync).
  Ship `Services/NavigationRegistry.cs`: single list of
  (route, loc-key, Symbol/FontIcon, order, footer?) that XAML binds to
  (ItemsSource) and `add-page.ps1` appends to. Restyles then cannot
  break the script's anchor (there is no anchor). Keep the
  `<devtem:nav-items>` region as the documented fallback for exotic
  shells.
  Shipped 2026-09-22 with one documented divergence: items are built in
  code from the list (not `ItemsSource` binding — `x:Bind` cannot reach
  statics, and per-item Symbol/glyph icons need selectors); the single
  list is the goal and it holds. `add-page.ps1` never touches XAML.
- [x] **C3. Revisit IHost once, with measurements, then stop.**
  Peers (Uno Recommended, community Host templates) standardize on
  `Microsoft.Extensions.Hosting` (DI + logging + configuration +
  lifetime). We deliberately use a bare `ServiceCollection` (startup
  cost - see `ServiceLocator` docs). Ship a measured spike: cold-start
  delta of `Host.CreateDefaultBuilder` vs current on the reference
  machine, published in DECISIONS either way. If adopted: hosted
  services own the background loop + DB init; if not: the spike is the
  permanent answer and the question is closed.
  Spiked 2026-09-22: cold delta ~110ms (~2ms warm), verdict DECLINE
  (see DECISIONS). Question closed.
- [x] **C4. Settings as validated options.**
  `SettingsService` is a bespoke JSON store (correct for unpackaged -
  `ApplicationData.LocalSettings` never persists there). Add a thin
  validated-options layer: range/default/schema-version per key,
  migration functions keyed by stored schema version (extends A2's
  `"version"` field to the live store), one test per migration. Do NOT
  pull `Microsoft.Extensions.Options` unless C3 adopts the host.
  Shipped 2026-09-22: `SettingsSchema` (version 1 + dev-channel
  migration, theme normalization on read/write, future stores
  untouched) wired at startup next to the channel migration.
- [x] **C5. Background-work abstraction.**
  `BackgroundUpdateService` owns a bespoke periodic loop (check
  interval, metered guard, ask-mode). Generalize to a tiny
  `IBackgroundTask` registry (interval, network requirement,
  single-flight + per-task CTS - the shared-slot lesson from the
  Formixa log) with the update check as task #1. Future tasks (feed
  sync, cleanup) plug in without new timers.
  Shipped 2026-09-22: `BackgroundTaskRunner` (registry, due-sweep,
  single-flight slots, per-task cancel, offline gate) + the update
  check as task #1; the service loop delegates with identical timing
  semantics; per-mode excludes extended to the new files.
- [x] **C6. Evaluate an auth template behind a flag.**
  Uno ships auth as a template option; we have none. This is a
  deliberate deferral candidate (identity providers vary; MSAL adds
  packaging/entitlement questions). Ship the evaluation only: MSAL +
  broker vs embedded, token-cache location per distribution, what a
  `--auth` flag would exclude/include. No code until a real consumer
  asks (same rule as the deferred MVVM-toolkit choice).
  Evaluated 2026-09-22, verdict DEFER (see DECISIONS). No code.

---

## Phase D - Performance budgets with teeth

`Services/StartupBudgets.cs` + `measure-publish-weight.ps1` currently
warn (decided: fail after the first green baseline). Time to arm them.

- [x] **D1. Arm the budgets.**
  Flip `StartupBudgets.Check` and `-FailOnJump` to failing on breach
  with the baselines committed in-repo; CI (main-push) runs both.
  First breach gets triaged, not muted (mute = new baseline + reason
  in DECISIONS).
  Shipped 2026-09-22: `StartupBudgets.Report` logs breaches as errors
  at launch (wired post-window-phase) + budget-shape anti-mute test;
  CI `weight` job runs `-FailOnJump` on main push; local gate proven
  green (+0.1% vs baseline).
- [x] **D2. Publish-weight diet, measured.**
  `docs/publish-weight-baseline.json` exists - add the trend to the
  release checklist (delta per release in CHANGELOG). Evaluate: unused
  Toolkit packages per flag combo (matrix already builds all combos -
  assert per-combo weight ceilings), `PublishTrimmed` analysis for the
  non-WinUI assemblies we own, icon/asset compression (11 PNG variants
  + 3 ICOs today; all small - verify, don't assume).
  Shipped 2026-09-22: dead `Animations`/`Helpers` refs removed
  (267.8MB vs 268.2MB baseline, -0.2% — transitives remain via
  Controls); assets verified small (largest 128KB); trend rule in
  WORKFLOW. `PublishTrimmed` declined (WinUI-unsupported, like AOT);
  per-combo ceilings deferred to nightly/tag (21 publishes ≈ 84min,
  over the 30min matrix budget) — see DECISIONS.
- [x] **D3. Startup path audit.**
  Trace cold start end-to-end (Serilog init, DI build, first frame,
  deferred queue): assert P0-4 holds (ctor = chrome + navigation only),
  move anything that crept into the ctor back past the first frame.
  Record the flame numbers in STATE; regressions trip D1.
  Shipped 2026-09-23: P0-4 holds (only creep: instance-listener thread,
  moved to deferred); flame in STATE (cold 1451/1715/1964, warm
  ~300/~320/~500); splash re-baselined 800 → 1600 with reason.
- [x] **D4. List/log throughput guards.**
  Live-event cap (500) exists; extend the pattern: file-tail read caps
  + background parsing (never parse multi-MB logs on the UI thread -
  verify `DiagnosticsService` tail path), DB query time guards, image
  decode sizes (`Logo.png` 76KB is fine; user content is not - decode
  to container size, the Formixa lesson).
  Shipped 2026-09-22: export bundle I/O moved off the UI thread (was
  8MB read + scrub + zip inline); DB 30s command timeout + 2s slow
  tripwire on all three query paths; tail path verified already-capped
  (seek window + 200-line bound, pinned by a 2MB cap test). No
  user-image decode path exists (bundled assets only) — nothing to
  guard.
- [x] **D5. Splash honesty.**
  `SplashScreen.xaml` + `SplashLoadingServices/PreparingWindow` strings
  exist - wire them to real milestones (services ready, window ready)
  instead of static text, or drop the staged strings. A fake progress
  narrative is worse than none.
  Shipped 2026-09-22: audited already-honest (0.4/services,
  0.85/constructed — real completions, determinate bar, no filler
  animation) + added the 1.0 completion milestone at window-shown.

---

## Phase E - Template mechanics, CI, and docs

- [ ] **E1. Scaffold-matrix cost control.**
  Full matrix (~30 min, 21 combos) on every `Templates/**` change is
  correct but slow. Ship: fast subset on PR (allon + alloff + one msix
  + init-template scratch), full matrix nightly + on `templates-v*`
  tags. Document in WORKFLOW; CI enforces the subset per PR.
- [ ] **E2. Smoke-test reliability.**
  Suite is flaky on shared desktops (known fragile point) and needs a
  quiet desktop. Ship: retry-with-evidence (already partially),
  screenshot-on-failure artifacts in CI (already `TestResults/`),
  plus a documented "CI agent display" setup so UI tests run
  headless-reliably (or mark the suite `[Explicit]`-style nightly).
  Goal: no human re-runs to get green.
- [x] **E3. AGENTS/docs drift guards.**
  The 0.0.26-era stale version/branch/pages proved generated docs
  drift. Ship: a structural test asserting AGENTS.md has no
  hardcoded `0.0.x` version line (must say "see csproj"), the branch
  table is generic, and README layout lists only pages that exist
  (parse `Pages/*.xaml` vs the layout code fence). Cheap, permanent.
  Shipped 2026-09-23: `DocsDriftTests` (csproj pointer, app-version
  allowlist, generic branch table, page mentions incl. the scaffold
  fence, flag-relational); rotting `0.0.26-beta` example removed.
- [x] **E4. Docs information architecture.**
  TEMPLATE-GUIDE.md is ~500 lines serving first-run, daily, and
  maintainer audiences. Split: `docs/GETTING-STARTED.md` (scaffold ->
  first run, 15 min), TEMPLATE-GUIDE (daily workflows), `docs/MAINTAINERS.md`
  (parity, matrix, release, encoding rules). Keep the bootstrap order
  (AGENTS -> STATE -> WORKFLOW) intact; update all cross-links in the
  same commit.
  Shipped 2026-09-23: byte-exact section moves (guide 539 → 250
  daily-only lines); AGENTS/msix/init-template cross-links updated;
  new docs frozen for identity like the guide; Fast matrix green.
- [x] **E5. NuGet/VS presentation.**
  Package face (`icon.png`, README, tags) ships - verify the VS New
  Project dialog rendering (screenshots in the release PR), fill
  `PackageReleaseNotes`, confirm `dotnet new update` picks up 0.3.x.
  Record VS template-cache troubleshooting (already in guide §2c -
  verify it still matches VS 2026 behavior).
  Shipped 2026-09-23: `PackageReleaseNotes` filled; `devtem-list-details`
  actually added to the nupkg (B6 shipped it, packaging missed it —
  caught by pack-content audit); pack verified (277 entries, all three
  templates install + resolve, update check queries clean). VS dialog
  screenshots stay manual (release PR); cache remedy text stands.
- [x] **E6. Revisit the version-reset story.**
  Scaffolded apps inherit the template version; reset is one documented
  `bump-version` command (engine runs no post-actions - decided). Leave
  as is unless the engine gains post-actions; if it does, prefer a
  `version` symbol (default `0.0.1`) over scripts. Do NOT invent a
  custom post-scaffold runner (worse UX than flags - decided).
  Revisited 2026-09-23: engine unchanged (still no post-actions) —
  keep the command. See DECISIONS.
- [x] **E7. `basic`-mode + MSIX duality docs.**
  `distribution-dual.md` covers Velopack+Store. Extend the truth table:
  basic+MSIX? none+MSIX? appinstaller+portable (invalid - MSBuild
  errors, proven by `badupd` combo)? One matrix table in the guide,
  each cell linking its behavior. No code - comprehension.
  Shipped 2026-09-23: updates × distribution table in
  `distribution-dual.md` (valid cells link guides, invalid cite the
  MSBuild guards + proving combos).

---

## Phase F - Ecosystem parity (cherry-picked, not cloned)

DevTem is a curated production starter, not Template Studio with a
wizard. Adopt peer ideas only where they beat the current answer:

| Peer idea | Verdict |
| --- | --- |
| Template Studio page shapes (ListDetails, ContentGrid, DataGrid, WebView, Map, Camera) | Adopt ListDetails/DataGrid as item templates (B6); decline WebView/Map/Camera (app-domain, dependency + privacy weight) |
| Template Studio Settings Storage | Already have (`LocalSettingsStore` + backup) - no work |
| Template Studio suspend/resume + activation handlers | Evaluate: unpackaged suspend is limited; deep-link activation exists. One spike, then adopt-or-close like C3 |
| Template Studio testing templates | Already have (MSTest + FlaUI smoke) - no work |
| Uno presets (Blank/Recommended/Customize) | Docs presets exist; engine forbids `--profile` (decided). Consider `init-profile.ps1` interactive picker as sugar (E-priority low) |
| Uno .resw localization | Evaluate at 4th-language request only (decided P2-4); dictionary system stays below that line |
| Uno/auth option | C6 evaluation only |
| Avalonia `-f`/`-av` version params | Adopt the SHAPE: `--winappsdk`/`--net` params are tempting - decline for now (matrix multiplies per value; revisit past real demand) |
| Avalonia `--remove-view-locator` trimming flag | Adopt the instinct (D2), not the flag |
| Avalonia compiled bindings default | B4 (x:DataType) |
| Community ViewModel-first navigation | C2 registry is the pragmatic middle; full VM-first deferred |
| `PublishAot` | Declined for WinUI (platform-unsupported); track annually |

---

## Appendix 1 - Evidence index (where the plan came from)

- No-update-verification: `Services/BasicGithubUpdateService.cs:174-249`
  (download + launch, no hash, `CancellationToken.None`).
- Import without caps: `Services/SettingsBackupService.cs:17-120`.
- Protocol surface: `Services/ProtocolService.cs:1-120`
  (pure parsing good; registry quoting needs pinning).
- Secrets: `Services/CrashReportingService.cs:61-99` (clean today;
  A4 makes it structural), `Services/Configuration/ProductConfiguration.cs`
  (+ `DEVTEM_*` env layer).
- Release gap: `Templates/Project/.template.config/template.json`
  (`updates != 'velopack'` excludes `release.yml`).
- Identity validation gap: same `template.json` (no scheme/publisher
  validation possible in-engine; A7 covers it outside the engine).
- Keyboard/live-region gaps: 0 hits for `KeyboardAccelerator` /
  `LiveSetting` in `Pages|Controls|MainWindow` XAML.
- Binding gaps: 4 `{Binding}` in `Pages/DiagnosticsPage.xaml:302-307`
  (DataTemplate without `x:DataType`).
- Mechanism split: VSM in `Pages/SettingsPage.xaml:19-35` vs
  code-behind in Home/About/Diagnostics.
- Precedents: encoding doubling (0.0.27-beta notes),
  dormant-config drift (0.0.27-beta notes),
  `docs/DECISIONS.md` P2-4/C3-class deferrals.

## Appendix 2 - Explicitly out of scope (with reasons)

- VSIX distribution (NuGet IS the VS channel - decided, no revisit).
- MAUI/Avalonia ports (different dialects; Uno owns the WinUI-reuse story).
- WebView2 in the default scaffold (weight + privacy surface; app-domain).
- Polly (in-repo `ExponentialRetryHandler` suffices until circuit-breaking
  is needed - decided P2-2).
- MVVM-toolkit switch, test-framework switch, `packaging:msix` at
  scaffold time (deferred Phase-4 axes - decided, revisit on demand).
- Custom post-scaffold runners (worse UX than flags - decided).
- New template booleans without a consumer (flag discipline - decided P3-4;
  C6 stays an evaluation for this reason).

## Appendix 3 - How to work this plan

1. Pick the topmost open box in Phase A, then B, and so on. Phases
   overlap only when a box is blocked (note the blocker on the box).
2. Verification per WORKFLOW tiers; `Templates/**` changes always run
   parity + the E1 matrix subset (full matrix nightly/pre-tag).
3. Check the box in the same commit as the work; refresh `docs/STATE.md`
   at session end; append decisions to `docs/DECISIONS.md` (append-only).
4. Cut template MINOR only for new symbols/values, PATCH for content
   (version policy - decided); app follows patch/beta (Velopack must
   keep increasing).
