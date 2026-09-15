# Dual-Track Distribution Plan — DevTem-WinUI 3

Status: **implemented, uncommitted.** Filed 2026-09-15, built same day
(D0–D3). Load-bearing remainder: the packaged proof in §5 (manual
install checklist + WACK) needs a kit machine.

Goal: one scaffolded app, two release routes from one tag — GitHub
(Velopack portable installer and/or AppInstaller-fed MSIX for
sideloading) **and** the Microsoft Store (same MSIX, Microsoft-signed).
No new updater dependency, no template-surface growth.

Definitions: *track* = a release route for one binary (GitHub-Velopack,
GitHub-AppInstaller, Store). *Dual-track* = shipping two or more tracks
from one commit at one version. The binary is already runtime-adaptive
(`AppInfo.IsPackaged` forks paths, autostart, protocol, wizard); this
plan extends that rule to the update UI.

## 0. Locked answers

1. **No new template symbols or values.** Dual-track is a release
   topology, not an engine: a new `updates` value or `distribution`
   value would fork `ServiceLocator` guards and the matrix for zero
   runtime benefit. Presets stay docs-only (prior decision stands).
2. **Runtime-aware external mode, not compile-time forks.** One new
   seam property; six call-site swaps; zero guard-manifest changes.
3. **One tag, both artifacts.** The csproj version already feeds both
   `vpk` (InformationalVersion) and MSIX quads (Version) — a single
   `vX` tag stays the release trigger; CI gains an MSIX leg.
4. **Store route needs no app-code branch.** Packaged runs already take
   packaged paths everywhere else; updates join them.

## 1. Research summary (verified 2026-09-15)

- **Velopack inside MSIX is inert by construction.** `VelopackApp.Run()`
  does per-launch housekeeping (old-package cleanup, auto-apply) driven
  by the Velopack locator, which finds no install layout inside an
  MSIX container; fast-exit hooks only fire when launched with Velopack
  CLI args, which never happens packaged
  (sources: `docs.velopack.io/integrating/overview`,
  `integrating/hooks`). `UpdateManager.IsInstalled` is documented as
  "true if currently installed *and able to* check/download" — false
  with no Velopack layout, so `BackgroundUpdateService` already skips
  (`!IsInstalled` → one log line) and Settings already shows its
  not-installed path. Residual risk is real but narrow (startup crash
  under package identity) — hence the mandatory packaged proof in §5.
- **MSIX updates stay inside one package family** (Name + Publisher);
  new versions must be higher (our `-Validate` already enforces both).
  Store-installed apps are Store-updated, period; sideloaded apps
  follow the `.appinstaller` feed (sources: MS `app-package-updates`,
  `update-settings`). Win11 sideloads signed packages by default;
  Win10 2004+ double-click works for signed packages.
- **Same package serves both MSIX routes.** One MSIX build can be
  submitted to the Store (re-signed by Microsoft) *and* hosted on
  GitHub with an `.appinstaller` feed (self-signed + trusted cert).
  Same family ⇒ switching routes needs reinstall (data backup note
  in the guide; no auto-migration — same rule as portable→packaged).
- **MSIX staging from a Velopack scaffold is WACK-safe by construction.**
  `vpk` artifacts (`Update.exe`, `.nupkg`s) are created at release
  time, never in `dotnet publish` output — staging carries Velopack
  *DLLs* only (inert managed code). WACK still re-proves it per release.
- **StoreContext in-app update API exists** (`GetAppAndOptionalStorePackageUpdatesAsync`,
  source: MS `store-developer-package-update`) but is explicitly
  deferred (§6): the status-card deep link covers the need today.

## 2. Architecture / seams

- **New seam:** `AppFeatures.IsExternallyManaged =>
  IsExternalUpdateMode || AppInfo.IsPackaged` (one property, no
  `#if`, no guard-manifest entry). Rationale: packaged-ness is a
  runtime fact; the scaffold flag stays the compile-time default.
- **Six call-site swaps** (mechanical, behavior-preserving unpackaged):
  `SettingsPageViewModel` ×3 (lines ~241/278/357),
  `UpdateCenterViewModel` ×3 (lines ~79/121/176). Packaged runs then
  take the existing slim-status surface (handler text + owner action)
  that P2 built for `appinstaller`/`store` — including on
  `portable + velopack` binaries packed as MSIX.
