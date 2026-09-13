# DevTem template contract

This document is the compatibility contract for the `DevTem.Templates`
package and the two templates it contains:

- `devtem-winui` — a complete WinUI 3 project.
- `devtem-page` — a page, view model, test stub, and localization snippet.

The contract describes the current implementation. It is intentionally
conservative: a value is not considered configurable merely because it could
be changed in source code. New symbols and renamed symbols require a deliberate
compatibility decision.

## 1. Project template invocation

Install from the repository while developing the template, or install the
versioned `DevTem.Templates` NuGet package:

```powershell
dotnet new install .\Templates\Project
dotnet new devtem-winui -n AcmeDesk --displayName "Acme Desk" `
  --company "Acme" --repo "acme/desk" --scheme "acme://" `
  --tray true --updates true --database true --http true --attribution true
```

The project template uses `identity` `WinUI3.App.TrayUpdatesDb`, group identity
`WinUI3.App`, source name `DevTemWinUi3`, and short name `devtem-winui`.
`name`, `shortName`, `identity`, symbol names, and the package ID are public
compatibility identifiers. They must not be changed casually.

### Project symbols

| Symbol | Type/default | Current behavior |
|---|---|---|
| `safeName` | Generated from `name` | Sanitizes the project name for file names and generated identifiers. It replaces a leading non-ASCII-letter prefix with `_`, replaces other unsupported characters with `_`, and permits a leading underscore. |
| `displayName` | String, `My App` | Replaces `DevTem-WinUI 3` in generated display-facing text such as titles, toasts, and installer text. |
| `company` | String, `Contoso` | Replaces `Fettah` in generated package/publisher-facing text. |
| `repo` | String, `myorg/my-app` | Replaces the repository path and is used by release feeds, links, and CI. The current template expects an `org/name` value, not a full URL. |
| `scheme` | String, `myapp://` | Replaces the deep-link URI and must match a lowercase URI scheme followed by `://`. The generated manifest protocol name is derived from it. |
| `schemeName` | Generated from `scheme` | Renders the manifest `Name="..."` attribute. |
| `tray` | Boolean, `true` | Includes system-tray, autostart, desktop-toast, and related test files. |
| `updates` | Boolean, `true` | Includes Velopack update services, update tests, release scripts, and the release workflow. |
| `database` | Boolean, `true` | Includes SQLite persistence and deferred database initialization. |
| `http` | Boolean, `true` | Includes the typed HTTP client and HttpClientFactory registration. |
| `attribution` | Boolean, `true` | Includes `DevTemAttribution.cs`, which contains the one-line source attribution comment. It also reports the selected value in `docs/FEATURES.md`. |

The three feature booleans are rendered into `Services/AppFeatures.cs` as
compile-time-looking boolean literals. Feature-owned files are excluded by
`sources.modifiers`; feature package references are supplied by the
corresponding `Build/Features.*.props` file. The current template does not
provide independent logging, crash-reporting, localization, test, or
release-workflow switches.

### Defaults and required generated behavior

Every generated project is an unpackaged WinUI 3 application targeting
`net10.0-windows10.0.19041.0`, with Windows App SDK self-contained deployment,
nullable reference types, implicit usings, MVVM Toolkit, Serilog logging,
settings persistence, localization dictionaries for `en-US`, `es-ES`, and
`fr-FR`, Mica window chrome, navigation, diagnostics, and the About/Home/
Settings shell.

The generated application must:

1. Build with zero warnings and zero errors when the selected feature files and
   package references are present.
2. Use the selected safe project name in the project, solution, root
   namespace, assembly, tests, mutex/event names, and persisted settings path.
3. Use the selected display name for user-facing application text.
4. register the selected URI scheme through the existing protocol service.
5. Remove disabled feature-owned files and package imports rather than leaving
   dead references.
6. Keep all localization dictionaries structurally complete.

## 2. Identity and replacement rules

The current project template exposes only `name`, `displayName`, `company`,
`repo`, and `scheme` as identity inputs. These concepts are not yet fully
independent:

| Concept | Current source of truth | Current limitation |
|---|---|---|
| Safe project name | `name` → `safeName` | Sanitization is template-engine generated; the migration script has separate rules. |
| Display name | `displayName` | Replaces a literal token across generated content. |
| Root namespace | `name` → `safeName` replacement | Derived from the project name by design; not an independent template symbol. |
| Assembly name | `name` → `safeName` replacement | Derived from the project name by design; not an independent template symbol. |
| Company | `company` replacement | Uses the existing `Fettah` token. |
| Repository | `repo` replacement | Repository URL construction is convention-based. |
| URI scheme | `scheme` → `schemeName` | Template input is documented as lowercase `scheme://`; the migration script validates this shape. |
| App ID/settings folder | `AppMetadata` replacements | Derived from the safe name and not independently configurable. |

`Scripts/init-template.ps1` remains the migration/rebranding tool for an
already-generated repository. It rewrites a broader set of literals, renames
the project/test/solution files, verifies template leftovers, and deliberately
does not rewrite `TEMPLATE-GUIDE.md` or frozen archive material. It now uses
the same safe-name shape as the project template, validates GitHub repository
paths and URI schemes, and accepts an explicit `-Scheme` while defaulting to
`<safeName>://`.

## 3. Feature ownership and boundaries

