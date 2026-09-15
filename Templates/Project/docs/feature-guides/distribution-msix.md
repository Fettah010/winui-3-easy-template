# Packaged MSIX distribution

This scaffold chose `--distribution msix`: a packaged app with identity,
for Microsoft Store submission or sideloading with native updates.
The app ships as **one binary** that adapts at runtime (`AppInfo.IsPackaged`):
no separate build, no `#if` forks — packaged runs take the packaged path
for data, autostart, and protocol, everything else is shared.

## What changes when packaged

| Area | Portable | Packaged (this scaffold, installed) |
| --- | --- | --- |
| Data root | `%LocalAppData%\<Name>` | Package `LocalFolder` (`AppPaths.DataFolder`) |
| Settings | `settings.json` under data root | same file, packaged data root (fresh path — see below) |
| SQLite | `<data>\Data\app.db` | same, packaged data root (the install dir is read-only) |
| Logs | `<data>\Logs` | same, packaged data root |
| Autostart | HKCU Run key | Manifest `StartupTask`, via the Settings toggle (Windows may show a consent prompt; `DisabledByUser` can only change in Settings) |
| Protocol | HKCU self-registration on startup | Manifest protocol extension (registration skipped automatically) |
| Protocol launch | URI on the command line | URI from activation args (first launch and single-instance handoff both resolve it) |
| Singleton | named mutex | same mutex (works packaged, same user) |
| Updates | Velopack / basic / none | Store / `.appinstaller` / none (file-replace updaters cannot work — install dir is read-only) |
| Setup wizard | first-run wizard | none — Windows owns location and shortcuts (`--setup` warns and is ignored) |

## Fresh data path (read this)

The packaged data root differs from the portable one, so settings, the
database, and logs **do not migrate** from a portable install — a packaged
install starts clean. Moving portable data over manually is possible
(copy `%LocalAppData%\<Name>\settings.json` and `Data\app.db` into the
package `LocalFolder`), but there is no automatic migration (deferred —
see the Distribution Plan).

Side benefit for portable users: the database and logs now live under the
data root instead of next to the executable, so Velopack updates no longer
orphan them in a versioned folder.

## Packaging and signing

- Pack with `Scripts/build-msix.ps1` (publish → stage → `makeappx` →
  optional `signtool`). Needs the Windows SDK.
- **Store:** submit the `.msixupload` — the Store re-signs with a
  Microsoft certificate (free, no pfx, no token).
- **Sideload:** sign it yourself. Self-signed is fine for testing after
  installing the cert; real users need a trusted cert (expect
  SmartScreen friction otherwise).
- Before submitting: run the Windows App Certification Kit against the
  package (`appcert.exe test -appxpackagepath <pkg> -reportoutputpath out.xml`).

## Manifest reference

`Packaging/Msix/Package.appxmanifest` carries the identity (renamed with
the app), the `windows.protocol` extension (deep-link scheme), and the
`windows.startupTask` extension (registered **disabled** — users enable it
via the in-app toggle or Settings > Apps > Startup). `build-msix.ps1`
syncs Identity Name/Version/Publisher at pack time.
