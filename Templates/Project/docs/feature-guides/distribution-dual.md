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
