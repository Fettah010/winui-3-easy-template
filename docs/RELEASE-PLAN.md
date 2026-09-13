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

- [x] Mirror-parity check in CI (`Scripts/test-mirror-parity.ps1`: file
  sets, verbatim hashes, csproj version agreement, nested page copy;
  self-tested pass + reject; `templates.yml` runs it first and triggers
  on app source paths too).
- [x] Assert rename-engine paths in the matrix (`Assert-ScaffoldIdentity`
  per combo: manifest `Name="<scheme>"`, `ProtocolPrefix`; fixed the
  `schemeName` generator this caught — `replaces` swaps the whole match,
  so the value now emits the full `Name="…"` attribute via `$1` group).
- [x] `init-template.ps1` scratch re-brand in the matrix (copy, rename,
  self-verification). Runs after combos in `test-templates.ps1`.
- [x] `register-protocol.ps1`: `-WhatIf` support + real probe
  register/unregister cycle locally (zero residue) + CI dry-run step in
  `templates.yml`. Fixed a latent `"$Scheme://"` PS 5.1 parse error found
  by the dry run.
- [x] `ui-tests.yml` paths widened (`Services/`, `ViewModels/`,
  `App.xaml*`, `Program.cs`, page code-behind).

## P2 — VS Studio + NuGet polish

VS surface is sound (bool→checkbox, text→field, `icon` choice→dropdown,
all with `displayName`; no `postActions`; `schemeName` correctly shows
no UI). Remaining:

- [x] Package `Description` lists deep-links, backup, diagnostics
  (verified in a locally packed 0.1.2 nupkg: install → custom-scheme
  scaffold → manifest assert → build 0/0 → 116/116).
- [x] `templates-publish.yml` smoke scaffolds with `--scheme acme://`,
  asserts the manifest, and runs `dotnet test` (same steps proven
  locally against the packed nupkg).
- [x] VS troubleshooting note (template cache: close, `dotnet new
  update`, reopen) + item-template CLI-only limit stated in both guides.
- [x] Asset-mapping table in both TEMPLATE-GUIDEs (source → outputs →
  consumers, incl. pack-time MSIX tiles).

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

## CI follow-up (v0.0.3-beta push)

- [x] `ui-tests / smoke` failed after ~2m: the test DLL was never built.
  Root cause: the sln mapped both test projects `Debug|x64 → Debug|Any
  CPU`, so the sln build dropped DLLs in `bin\Debug\…` while `dotnet test
  --no-build -p:Platform=x64` looks in `bin\x64\Debug\…`. Local runs
  never saw it (`dotnet test` builds the csproj directly). Fixed the
  x64 mappings (Debug + Release, app sln + template sln) and reproduced
  the exact CI sequence green (sln build → `--no-build` 4/4).
  Diagnostic improvements (exit code, app-log artifact) stay in place.
