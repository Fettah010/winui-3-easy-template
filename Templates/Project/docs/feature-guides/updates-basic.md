# Basic auto-updates (GitHub-releases checker)

This scaffold checks `https://api.github.com/repos/<owner>/<repo>/releases`
on startup and every 6 hours while running — no update SDK, no installer
framework. When a newer tag exists, the app downloads the attached Setup
`.exe` with a live progress bar, then launches it and exits.

## Release convention

Tags look like `v0.0.4` (stable) or `v0.0.4-beta` (prerelease). Attach the
installer as a **`.exe` asset** plus a **`{name}.exe.sha256` checksum file**
(hex digest, bare or "`hex  filename`" form) to the GitHub release — a
release without one is skipped with a log line. `stable` channel ignores
prereleases; `beta` includes them. Same-number stable supersedes prerelease
(`0.0.4` beats `0.0.4-beta`).

The app verifies the SHA-256 before staging and refuses to launch
unverified installers: a missing checksum fails the download with an
actionable error, a mismatch discards the file. Never ship the `.exe`
without its `.sha256` — one command does both (see
`Scripts/publish-basic.ps1`):

```powershell
# 1. Publish the installer + checksum, attach both to a draft release:
powershell -File Scripts\publish-basic.ps1 -Version 0.0.4 -Publish

# 2. Or attach by hand:
(Get-FileHash Setup.exe -Algorithm SHA256).Hash | Out-File Setup.exe.sha256 -NoNewline
gh release create v0.0.4 Setup.exe Setup.exe.sha256 --title "v0.0.4"
```

## Publishing without the release pipeline

`--updates basic` scaffolds no `release.yml` and no `vpk` scripts on
purpose — but it does scaffold `Scripts/publish-basic.ps1`, the single
source of truth for basic releases (publish single-file Setup.exe,
write + self-check the `.sha256`, optionally draft the GitHub release).
Any static host works, as long as the `.exe` AND its `.sha256` end up
attached to the GitHub release above.

## Limits (by design)

- No delta downloads, no background apply, no install detection: checks
  run even from a dev build (handy for testing the flow end to end).
- Unauthenticated GitHub API calls are rate-limited (60/hour/IP); set
  `GITHUB_TOKEN` in the environment for 5,000/hour.
- Want full install/update automation later? Re-scaffold with
  `--updates velopack` and copy your app code over.