| Feature | Files/packages owned today | Registration/UI/docs | Disabled result |
|---|---|---|---|
| Tray | `SystemTrayService`, `DesktopToastService`, `AutoStartService`, tray native helpers, tray tests | `ServiceLocator`, shell/window activation, settings, localized tray strings | Tray services/tests are removed; shell remains usable without tray. |
| Updates | `UpdateService`, `BackgroundUpdateService`, update abstractions/tests, `Features.Updates.props`, release/local upload scripts, release workflow | `Program`, `App`, `ServiceLocator`, settings/update UI and strings | Update services, package, scripts, and workflow are removed. |
| Database | `DatabaseService`, `DatabaseInitializer`, database tests, `Features.Database.props` | `App`, `ServiceLocator`, startup initialization, generated README/AGENTS, `docs/FEATURES.md` | SQLite services, package references, and the database guide are removed. |
| HTTP | `ApiService`, API tests, `Features.Http.props` | `ServiceLocator`, generated README/AGENTS, `docs/FEATURES.md` | HTTP service, package reference, and the HTTP guide are removed. |
| Logging | `LoggingService`, Serilog packages | `Program`, global exception paths, README | Always included; no switch exists. |
| Crash reporting | `CrashReportingService`, Sentry package | `Program`, `App`, DSN/environment/release metadata, README | Always included but inactive unless a DSN is supplied; no switch exists. |
| Localization | `LocalizationService`, `LocExtension`, three dictionaries, language tests | Shell/settings and all localized UI | Always included; no switch exists. |
| Attribution | `DevTemAttribution.cs` | No runtime registration | Attribution source file is removed only. |
| Tests | `Tests/**` | Project excludes tests from app compilation | Project template always includes the headless test project. |
| Release content | `.github/workflows/release.yml`, release scripts | Documentation and release runbook | Removed only with `updates`. |

The machine-readable ownership and dependency summary is
[`template-features.json`](template-features.json). Database and HTTP are
independent options; either may be enabled without the other.

## 4. Extension points and safe edits

### Supported extension points

- Register application services in `Services/ServiceLocator.cs`.
- Add routes in `MainWindow.xaml.cs` and navigation items in
  `MainWindow.xaml`.
- Add page strings to all three files under `Services/Localization/`.
- Add persisted preferences through `SettingsService` and
  `LocalSettingsStore`.
- Add pages with `devtem-page`, then use `Scripts/add-page.ps1` for the
  supported integration path.
- Configure repository-specific metadata through `Services/Helpers/AppMetadata.cs`.
- Configure deployment defaults through
  `Services/Configuration/ProductConfiguration.cs`; use `DEVTEM_*`
  environment variables for developer/CI overrides.
- Replace branding assets through `Scripts/set-app-icon.ps1` and the `Assets`
  files.

### Safe for normal application edits

Users may edit pages, view models, services, localization dictionaries,
settings cards, assets, tests, and release metadata after generation. They may
also remove optional feature code manually, but then they own the resulting
package, registration, and documentation cleanup.

### Generated or tool-managed files

The template engine owns files under `.template.config` and applies source
modifiers during generation. `init-template.ps1` may rewrite identity-bearing
content and rename project files. `add-page.ps1` may insert localization,
dependency-injection, route, and navigation entries. These tools are
deterministic only when their documented anchors and validation assumptions
remain intact; do not hand-edit those anchors without updating the scripts.

## 5. Manual steps after generation

`dotnet new` produces a buildable project, but the following are intentionally
consumer-owned steps:

1. Replace `Assets/app.ico`, light/dark icons, and logo PNGs with application
   branding.
2. Review `Services/Helpers/AppMetadata.cs`, repository links, release
   settings, and the generated README.
3. Configure `AppMetadata.SentryDsn` only if crash reporting is desired; it is
   off by default. Optionally set `SentryEnvironment` and `SentryRelease`;
   empty values use the beta/production and assembly-version defaults.
4. If adding a page through the item template manually, paste its strings into
   all three localization dictionaries, register its ViewModel, add its route
   and navigation item, then delete the strings snippet. `Scripts/add-page.ps1`
   automates those edits.
5. Translate the `TODO-translate` entries in generated Spanish and French
   strings after adding a page.
6. Configure signing, repository secrets, release versioning, and the
   Velopack/NuGet publication workflow before distributing an app.
7. Run the generated app and verify protocol activation, theme/language
   switching, and any selected optional feature.

## 6. Repository/template file classes

The source repository contains three file classes:

- **Mirrored:** app sources and documentation copied into `Templates/Project`
  and checked by `Scripts/test-mirror-parity.ps1`.
- **Repository-only:** maintainer, discovery, trust, packaging, CI, and
  historical files that are intentionally not shipped in generated projects.
- **Template-only:** `.template.config`, feature props, `AppFeatures.cs`,
  attribution, generated project/solution names, and dormant nested page
  template machinery.

The parity script is the authoritative allowlist for current differences.
Adding a file to either side requires classifying it there and, when
appropriate, updating this contract.

## 7. Compatibility policy

- Preserve `devtem-winui`, `devtem-page`, `WinUI3.App`, and the package ID.
- Preserve existing symbol names and defaults when possible.
- Add symbols rather than changing the meaning of existing symbols.
- Treat changing a default, removing a generated file, or changing a feature's
  package boundary as a compatibility-affecting change.
- Every compatibility-affecting change requires an updated contract,
  scaffold-matrix coverage, parity validation, and a roadmap note.
- Do not claim an option is independent until the disabled and mixed scaffold
  combinations prove that its files, packages, registrations, UI, tests, and
  documentation are independent.

The roadmap for extending this contract is
[`TEMPLATE-REUSABILITY-ROADMAP.md`](TEMPLATE-REUSABILITY-ROADMAP.md).
