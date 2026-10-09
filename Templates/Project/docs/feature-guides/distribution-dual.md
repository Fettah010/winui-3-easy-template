# Dual-track distribution (GitHub + Store)

This app ships one runtime-adaptive binary. One release tag can serve
**both** routes at the same version:

| Artifact | Route | Updates via |
| --- | --- | --- |
| Velopack installer (`Setup.exe`) | GitHub Releases (portable install) | Velopack feed, deltas, in-app restart prompt |
| `.msixupload` | Partner Center submission | Microsoft Store (Microsoft-signed) |
| `.msix` + `.appinstaller` | GitHub Releases or any HTTPS host | Windows (on-launch + background) |

Packaged runs of this binary automatically take the slim status surface
(Settings + Update Center report the Windows-owned flow); unpackaged
runs keep the full Velopack flow. No re-scaffold, no rebuild per route.

## Updates × distribution truth table

Not every combination exists — the engine cannot constrain parameters,
so invalid pairs fail the build with an MSBuild guard instead (proven by
the `badupd`/`badupd2` matrix combos). Each valid cell links its guide.

| `--updates` \ `--distribution` | `portable` (default) | `msix` |
| --- | --- | --- |
| `velopack` (default) | ✅ Setup.exe + feed, full in-app flow ([guide](updates-velopack.md)). Proof: scaffold matrix green (build 0/0 + tests, `allon`/`production` combos, every push). Installed-app update run (check→download→restart across two releases): pending-kit — needs an installed Setup.exe plus a second published release to check against. | ❌ Guard: `cannot be combined with --distribution msix` (ship the Dual tracks below instead) |
| `basic` | ✅ Setup.exe + `.sha256`, zero-dependency checker ([guide](updates-basic.md)). Proof: scaffold matrix green (`updbasic` combo). Installed-app checker run: pending-kit (same requirement as Velopack). | ❌ Guard: `cannot be combined with --distribution msix` |
| `none` | ✅ No update code at all (manual distribution). Proof: scaffold matrix green (`noupd`/`alloff` combos); there is no engine to run, so an installed run proves launch only. | ✅ Slim status surface, Windows owns updates. Proof: scaffold matrix green (`msixnone` combo); packaged install run: pending-kit. |
| `appinstaller` | ❌ Guard: `cannot be combined with --distribution portable` (needs package identity) | ✅ `.msix` + `.appinstaller`, Windows owns updates ([guide](updates-appinstaller.md)). Proof: scaffold matrix green (`msixapp` combo) + `build-msix.ps1 -DryRun` staging valid (publish + `.appinstaller` XML emission, no SDK). Feed-driven update on an installed package: pending-kit — needs a Publisher ID, signing cert, and HTTPS host. |
| `store` | ❌ Guard: `cannot be combined with --distribution portable` (needs package identity) | ✅ Partner Center submission ([guide](updates-store.md)). Proof: scaffold matrix green (`msixstore` combo) + `build-msix.ps1 -Validate -Publisher` pre-flight passed (see Store GA notes). Certification + submission receipt: pending-kit — needs a Partner Center account and app listing. |

The Dual profile (Production source + `--publisher`) is not a cell —
it is two artifacts from one source: the portable Velopack track and
the Store `.msixupload` at the same version, released together below.

## Releasing both tracks

1. Bump once (`Scripts/bump-version.ps1 -Version X`): the same version
   feeds `vpk` and the MSIX quad, so both tracks increase together.
2. Tag and push (`git tag vX && git push origin vX`): CI builds the
   Velopack release, then — when the `DEVTEM_MSIX_PUBLISHER` repo
   secret holds your Partner Center Publisher ID — packs the Store
   upload and attaches it to the same GitHub Release. Without the
   secret the MSIX leg skips gracefully (Velopack release unaffected).
3. Submit the `.msixupload` in Partner Center (or run the
   `store-submit` workflow / `Scripts/submit-store.ps1`).
4. Optional sideload feed: `build-msix.ps1 -AppInstaller -InstallUrl
   <public base URL>` next to the hosted `.msix`.

## Rules that bite

- **Same family or reinstall.** Store and sideload copies share Name +
  Publisher; switching routes on one machine needs uninstall first.
  Back up `%LocalAppData%\<Name>\settings.json` (portable) — packaged
  installs start with a fresh data path, nothing migrates.
- **Store owns Store installs.** A Store-installed copy never follows
  the `.appinstaller` feed, and sideloaded copies never check the
  Store. Version numbers may coincide; the owners stay separate.
- **One version, two owners.** Never ship different versions per track
  under one tag — diagnostics and the what's-new dialog assume one.

## Proof status (v0.5.0)

`matrix-proven` = scaffold builds 0/0 + tests green in CI (every push).
`pending-kit` = needs install rights, a Publisher ID / signing cert, or a
Partner Center account — tracked explicitly per cell above, never implied.
When a kit run completes, its date + version + outcome replace the pending
line; log forensics (`Logs/applog-*.log`) backs every installed-app claim.
