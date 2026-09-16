# Performance & Scalability Plan — DevTem WinUI 3

> **Status:** IMPLEMENTED — P0–P3 landed in the working tree on top of
> `main` @ `7916e03` (app `0.0.8-beta`), Fast tier green
> (`dotnet build -c Debug -p:Platform=x64` → 0 warnings / 0 errors;
> `dotnet test Tests/` → 267 passed + 4 skipped), mirror parity green.
> No commit/push/tag yet (held per maintainer rule until verified).
> Live-UI tier (FlaUI screenshots) and the full scaffold matrix are pending
> a quiet-desktop + time-boxed run; a single-combo scaffold proof is below.
> **Scope:** app `0.0.8-beta` (`DevTemWinUi3.csproj` `<Version>0.0.8` /
> `<InformationalVersion>0.0.8-beta`), `main` @ `7916e03`, tree green
> (`dotnet build -c Debug -p:Platform=x64` → 0 warnings / 0 errors).
> **Companion docs:** `AGENTS.md` (conventions) · `docs/STATE.md` (current
> facts) · `docs/WORKFLOW.md` (verification tiers + mirror rule) ·
> `docs/DECISIONS.md` (why things are the way they are).

Every work item below carries **evidence** (`file:line`), **acceptance
criteria**, a **verification tier** from `docs/WORKFLOW.md`, and a
**template-mirror note** (per the mirror rule, each app-source change lands
in `Templates/Project/` in the same commit, proven with
`Scripts/test-mirror-parity.ps1` + the scaffold matrix). Effort is in
T-shirt sizes; impact is expected user- or maintainer-visible gain.

---

## 1. Executive summary

The template is already performance-conscious: splash-first launch,
deferred DB/update work (`App.xaml.cs:72-74`), non-blocking Velopack apply
(`Program.cs:135-139`), a deliberately host-free DI container
(`Services/ServiceLocator.cs:10-12`), and built-in startup instrumentation
(`Program.StartupStopwatch`, `AppMetrics.RecordStartupPhase`,
`AppTrace` spans). The plan below protects those wins and attacks what
remains, in priority order:

| # | Lever | Expected gain | Phase |
|---|-------|---------------|-------|
| 1 | Remove fixed animation delays from the launch critical path | ~0.75–1.25 s faster time-to-interactive | P0 |
| 2 | Take settings I/O off the UI thread (cache + debounced writes) | Removes the #1 UI-thread stall source; faster launch + no per-keystroke jank | P0 |
| 3 | Make `UpdateService` init UI-thread-safe (`WaitAsync` + lazy async) | Eliminates dispatcher-block risk on every update-surface open | P0 |
| 4 | Cap logging cost in Release (level default, size quota, O(1) buffer) | Lower steady-state CPU/disk; bounded `%LocalAppData%` growth | P1 |
| 5 | Throttle UI-flooding paths (download progress, diagnostics filter, toasts) | Smooth progress + filter UX at any log/update volume | P1 |
| 6 | Give the data layer a real future (WAL, migrations, retryable init) | Unblocks 10× tables/services without rewrites | P1 |
| 7 | Modularize DI + harden HTTP (resilience, per-API clients) | Linear, safe growth to 30+ pages/services | P2 |
| 8 | Contain packaging + matrix + test-suite cost | Keeps install size, CI time, and smoke reliability flat as features grow | P2/P3 |
| 9 | Budgets, benchmarks, and CI guardrails | Regressions get caught by machines, not users | P3 |

**What this plan does NOT propose:** Native AOT / trimming, single-file
publish, or adopting the generic host — all are currently blocked by WinUI
3 / MVVM-generator / Serilog / SQLite / Velopack realities (see §8). They
are revisit gates, not work items.

---

## 2. Baseline & measurement method

Measure before changing. The repo already ships the instruments — use them.

### 2.1 Existing instruments (verified)

- `Program.StartupStopwatch` (`Program.cs:19`) — process-entry stopwatch.
- Phase timings: `"splash"` (`App.xaml.cs:37-38`), `"services"`
  (`App.xaml.cs:45-46`), `"window"` (`App.xaml.cs:60-61`) via
  `Services/Diagnostics/AppMetrics.cs`; spans via `AppTrace`
  (`App.xaml.cs:31,43,55`).
- Diagnostics page surfaces status, log files, buffered events
  (`Services/DiagnosticsService.cs:58-102`, `InMemoryLogSink` cap 1000).

### 2.2 Budgets (proposed SLOs — ratify before P0 lands)

