# Release Plan — v0.0.3-beta + DevTem.Templates 0.1.2

Goal: make this repo a better **template** and ship it. Replaces the
completed `docs/FLAUI-PLAN.md`, `docs/UPGRADE-ROADMAP.md` and root
`ROADMAP.md` (all done; deleted with this plan).

Status: `[ ]` todo · `[~]` in progress · `[x]` done.

## Version decisions (locked)

- Next beta is **`v0.0.3-beta`** — `v0.0.2-beta` and `v0.0.2` tags already
  exist and Velopack versions must keep increasing. There is no "0.0.2
  beta" left to cut.
- csproj: `<Version>0.0.3</Version>`, `<AssemblyVersion>/<FileVersion>`
  `0.0.3.0`, `<InformationalVersion>0.0.3-beta` (fresh installs track
  beta — intended for a beta).
- Template package: **`templates-v0.1.2`** (0.1.1 already published;
  unreleased content since: P7, smoke hardening, `schemeName`,
  `DiagnosticsPageViewModel` test).

## P0 — v0.0.3-beta release blockers

- [ ] Working tree committed (P7 + hardening + this plan + P0 items below).
- [x] Version bump 0.0.2 → 0.0.3 everywhere: `DevTemWinUi3.csproj`,
  `Templates/Project/__SafeName__.csproj`, both MSIX manifests,
  README examples.
- [x] `CHANGELOG.md` (new): 0.0.3-beta entry (P5/P6/P7, smoke hardening,
  protocol, backup, diagnostics).
- [x] `release.yml`: notes from `CHANGELOG.md` (stub fallback) + guard
  failing the run when the tag disagrees with the csproj (both verified
  locally: pass, reject, YAML parse).
- [x] Smoke harness dismisses the what's-new dialog (the bump triggers
  it; modal blocks nav clicks) + `GetChangelog()` refreshed to 0.0.3
  highlights (app + template).
- [ ] Tag `v0.0.3-beta`, push, verify CI release; move `beta` branch
  (runbook below — maintainer step, needs no code).
- [ ] README release examples use the new number (done with the bump).

Release runbook (maintainer, after green main):

```powershell
git tag v0.0.3-beta
git push origin v0.0.3-beta
# CI builds, packs, publishes to GitHub Releases (beta channel)
git branch -f beta v0.0.3-beta
git push origin beta --force
git tag templates-v0.1.2
git push origin templates-v0.1.2
# CI packs DevTem.Templates 0.1.2 and pushes to NuGet
```

## P1 — Template correctness (hand-mirror risk)

`Templates/Project/` is a hand-conditioned copy (`#if` flags,
`DevTemWinUi3Tray_` naming); nothing proves parity today.

- [ ] Mirror-parity check in CI: diff app vs template modulo an allowlist
  of conditioned regions. Note `templates.yml` does NOT trigger on app
  `Services/`/`Pages/`/`ViewModels/` — a mirror-less app change stays
  green. Widen paths or rely on the parity script.
- [ ] Assert rename-engine paths in the matrix: scaffolded manifest
  `Name="acme"`, scaffolded `ProtocolPrefix`, `init-template.ps1` on a
  scratch copy with leftover verification.
- [ ] `register-protocol.ps1`: `-WhatIf` support (`SupportsShouldProcess`,
  like `set-app-icon.ps1`) + a CI-safe invocation test.
- [ ] Include the nested `Templates/Project/Templates/Page/` copy in the
  parity check.
- [ ] `ui-tests.yml` paths miss `Services/`/`ViewModels/` — a runtime
  regression from a service change skips smoke tests in CI.

## P2 — VS Studio + NuGet polish

VS surface is sound (bool→checkbox, text→field, `icon` choice→dropdown,
all with `displayName`; no `postActions`; `schemeName` correctly shows
no UI). Remaining:

- [ ] Package `Description` still lists only tray/updates/SQLite/i18n —
  add deep-links, diagnostics, backup for 0.1.2.
- [ ] `templates-publish.yml` smoke scaffolds default flags only and
  builds without tests — pass `--scheme acme://` and run `dotnet test`.
- [ ] VS troubleshooting note: aggressive template caching (`dotnet new
  update`/reinstall when the new version doesn't appear).
- [ ] State the item-template VS limitation once where VS users look
  (Add → New Item never shows `devtem-page`; CLI only).
- [ ] Asset-mapping table in both TEMPLATE-GUIDEs (P5/P6 rule, never
  written): source → `app.ico` entries, `Logo*.png`, MSIX tiles.

## P3 — Docs & small debt

- [ ] `FirstRunService.GetChangelog()` lists ancient features — update per
  release or revert to a static welcome.
- [ ] Dependabot: add `github-actions` ecosystem (only NuGet covered).
- [ ] Scaffolded apps get unit tests but no smoke harness (`UI/` doesn't
  ship) — make it an explicit documented decision either way.
- [ ] Sentry DSN empty default: verified correct for a template
  (no leaks from scaffolds). Keep.

## Verification bars (every item)

- `dotnet build -c Debug -p:Platform=x64`: 0 warnings, 0 errors.
- `dotnet test Tests/`: green (currently 116/116).
- Template change: `Scripts/test-templates.ps1` MATRIX PASSED.
- Manifest change: `Scripts/build-msix.ps1 -DryRun` staging valid.
- UI-affecting change: FlaUI 4/4 + live screenshot.
- Workflow change: `pwsh` parse check + logic test with sample values
  (CI itself only runs on tag/push).