- **Untouched by design:** `ServiceLocator` registrations (Velopack
  stays compiled in — required unpackaged), `BackgroundUpdateService`
  (skip-on-`!IsInstalled` is already the correct packaged behavior;
  one log-line touch at most), `Program.cs` `VelopackApp.Run()`
  (proven inert — removing it would break unpackaged hooks),
  MSBuild distribution guard (scaffold-time pairs unchanged;
  `build-msix.ps1` is and stays symbol-agnostic), `template.json`.
- **Release flow (one tag):** `release.yml` gains an MSIX leg gated on
  a repo secret (`DEVTEM_MSIX_PUBLISHER`; job skips gracefully when
  absent so forks stay green): `build-msix -Validate` →
  `-StoreUpload` → attach `.msixupload` to the GitHub Release (doubles
  as the sideload feed host) → optional `submit-store.ps1` step
  (manual opt-in per release; certification takes days).
- **Secrets setup (maintainer, once):** Partner Center Publisher ID +
  existing `DEVTEM_STORE_*` app credentials. Documented in the guide;
  never committed (`.gitignore` already covers `.env`/`*.secret`).

## 3. Template surface (none new)

Docs-only dual preset (TEMPLATE-GUIDE + scaffold README + NuGet README):

```powershell
# Primary scaffold (GitHub Velopack + dual-capable binary)
dotnet new devtem-winui -n AcmeDesk --publisher "CN=Your-ID"
# Release: tag once → Velopack installer (GitHub) + .msixupload (Store/sideload)
```

`--publisher` (shipped) already pre-fills manifest + script default, so
the preset is one flag. `template-features.json`: no change (no new
files per combo; the dual guide is unconditional narrative docs).

## 4. Phases

- **D0 — Call-site audit (no behavior).** Confirm §2's six sites are
  exhaustive (re-grep `IsExternalUpdateMode` + `IUpdateService` +
  `UpdateService.Current`); confirm Diagnostics shows no engine state;
  record the packaged-proof checklist. Done when this section matches
  the tree.
- **D1 — Runtime seam + routing.** New `IsExternallyManaged` property,
  six swaps, `UpdateCenterViewModelTests`/`SettingsUpdateFlowTests`
  extended for the unpackaged-identical paths (packaged side is
  untestable headless — `IsPackaged` is false in the runner — so the
  property test pins the wiring and the packaged proof in §5 covers
  behavior). `basic` inherits it free through the same seam.
- **D2 — Release tooling.** `release.yml` MSIX leg (secret-gated skip),
  `publish-store.ps1` dual note, `updates-store.md` + new
  `distribution-dual.md` guide (version lockstep, reinstall-between-
  routes, data backup, which artifact goes where). Matrix: run
  `build-msix.ps1 -DryRun` on the `allon` scaffold (proves
  pack-from-Velopack-scaffold with no SDK).
- **D3 — Docs + version.** TEMPLATE-GUIDE preset, both READMEs, NuGet
  README, FEATURES manifest note (if any file list changes), app beta
  bump + CHANGELOG + CITATION + what's-new line, release prep (no
  tagging without request — standing rule).

## 5. Verification

Fast (`dotnet build` 0/0 + `dotnet test`) · parity OK (no guard changes;
new guide mirrored) · matrix subset then full (21 combos; `allon`
+ DryRun) · Workflow tier for `release.yml` edits (`pwsh` parse +
logic with sample values; never push a tag to test CI) · Pack DryRun.
Then the **packaged proof (manual, load-bearing):** install the MSIX
built from a `portable + velopack` scaffold (self-signed cert),
confirm launch with no Velopack crash, Settings + Update Center show
the slim Store/external status, protocol + autostart + first-run
(no wizard) behave, uninstall is clean · WACK pass per release.

## 6. Non-goals / deferred (explicit)

- StoreContext in-app update API (status deep link suffices; revisit
  past real demand).
- Portable→packaged data auto-migration (backup note only).
- winget submission, ARM64 matrix, per-track channels (one version,
  channel semantics stay Velopack-side).
- New `updates`/`distribution` values for dual (rejected in §0).

## 7. Versioning

App behavior ships ⇒ app beta bump (Velopack versions must keep
increasing) + CHANGELOG section (release-notes source) + CITATION.
Template content changes with no surface change ⇒ patch-level
`templates-v0.3.1` recommended (maintainer confirms; MINOR policy
covers new symbols/values only).