| Metric | Budget | How to read |
|--------|--------|-------------|
| Cold start: process → splash pixels | ≤ 800 ms (dev box, Debug) | `splash` phase |
| Cold start: process → interactive main window | ≤ 2.5 s (Debug) / ≤ 2.0 s (Release) | `window` phase + manual smoke |
| Warm start → interactive | ≤ 1.2 s | stopwatch, second launch |
| Settings read (any `Get`) | ≤ 1 ms p99, never blocks UI > 1 frame | benchmark test |
| Log steady-state overhead | < 1 % CPU at 100 ev/s, disk ≤ 256 MB cap | file-size audit + profiler |
| Portable publish (win-x64, Release) | track + report; fail CI on > 10 % jump | `build-and-release.ps1` artifact size |
| Scaffold matrix | stays green; new flag must name its matrix additions | `Scripts/test-templates.ps1` |

### 2.3 Measurement cookbook

```powershell
# Build + headless tests (Fast tier — every change)
dotnet build DevTemWinUi3.csproj -c Debug -p:Platform=x64
dotnet test Tests/

# Timeboxed startup observation (quiet desktop, app closed first)
# Read Diagnostics page → startup phases; compare before/after per phase.

# Publish weight (Release, win-x64)
powershell -File Scripts/build-and-release.ps1 -WhatIf  # confirm steps first
```

---

## 3. Findings — startup critical path

Verified order of operations (`Program.cs:107-151` → `App.xaml.cs:26-75`):

1. `LoggingService.Initialize()` runs **synchronously on the main thread
   before the splash exists** (`Program.cs:109`; init does
   `Directory.CreateDirectory`, a settings read, Serilog pipeline setup —
   `Services/LoggingService.cs:67-136`).
2. Crash-report init, protocol-URI resolution, single-instance mutex
   (`Program.cs:110-129`) — cheap, correctly ordered.
3. Velopack explicitly non-blocking (`Program.cs:135-139`) — keep.
4. Splash `Activate()` = first paint (`App.xaml.cs:33-34`), but the splash
   itself plays a **600 ms entrance** (`SplashScreen.xaml.cs:181`) while
   services init on a background thread (`App.xaml.cs:79-86`) — good overlap.
