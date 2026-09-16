# Velopack updates (`--updates velopack`, the default)

This scaffold includes the Velopack update service, release scripts, and the
GitHub Actions release workflow. Configure the repository and release
credentials before publishing; updates work only for installed builds.

The app always opens first: background checks never block launch
(auto-apply stays off). By default a detected update downloads silently
and the animated popup asks for restart; turn off
**Install updates automatically** in Settings to be asked before anything
downloads, with in-popup progress and Restart now / Later.

Lighter or no updating instead? Re-scaffold with `--updates basic`
(zero-dependency GitHub-releases checker, see
`updates-basic.md`) or `--updates none`.
