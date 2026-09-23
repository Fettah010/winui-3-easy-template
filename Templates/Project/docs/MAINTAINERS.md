# Maintainers — template mechanics, matrix, releases

Audience: whoever cuts template releases and keeps the mirror green.
Consumers start at [`GETTING-STARTED.md`](GETTING-STARTED.md); daily
development lives in [`TEMPLATE-GUIDE.md`](TEMPLATE-GUIDE.md).
Bootstrap order stays `AGENTS.md` → `docs/STATE.md` → `docs/WORKFLOW.md`.

## Flag mechanics

How flags work (verified over all-on, all-off, and mixed scaffolds):

- **Files**: services, tests, release scripts and feature packages are
  excluded per flag (`sources.modifiers` in `template.json`).
- **Code**: `.cs` `#if (tray|setup|...)` blocks and `#if (updates == 'velopack')`-style
  value comparisons (the engine only
  evaluates markers in code files).
- **Packages**: `Build/Features.*.props` imported with `Exists` guards, so
  one static csproj serves every combo.
- **UI**: no markers in `.xaml` (the engine ignores them there) — instead
  `Services/AppFeatures.cs` (values rendered from the flags: distribution,
  setup, update mode) gates
  visibility, and `HomePage` fills its card grid from the visible cards.
- **Docs**: `README`/`AGENTS` rows carry `(feature)` qualifiers.

Engine gotchas learned while building it (do not regress):

- `dotnet new` CLI does NOT run script post-actions (IDE-only); there is no
  post-scaffold script — the design above needs none.
- Never author `$var`-heavy `.ps1` content relying on engine behavior; keep
  template scripts plain.
- The nested page template ships dormant
  (`Templates/Page/config.hold/template.json.hold`) so installing the project
  package does not register a stale global `devtem-page`; scaffolded apps
  rename it back per §2b.
- `init-template.ps1` still works in scaffolded apps for re-branding.

### Releasing template updates (upload + smooth updates)

Releases of the package are cut with `templates-v*` tags (CI packs +
pushes to NuGet); main pushes, nightlies, and tags run the full
scaffold matrix, PRs the Fast subset (`-Profile` — see
`docs/WORKFLOW.md` CI tiers).

The whole flow is automated; the maintainer only cuts a tag:

```powershell
git tag templates-v0.2.0
git push origin templates-v0.2.0
# CI (templates-publish.yml): dotnet pack -p:Version=0.2.0, push to NuGet
```

Prerequisites (one time, no secrets): on nuget.org go to your account →
Trusted Publishing → Create, and register a policy for package
`DevTem.Templates` from this repo + workflow `templates-publish.yml`.
The first push through that policy also reserves the package ID for you.
CI (`templates-publish.yml`) authenticates with OIDC — there is no API key
to create, store, or rotate. (Manual `dotnet nuget push` from your machine
still needs a classic API key; prefer tags so every release is traceable.)
Local dry run before tagging: `dotnet pack Packaging/DevTem.Templates -o
nupkgs`, then install the file and scaffold once.

Users update with one command (VS picks it up in its dialog too):

```powershell
dotnet new update --check-only        # what's new
dotnet new update                     # update all template packages
```

## 5. Releases

Bump `<Version>`/`<AssemblyVersion>`/`<FileVersion>` (keep in sync) plus
`<InformationalVersion>` (`-beta` suffix on beta releases so fresh installs
default to the beta channel). Commit, push, tag (`v0.0.1-beta`), push the tag
(CI builds/packs/uploads), then move the `beta`/`stable` pointer. Full flow
is in `AGENTS.md` ("Branches & releases").

### Code signing (do this before distributing)

Unsigned installers trip SmartScreen. Velopack signs every PE it packs
(your exe, its `Update.exe`, the setup) when you pass signtool args:

```powershell
vpk pack ... --signParams "/fd SHA256 /td SHA256 /f C:\certs\app.pfx /tr http://timestamp.digicert.com"
```

Notes from the Velopack docs (verified against `vpk pack -h`):

- Use **absolute paths** in the params; vpk may invoke signtool elsewhere.
- Get signing working on **one binary with signtool first**, then move it
  into `--signParams`. Quote-with-backslash anything containing spaces.
- Secrets belong in **env, not the command line**: every `vpk` option also
  reads `VPK_*` (e.g. `VPK_SIGN_PARAMS`). In CI, store the PFX base64-encoded
  in a GitHub secret, decode it at workflow time, pass password via secret.
- Alternatives: `--signTemplate "<cmd> {{file}}"` for custom signers, and
  `--azureTrustedSignFile` for Azure Trusted Signing.
- Test locally with a self-signed cert (`New-SelfSignedCertificate`),
  imported into Trusted People so your machine trusts it.
- Reputation is separate from validity: brand-new certs still SmartScreen-warn
  until trust builds; EV certs skip the queue, OV certs wait it out.

`Scripts/build-and-release.ps1` does not sign today — extend its `vpk pack`
call with `--signParams` once you hold a cert.

### MSIX packaging (sideload or Store)

`Packaging/Msix/Package.appxmanifest` + `Scripts/build-msix.ps1` produce an
MSIX from a Release publish (tile art is generated from `Assets/Logo.png`,
version is synced from the csproj). This **replaces the Velopack installer**,
not the app: under MSIX the in-app updater reports "not installed" by design
(Store/AppInstaller owns updates), and registry autostart does not apply —
the manifest registers a disabled StartupTask instead, and the app disables
its autostart toggle itself when packaged (`AppInfo.IsPackaged`).

```powershell
powershell -File Scripts/build-msix.ps1 -DryRun     # validate without SDK
powershell -File Scripts/build-msix.ps1 -Publisher "CN=Acme" -CertificatePath C:\certs\app.pfx -CertificatePassword "secret"
```

- `Publisher` MUST match the signing certificate subject.
- Unsigned packages validate the pipeline but cannot be installed — sign
  them, even self-signed for testing.
- CI (`msix.yml`) proves the pipeline on every change with an ephemeral
  self-signed cert and uploads the package + cert.

## Standing rules (pointers, not copies)

- Mirror: every app source change lands in `Templates/Project/` in the
  same commit (`Scripts/test-mirror-parity.ps1` proves it) — see
  `docs/WORKFLOW.md` definition of done.
- Matrix tiers: PR Fast subset, main/nightly/tag full (`-Profile`) —
  see `docs/WORKFLOW.md` CI tiers.
- Encoding: explicit UTF-8 IO on repo files, ASCII-only in tooling
  comments, non-ASCII in loc dictionaries only — see `docs/DECISIONS.md`.
  File encodings are load-bearing: `.ps1`/`.csproj`/`.xaml`/`.json` ship
  UTF-16LE, `.md` UTF-8. Editing tools rewrite bytes — after touching a
  UTF-16 file, convert it back (UTF-16LE CRLF) and re-prove with a probe
  scaffold; a UTF-8 `.ps1` fails `dotnet new` with exit 100 (seen 2026-09-23:
  `test-mirror-parity.ps1`). The `read` tool cannot open UTF-16 files
  ("binary") — use `Select-String` to inspect and patch scripts to edit.
- Releases: version policy (template MINOR for symbols, PATCH for
  content) and the runbook — see `docs/WORKFLOW.md`.
