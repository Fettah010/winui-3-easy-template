# Release notes template (per channel)

Copy into the CHANGELOG section for the version, then let `release.yml`
source the GitHub release notes from it. Delete the lines that do not apply;
never ship a section header with nothing under it.

```markdown
## [X.Y.Z-beta] - YYYY-MM-DD

One-line goal: what this beta proves.

### Added

- Feature: what ships, behind which flag, default on/off.

### Tracks (dual-release only)

- Velopack: installer + version attached to the GitHub release.
- Store: `.msixupload` attached (or "MSIX leg skipped: no Publisher secret").

### Weight

- Publish weight: N MB (+/-P% vs 268.2 MB baseline; driver).

### Proof

- Matrix: Fast/Full PASSED (or link the run).
- Installed runs: cell + date + outcome, or pending-kit with requirement.
- WACK: passed / triaged-fail + reason / pending-kit.
```

Stable releases use `## [X.Y.Z]` (no suffix) and move the `stable`
pointer instead of `beta`. Velopack versions must keep increasing — one
version per channel. Template package versions ride `templates-v*` tags
(`-p:Version` from the tag; the packaging fallback is never edited).