5. **Fixed-delay chain after services are ready** (all verified):
   - `App.xaml.cs:105` — `Delay(100)` before `Activate()`
   - `SplashScreen.xaml.cs:225` — `Delay(300)` inside close animation
   - `Services/WindowChromeService.cs:95` — `Delay(350)` inside entrance animation
   - `Services/FirstRunDialogService.cs:21` — `Delay(500)` before welcome/what's-new
   - Splash entrance `Delay(600)` (`SplashScreen.xaml.cs:181`) overlaps init, so it is *not* additive — but the other four serialize to **≈ 750 ms post-`Activate()` (+ 500 ms on first-run/what's-new builds)** of pure waiting before the user can interact.
6. `MainWindow` ctor does heavyweight work inline (theme apply, tray init,
   toast registration, nav to home — `MainWindow.xaml.cs` wiring; tray
   creates a native window + icon on the UI thread,
   `Services/SystemTrayService.cs:46-79,175-247`).
7. Two live Mica surfaces coexist during the transition (splash + main —
   `SplashScreen.xaml.cs:27`, `Services/WindowChromeService.cs:36`).
8. DB init + update check + periodic loop are correctly deferred past the
   first frame (`App.xaml.cs:72-74`) — keep; P1 hardens the loop.

**Secondary startup taxes:** `SettingsService.Current.EnsureChannelForCurrentBuild()`
back on the UI thread (`App.xaml.cs:89`) performs several synchronous
`Get`/`Set` round-trips through the file-backed store; `UpdateService`'s
ctor reads `SettingsService.Current.Channel` (`Services/UpdateService.cs:47`),
adding another synchronous settings read to process start.

---

## 4. Findings — runtime performance

Ranked by impact × frequency. All line references verified against `main`.

### R1. `LocalSettingsStore`: sync file I/O + global lock on the UI thread (HIGHEST)

- Every `Get`/`Set`/`Remove` takes a process-global lock and ensures a
  synchronous load (`Services/LocalSettingsStore.cs:71-117,119-146`).
- Every `Set` rewrites the **entire** `settings.json` (create temp →
  serialize → move) synchronously (`LocalSettingsStore.cs:148-164`).
- Startup performs ~5–10 such round-trips (`LoggingService.cs:87`,
  `UpdateService.cs:47`, `App.xaml.cs:89`, theme/tray reads).
- Any future per-keystroke or per-toggle settings write (e.g. verbose
  toggle, sliders) pays a full-file rewrite on the dispatcher.

### R2. `UpdateService.Manager`: blocking `SemaphoreSlim.Wait()` (HIGH)

- `Services/UpdateService.cs:88` uses synchronous `_managerLock.Wait()`.
  `IsInstalled`, `CurrentVersion`, `HasPendingUpdate`,
  `PendingRestartVersion` all funnel through `Manager` (`UpdateService.cs:54-82`
  and below) — any UI-thread call (opening Update Center / Settings update
  section) blocks the dispatcher on Velopack construction + disk probes.

### R3. Logging defaults to `Debug` with no size quota (MEDIUM-HIGH)

- `_levelSwitch` starts at `Debug` (`Services/LoggingService.cs:47`,
  `ApplyLevel` `:151-155`); every event pays format + console + file +
  in-memory sink (`:93-109`). 14-day *count* retention (`:38`) with no byte
  cap — a verbose 10×-service app grows `%LocalAppData%` without bound.

### R4. `InMemoryLogSink.Count` is O(n) on the hot path (MEDIUM)

- `Count => _events.Count` on `ConcurrentQueue` snapshots the queue
  (`Services/Diagnostics/InMemoryLogSink.cs:27`); the overflow check runs
  **per emitted event** (`:36`), and diagnostics polls `Count` per refresh
  (`Services/DiagnosticsService.cs:79`).

### R5. UI-flooding paths (MEDIUM, user-visible jank)

- **Download progress:** Velopack progress callbacks marshal every percent
  to the UI thread (`UpdateCenterViewModel` / `SettingsPageViewModel`
  progress setters) → dispatcher flood + `ProgressBar` re-render storms.
- **Diagnostics filter:** `UpdateSourceTrigger=PropertyChanged` search boxes
  rebuild the filtered view per keystroke (`DiagnosticsPageViewModel`
  filter path; full-file tail read in `DiagnosticsService.cs:237-266`
  reads the entire file to keep 200 lines) → per-keystroke full scans +
  up to hundreds of `ObservableCollection.Add` → `ListView` re-realization.
- **Toasts:** each `NotificationService.Show()` holds an uncancellable
  multi-second delay with storyboards; bursts churn the host `StackPanel`.

### R6. Double Mica + heavyweight `MainWindow` ctor (MEDIUM)

- Two blur surfaces during splash→main transition (§3.7); ctor performs
  icon disk I/O, native tray-window creation, toast registration, and
  first navigation inline. Theme changes destroy + recreate the tray icon
  from disk (`Services/SystemTrayService.cs:147-163` via `RefreshThemeIcon`).

### R7. Localization broadcast + `WrapPanel` measure (LOW-MEDIUM)

- Language switch raises a single `PropertyChanged("Item[]")`
  (`Services/LocalizationService.cs:170-177`), re-evaluating **every**
  `{loc:Loc}` binding at once (~50+ across pages) — correct but bursty;
  fine today, worth batching awareness as page count grows.
- `Controls/WrapPanel` measures children with unbounded constraints on
  every layout pass — cheap at 2–4 buttons, thrashes on narrow-window
  resize storms combined with `AdaptiveTrigger`s.

---

## 5. Findings — scalability

### S1. DI: monolith `Initialize()` + `Current`-singleton dual ownership

- All registrations live in one method (`Services/ServiceLocator.cs:33-76`,
  app-service marker `:40-43`); no modules, no scopes, no keyed services.
  Shutdown disposes a provider that doesn't own the `Current` instances it
  registered (`:97-104`) — safe while services are stateless, hazardous the
  moment someone adds a scoped/disposable dependency (EF contexts, watchers).
- 10× services/pages ⇒ one ever-growing method + `RegisterRoute` additions
  in `MainWindow`/`NavigationService` with no composition boundaries.

### S2. HTTP: correct foundation, zero resilience

- `AddHttpClient<ApiService>` with timeout + UA (`ServiceLocator.cs:68-72`)
  is the right base (socket/DNS reuse). Gaps: one sample client, no
  per-backend typed clients, no `CancellationToken` propagation, no
  retry/timeout/circuit-breaker, and swallow-to-default error handling that
  erases cancel-vs-404-vs-500 distinctions for callers.

### S3. Data layer: single-connection SQLite, no migrations, init-once latch

- One held-open connection, one demo table, raw-SQL helpers
  (`Services/DatabaseService.cs`); `_initialized = true` is set **before**
  the `try` (`DatabaseService.cs:34`), so a failed first init never retries;
  no WAL config, no version/migration runner. Diagnostics resolves the DB
  path via `AppContext.BaseDirectory` (`DiagnosticsService.cs:119-130`),
  which disagrees with the real `AppPaths.DataFolder` path — packaged runs
  misreport DB size.
- Any second table/feature forces ad-hoc schema code with no upgrade story.

### S4. Packaging weight is locked in (no trim/AOT/single-file path)

- Always self-contained + WinAppSDK (`DevTemWinUi3.csproj:8-10`,
  `Scripts/build-and-release.ps1` publish flags) with Release-only
  ReadyToRun (`DevTemWinUi3.csproj:29`): fast JIT behavior at the cost of a
  150–250 MB-class `publish/` before Velopack deltas. Trim/AOT/single-file
  are explicitly off and **blocked** (XAML, MVVM generators with
  `MVVMTK0045` suppressed, Serilog reflection, SQLite native bundle,
  Velopack, Sentry). First-install size only shrinks via payload work, not
  linker work.

### S5. Template flag matrix: linear file cost, combinatorial verification cost

- Flags (`tray × distribution × updates × setup × database × http × health ×
  crash × localization × tests × logging`) are pruned by constraints, but
  each new `bool` still doubles theoretical combos; verification (parity
  script + scaffold matrix, ~30 min per `docs/WORKFLOW.md`) is the binding
  constraint. The current cost-control tactics work (guarded props imports,
  `AppFeatures` runtime bools keeping XAML conditional-free, unconditional
  seams like `IUpdateService`/`AppLog`) — new features must follow them.

### S6. Test suite: headless scales, FlaUI does not

- MSTest headless tests (fakes for `IUpdateService`/`IFilePickerService`/
  `HttpMessageHandler`) scale linearly and parallelize — push per-page
  logic here.
- FlaUI smoke is serial, exclusive (refuses to run if the app is open),
  mutates **real** `settings.json`, needs a quiet foreground desktop, and
  `Can_Navigate_All_Pages` is O(pages) — it will time out/flake at 10×
  pages. Keep it at launch + nav + theme + language.

### S7. Assets/resources

- Localization: +1 language = 1 file + registration; +1 key = 3 edits,
  enforced by coverage tests. Fine to ~10 languages; beyond needs
  `.resw`/tooling + plural support.
- Icons: dual-registered `app.ico` (explicit `Content` so it flows to
  Velopack publish — `DevTemWinUi3.csproj:32-43`) is load-bearing; document
  before touching.

---

## 6. The plan

Ordering principle: **measure → unblock the UI thread → bound steady-state
cost → enable safe growth → lock in with guardrails.** Each phase ends with
its verification line; no phase is "done" until the mirror + matrix
obligations (where applicable) are green.

### P0 — Launch + UI-thread freedom (do first; biggest user-visible win)

**P0-1. Remove fixed delays from the launch path; animate without awaiting.**
Problem: §3.5 — ~750 ms serialized waiting (+500 ms first-run).
Change: replace `await Task.Delay(N)` close/entrance sequencing
(`App.xaml.cs:105`, `SplashScreen.xaml.cs:225`,
`WindowChromeService.cs:95`, first-run `FirstRunDialogService.cs:21`,
splash enter `SplashScreen.xaml.cs:181`) with completion- or
composition-driven transitions (await the storyboard's `Completed` /
`CompositionTarget`, overlap `MainWindow.Activate()` with splash fade-out,
gate animation length behind reduced-motion + a "fast launch" setting).
Keep the *visual* language; remove the *blocking waits*.
Accept: `window` phase −500 ms or more on the reference dev box; no
perceived pop-in (screenshot comparison in summary per Live-UI tier).
Verify: Fast + Live UI tiers. Mirror: `Templates/Project/App.xaml.cs`,
`SplashScreen.xaml.cs`, `Services/{WindowChromeService,FirstRunDialogService}.cs`.

**P0-2. Async settings store: in-memory cache + debounced coalesced writes.**
Problem: R1 — sync whole-file rewrite per `Set` on the dispatcher.
Change: `LocalSettingsStore` keeps a loaded-once in-memory dictionary;
`Get` never touches disk after first load; `Set` updates memory
synchronously and schedules one coalesced atomic write (e.g. 250–500 ms
debounce, flush on suspend/exit). Preserve atomic temp+move writes, corrupt
backup, and `SetTestPath` isolation. Load the file once on a background
thread during splash (`App.xaml.cs:79-86` already runs there).
Accept: settings `Get` p99 ≤ 1 ms post-warmup (new benchmark test); 20 rapid
`Set`s ⇒ ≤ 2 disk writes (new headless test); launch `services` phase not
regressed.
Verify: Fast tier + new headless benchmark/coalescing tests. Mirror:
`Templates/Project/Services/LocalSettingsStore.cs`.

**P0-3. Async `UpdateService` init (`WaitAsync` + lazy-async singleton).**
Problem: R2 — `_managerLock.Wait()` (`UpdateService.cs:88`) can block the
dispatcher.
Change: `AsyncLazy<UpdateManager>` (or `SemaphoreSlim.WaitAsync` +
double-checked init); all public surface (`IsInstalled`,
`CurrentVersion`, check/download flows) becomes UI-thread-safe; surface an
explicit "initializing" state to view models instead of blocking.
Accept: Update Center opens with zero dispatcher stalls under a cold
Velopack cache (manual smoke + phase timing); new headless test pins
non-blocking init from a single-threaded `SynchronizationContext`.
Verify: Fast tier. Mirror: `Templates/Project/Services/UpdateService.cs`.

**P0-4. Slim the `MainWindow` ctor: defer tray/toast/icon work past first frame.**
Problem: §3.6 — native window + icon I/O + notification registration inline.
Change: construct chrome/navigation first; `await`-free deferral of
`SystemTrayService.Initialize`, toast registration, and non-critical icon
resolution to idle-after-first-render (same pattern as `App.xaml.cs:72-74`).
Cache both theme `HICON`s once at init instead of destroy+recreate per
theme flip (`SystemTrayService.cs:147-163`).
Accept: `window` phase improves; theme toggle performs no disk I/O
(verified by code inspection + manual smoke).
Verify: Fast + Live UI tiers. Mirror: `MainWindow.xaml.cs`,
`Services/SystemTrayService.cs` (+ template copies).

### P1 — Bound steady-state cost (reliability + resource ceiling)

**P1-1. Logging: Release default `Information`, byte-capped retention, O(1) buffer.**
Problem: R3–R4.
Change: default level `Information` in Release (keep `Debug` for Debug
builds; runtime verbose toggle unchanged — `LoggingService.cs:142-155`);
add a size quota alongside the 14-day count retention (e.g. ≤ 256 MB, delete
oldest); replace per-emit `ConcurrentQueue.Count` with an `Interlocked`
counter in `InMemoryLogSink` (`InMemoryLogSink.cs:27,36`).
Accept: new headless tests (counter correctness under concurrency; quota
deletes oldest first — fake file-system or temp-dir test); Diagnostics page
unchanged visually.
Verify: Fast tier. Mirror: `Services/LoggingService.cs`,
`Services/Diagnostics/InMemoryLogSink.cs`.

**P1-2. Throttle the three UI floods.**
Problem: R5.
Change: (a) coalesce download-progress callbacks (≥ 2–5 % delta or 100 ms
interval) before `SetOnUiThread`; (b) debounce diagnostics search 150–300 ms,
move tail-read off the UI thread, batch `ObservableCollection` updates
(range-add / `DeferRefresh` equivalent); cap `ReadLogTail` early-exit
(`DiagnosticsService.cs:237-266`); (c) make toast delays cancellable and
coalesce bursts (max N visible, queue rest).
Accept: smooth `ProgressBar` at simulated 100 callbacks/s (new headless
throttle test on the coalescer); filter keystroke storm (10 keys in 200 ms)
⇒ ≤ 2 recomputes; toast burst of 10 ⇒ bounded host children.
Verify: Fast tier (+ Live UI screenshots for progress/filter). Mirror:
`ViewModels/UpdateCenterViewModel.cs`, `ViewModels/SettingsPageViewModel.cs`
(progress paths), `ViewModels/DiagnosticsPageViewModel.cs`,
`Services/NotificationService.cs`.

**P1-3. Data layer v1: WAL + retryable init + migration runner + path fix.**
Problem: S3.
Change: enable WAL + busy-timeout; move `_initialized = true` behind success
(`DatabaseService.cs:34`) with limited retry; add a `schema_version` table +
ordered migration runner (idempotent, tested); fix diagnostics DB path to
`AppPaths.DataFolder` (`DiagnosticsService.cs:119-130`); document the
repository pattern for scaffolded apps (sample `IRepository<T>`, no forced
dependency).
Accept: kill-during-first-init ⇒ recovers on next launch (headless test);
v1→v2 migration test; packaged-path test asserts the real data folder.
Verify: Fast tier. Mirror: `Services/DatabaseService.cs`,
`Services/DatabaseInitializer.cs`, `Services/DiagnosticsService.cs`.

**P1-4. Harden the periodic update loop.**
Problem: `BackgroundUpdateService.RunPeriodicChecksAsync` holds a `Window`
for process life with no cancellation (`BackgroundUpdateService.cs:28-44`,
`PeriodicTimer` at `:34`); background download → modal prompt can surprise.
Change: tie loop lifetime to window `Closed` via `CancellationToken`;
skip download on metered networks (opt-out setting); prompt copy already
localized — keep; add "quiet hours respect tray-only mode" decision to
`docs/DECISIONS.md`.
Accept: closing the window cancels the loop (headless test with token);
no behavior change otherwise.
Verify: Fast tier. Mirror: `Services/BackgroundUpdateService.cs`.

### P2 — Scale the architecture (growth without rewrites)

**P2-1. Modular DI with composition boundaries.**
Problem: S1.
Change: split `ServiceLocator.Initialize()` into feature modules
(`services.AddAppCore()`, `AddUpdates()`, `AddDiagnostics()`, …) behind the
existing `<devtem:services>` marker; introduce scopes for
per-operation lifetimes; resolve the `Current` dual-ownership (container
owns *or* `Current` owns — document the rule in `DECISIONS.md`; prefer
container-owned singletons with `Current` as a compat accessor where the
test-suite needs it). No generic host (keep the measured startup win).
Accept: adding a sample 13th service touches exactly one module file +
tests; `Shutdown()` disposes owned instances (headless test).
Verify: Fast tier + full Matrix (DI shape ships to scaffolds). Mirror: all
touched DI files.

**P2-2. HTTP resilience kit.**
Problem: S2.
Change: typed-client-per-backend guidance + sample; `CancellationToken`
plumbing on all `ApiService` methods; opt-in retry/timeout/circuit-breaker
(recommend Polly, evaluate package weight first — if heavy, ship a minimal
exponential-backoff handler in-repo); typed results (`Result<T>` or
documented exceptions) replacing swallow-to-default on new APIs while
keeping never-throw at the template's own call sites.
Accept: flaky-handler test (2×500 then 200 ⇒ success); cancellation test;
docs page under `Templates/Project/docs/feature-guides/`.
Verify: Fast tier + Matrix. Mirror: `Services/ApiService.cs` + new
resilience files + feature guide.

**P2-3. Navigation + page-construction budget.**
Problem: unlisted growth tax — every page re-parses XAML on nav except
`HomePage` (`NavigationCacheMode`); per-page VM resolution + `INavigationAware`
callbacks grow with page count.
Change: define the caching policy explicitly (cache lightweight pages,
keep heavy pages transient — document in `DECISIONS.md`); add a nav-timing
counter to `AppMetrics` (already counts navigations — extend with
elapsed); lazy-load heavy page dependencies (defer Diagnostics log-tail
until scrolled-visible).
Accept: nav p95 per page reported on Diagnostics page; no regression vs
pre-change numbers.
Verify: Fast + Live UI tiers. Mirror: `Services/NavigationService.cs`,
page code-behinds as touched.

**P2-4. Localization at scale (defer until > 3 languages is real).**
Problem: S7 — key×language edits, no plurals.
Change (only when a 4th language is requested): evaluate `.resw`/tooling
vs current dictionaries; add plural-rule helper + test. Until then: no
change — the current system is cheaper.
Accept: decision recorded in `DECISIONS.md` either way.

### P3 — Lock in with guardrails (automation > discipline)

**P3-1. Startup-budget CI check.**
Add a headless/smoke assertion on the recorded `AppMetrics` phases (warn,
don't fail, for one release; then fail on regression > 15 %). Keeps P0 wins
from eroding silently.

**P3-2. Publish-weight tracking.**
Log win-x64 Release artifact size per release (or per `build-and-release`
run) into the release notes/body; fail CI on > 10 % unexplained jumps.
Pairs with P1-1's disk quota narrative.

**P3-3. Smoke-suite charter.**
Pin the FlaUI suite to launch + nav + theme + language (current scope);
route all new page coverage to headless tests. If `Can_Navigate_All_Pages`
exceeds its time budget, split by area using the existing
`DEVTEM_SMOKE_PAGE_NAV_ID` extension point rather than lengthening timeouts.

**P3-4. Template-flag discipline.**
New bools require: an `AppFeatures` gate (no XAML conditionals), a
`template-features.json` entry, a feature guide, parity + matrix proof in
the same PR. Prefer value-choices and unconditional veneers
(`CrashReportingService` pattern) over new `#if` forks.

---

## 7. Sequencing & rollout

```
P0-2 (settings) ─┐
P0-3 (updates) ──┼─→ P0-1 (delays) ─→ P0-4 (ctor) ─→ measure vs §2.2 budgets
P1-1 (logging) ──┘         │
P1-2 (floods) ─────────────┤
P1-3 (data) ───────────────┼─→ P1-4 (loop) ─→ P2-1 (DI) ─→ P2-2 (HTTP) ─→ P2-3 (nav)
P3 guardrails ─────────────┴─→ ratify budgets → enforce in CI
```

- Land P0 as one release train (each item separately reviewable, budgets
  checked at the end); P1 items are independently shippable.
- Every item updates the template mirror in the **same commit** and runs
  `Scripts/test-mirror-parity.ps1`; any `Templates/**` touch runs the
  scaffold matrix (`docs/WORKFLOW.md` Matrix tier).
- Record each landed item's before/after numbers in `docs/STATE.md` at
  session end (per `AGENTS.md`); roadmap checkboxes move with the work
  (per `docs/WORKFLOW.md`).

---

## 8. Explicitly out of scope (with revisit gates)

| Rejected / deferred | Why | Revisit when |
|---------------------|-----|--------------|
| Native AOT / trimming | Blocked: WinUI XAML, MVVM generators (`MVVMTK0045` suppressed in csproj), Serilog reflection, `SQLitePCLRaw` native bundle, Velopack, Sentry | WinUI + Velopack publish trimming guidance; prototype on a scratch scaffold first |
| Single-file publish | Breaks WinUI/MSIX tooling (already explicit in `build-and-release.ps1`) | Upstream tooling support lands |
| Generic host (`CreateDefaultBuilder`) | Measured seconds of startup cost for unused plumbing (`ServiceLocator.cs:10-12`) | A scoped-host benchmark shows < 100 ms cost |
| Removing never-throw guards | Template's core promise (see `SystemTrayService` precedent in `docs/WORKFLOW.md`) | Never — resilience kit (P2-2) adds *typed* errors on new APIs instead |
| `ApplicationData.LocalSettings` | Doesn't persist unpackaged (no `settings.dat`) — `LocalSettingsStore.cs:9-16` | OS behavior changes |
| Growing FlaUI beyond smoke | Serial, desktop-sensitive, O(pages) — §S6 | Dedicated UI-test agent + isolated test user |

---

## Appendix A — evidence index (verified on `main`)

| Claim | Location |
|-------|----------|
| Sync logging init before splash | `Program.cs:109`, `Services/LoggingService.cs:67-136` |
| Non-blocking Velopack apply (keep) | `Program.cs:135-139` |
| Splash activate / phases / deferred work | `App.xaml.cs:33-46,60-61,72-74` |
| Fixed delays: 100 / 300 / 350 / 500 / 600 ms | `App.xaml.cs:105`; `SplashScreen.xaml.cs:225`; `Services/WindowChromeService.cs:95`; `Services/FirstRunDialogService.cs:21`; `SplashScreen.xaml.cs:181` |
| Store: global lock, whole-file `Set` | `Services/LocalSettingsStore.cs:71-117,148-164` |
| Blocking update-manager init | `Services/UpdateService.cs:88` (+ ctor settings read `:47`) |
| Debug-by-default, 14-day count retention | `Services/LoggingService.cs:38,47,151-155` |
| O(n) in-memory buffer count | `Services/Diagnostics/InMemoryLogSink.cs:27,36` |
| Full-file log-tail read | `Services/DiagnosticsService.cs:237-266` (+ DB path drift `:119-130`) |
| Periodic loop holds `Window`, no cancel | `Services/BackgroundUpdateService.cs:28-44` |
| DI monolith + dual ownership | `Services/ServiceLocator.cs:33-76,97-104` |
| HTTP: factory OK, no resilience | `Services/ServiceLocator.cs:68-72` |
| DB: init-once latch before `try` | `Services/DatabaseService.cs:34` |
| Release-only R2R; self-contained; no trim/single-file | `DevTemWinUi3.csproj:29`; `:8-10`; `Scripts/build-and-release.ps1` publish flags |
| Icon dual-registration (load-bearing) | `DevTemWinUi3.csproj:32-43` |

## Appendix B — per-item verification cheat sheet

From `docs/WORKFLOW.md`: **Fast** = `dotnet build DevTemWinUi3.csproj -c
Debug -p:Platform=x64` (0/0) + `dotnet test Tests/` · **Matrix** =
`Scripts/test-templates.ps1` (~30 min, any `Templates/**` touch) ·
**Live UI** = `dotnet test UI/…` + screenshot (any XAML/nav/theme change) ·
**Docs-only** = no build needed. This plan file itself is docs-only; all
work items above name their tier inline.

---

## Appendix C — implementation record (2026-09-16, `main` @ `7916e03`)

All items landed in app sources + `Templates/Project/` mirror in the
working tree (uncommitted). Fast tier: build 0/0, tests 267 passed +
4 skipped (pre-existing skips). Parity: OK. Guard-manifest updates:
`MainWindow.xaml.cs` (early tray icon block moved into the deferred
method), `Services/LoggingService.cs` (one shared `DEBUG` guard behind
`IsDebugBuild`), `Tests/Services/ServiceLocatorTests.cs` (new module
tests). Full scaffold matrix + Live-UI screenshots pending (see
`docs/STATE.md`).

| Item | What shipped | Tests |
|------|--------------|-------|
| P0-1 | `Services/LaunchAnimations.cs` (scale + completion-await + fast-launch/reduced-motion gates); splash entrance fire-and-forget, close completes on animation; chrome entrance via helper; `App.TransitionToMainWindow` activates first, teardown in background; first-run delay → `Task.Yield`; `FastLaunch` setting | `LaunchAnimationsTests` (4) |
| P0-2 | `LocalSettingsStore`: load-once cache, debounced coalesced writes (300 ms, `WriteDebounce` test seam), `Flush()`/`PreloadShared()`/`FlushShared()`, `CompletedWrites`; splash preload; exit flush | `LocalSettingsStoreTests` +4 (coalescing, flush, Get benchmark, preload) |
| P0-3 | `UpdateService`: `WaitAsync` + double-checked lazy init off-thread, sync surface degrades (false/version/null), `IsInitialized`/`EnsureInitializedAsync`, lock-only `SetChannel`; `BasicGithubUpdateService` trivial seam | `UpdateServiceTests` +2 (init, UI-thread non-block) |
| P0-4 | `MainWindow.InitializeDeferredServices` (low-priority enqueue, inline fallback); tray icon handles cached once, flips I/O-free | manual smoke (Live UI pending) |
| P1-1 | Release default `Information` (`IsDebugBuild`/`DefaultMinimumLevel`); 256 MB `EnforceDirectoryQuota`; O(1) `InMemoryLogSink.Count` | quota ×2, default-levels, sink concurrency |
| P1-2 | `ProgressThrottler` (both VMs), diagnostics debounce + off-thread tail (`WaitForTailAsync`/`WaitForFilterAsync`), `ReadLogTail` 1 MB/256 KB early-exit, cancellable + bounded (`ToastPolicy`) toasts | throttler (5), keystroke-storm, large-tail, policy (2) |
| P1-3 | WAL + busy-timeout, retryable init (latch behind success), `schema_version` migration runner, diagnostics DB path → `AppPaths.DataFolder`, `Services/Data/IRepository.cs` + `InMemoryRepository<T>` | migrations, failed-init recovery, repository (4) |
| P1-4 | Loop tied to window `Closed` + caller token, metered-download skip (`DownloadOnMetered`), `EnsureInitializedAsync` first | cancel ×2, metered no-throw |
| P2-1 | `AddData/AddUpdates/AddPresence/AddCore/AddHttp` modules; ownership rule in `DECISIONS.md` | shutdown/re-init, module resolution |
| P2-2 | `ExponentialRetryHandler` (in-repo, no Polly), CT overloads, `ApiResult<T>` + `GetResultAsync`, guide in `docs/feature-guides/http.md` | flaky-then-ok, cap, cancel, 404 |
| P2-3 | `AppMetrics.RecordNavigation(tag, ms)` + histogram + diagnostics display; HomePage cached; policy in `DECISIONS.md` | nav-timing |
| P2-4 | Deferred (no 4th language): decision in `DECISIONS.md` | — |
| P3-1 | `Services/StartupBudgets.cs` + `Check()` (warn-first) | budgets (6) |
| P3-2 | `Scripts/measure-publish-weight.ps1` (warn default, `-FailOnJump` for CI) | script run (see STATE) |
| P3-3/4 | Smoke charter + flag discipline in `DECISIONS.md` | — |
