# DevTem Template Reusability Roadmap

**Status:** Planned  
**Owner:** Fettah010  
**Scope:** Project-template customization, feature options, generated-project quality, reuse, and maintainability  
**Out of scope:** Search/SEO work, external promotion, release publication, and unrelated application refactors

This is the implementation roadmap for making DevTem a professional, reusable WinUI 3 project template. It is ordered by value and dependency, not by file location. Agents must complete phases in order unless a phase explicitly states that it may run in parallel.

## How to use this roadmap

- The unchecked boxes are the work queue.
- One coherent phase should normally be one commit.
- Do not mark a parent item complete until its acceptance criteria pass.
- If implementation reveals a design choice not covered here, stop at the decision gate and update this document before coding further.
- Every source change must update `Templates/Project/` when the corresponding app source is mirrored.
- Do not push, tag, publish, or create a pull request unless the user explicitly requests it.
- Refresh `docs/STATE.md` at the end of every implementation session.

## Working rules for agents

Before starting:

1. Read `AGENTS.md`, `docs/STATE.md`, `docs/TEMPLATE-REUSABILITY-ROADMAP.md`, and `docs/WORKFLOW.md`.
2. Run `git status`, `git log --oneline -5`, and inspect the newest tags.
3. Run the fast verification baseline:
   `dotnet build -c Debug -p:Platform=x64`
   and the relevant unit tests.
4. Read this roadmap and identify the exact phase and checklist item being implemented.
5. State the intended files, behavior change, and validation tier before editing.

While working:

- Prefer existing abstractions and patterns.
- Keep template options small, composable, and testable.
- Do not add a parameter merely because a value is configurable in theory.
- Do not silently fall back on invalid input.
- Keep generated output buildable with zero warnings and zero errors.
- Treat template symbols, identities, and package names as public compatibility contracts.
- Keep user edits safe: generated changes must be deterministic, explainable, and idempotent.

Before marking work complete:

- Run the verification tier required by `docs/WORKFLOW.md`.
- Run mirror parity and the scaffold matrix for any template change.
- Update directly related documentation.
- Update this roadmap checkboxes and notes.
- Refresh `docs/STATE.md`.
- Report incomplete, blocked, or deferred work explicitly.

## Current baseline

The template currently provides:

- `devtem-winui` project template
- `devtem-page` item template
- identity, display name, company, repository, and URI-scheme parameters
- `tray`, `updates`, `database`, `http`, and `attribution` options
- MVVM, localization, settings, logging, diagnostics, Velopack, and optional SQLite infrastructure
- `Scripts/init-template.ps1` for post-generation rebranding
- `Scripts/add-page.ps1` for page integration
- mirror parity validation and a four-case scaffold matrix

Known baseline limitations:

- Technical identity and display identity are too tightly coupled.
- Database and HTTP behavior are coupled in the current feature model.
- Feature options are not represented by one dependency manifest.
- Generated documentation is not fully profile-aware.
- Page generation still relies on file insertion and manual integration assumptions.
- Branding is mostly a manual asset replacement process.
- Scaffold testing does not cover the full parameter and feature surface.
- Template metadata contains at least one documentation mismatch for attribution.

---

# Phase 0 — Contract, inventory, and safety baseline

**Priority:** P0 — do first  
**Goal:** Establish the rules that every later option and generated file must follow.

## 0.1 Inventory the current template contract

- [x] List every `template.json` symbol, default, datatype, choice, and file modifier.
- [x] List every feature-owned file, package reference, compile symbol, service registration, route, setting, localization key, script, workflow, and documentation claim.
- [x] Record all identity tokens used by source files, scripts, manifests, tests, and documentation.
- [x] Record the difference between repository-only files, template-only files, and mirrored files.
- [x] Identify all manual steps currently required after `dotnet new`.
- [x] Identify all behavior that `init-template.ps1` changes but `template.json` does not.

**Affected files:** `Templates/Project/.template.config/template.json`, `Templates/Page/.template.config/template.json`, `Scripts/init-template.ps1`, `Templates/Project/Scripts/init-template.ps1`, `Scripts/add-page.ps1`, `docs/TEMPLATE-GUIDE.md`, `Scripts/test-templates.ps1`, `Scripts/test-mirror-parity.ps1`.

## 0.2 Publish the stable contract

- [x] Create `docs/template-contract.md`.
- [x] Define required generated behavior.
- [x] Define optional features and their defaults.
- [x] Define supported identity fields.
- [x] Define extension points for services, navigation, settings, localization, updates, and data.
- [x] Define files users may safely edit.
- [x] Define files generated tools may rewrite.
- [x] Define compatibility rules for `identity`, `shortName`, symbols, and package IDs.
- [x] Document what is intentionally opinionated and what is configurable.

## 0.3 Correct baseline documentation mismatches

- [x] Correct the attribution description in `template.json` so it describes `DevTemAttribution.cs`, not a generated README.
- [x] Ensure generated README claims match the current implementation.
- [x] Ensure `docs/TEMPLATE-GUIDE.md` does not describe retired files or unsupported options.
- [x] Add a roadmap link from the template-maintainer documentation.

### Phase 0 acceptance criteria

- The complete current option and dependency surface is documented.
- Every existing option has one authoritative description and default.
- No documentation describes behavior that the current scaffold does not produce.
- No source code changes are required to complete this phase.

### Required validation

- Documentation review.
- `git diff --check`.
- No build required for docs-only changes.

---

# Phase 1 — Identity and naming model

**Priority:** P0  
**Depends on:** Phase 0  
**Goal:** Make generated application identity predictable, safe, and independently customizable.

## 1.1 Define independent identity fields

Support and document these concepts separately:

| Field | Example | Purpose |
|---|---|---|
| Safe project name | `AcmeDesk` | Directory and project file |
| Display name | `Acme Desk` | UI and installer-facing text |
| Root namespace | `Acme.Desk` | C# namespace |
| Assembly name | `AcmeDesk` | Binary and assembly identity |
| Company | `Acme Corporation` | Product metadata |
| Repository URL | `https://github.com/acme/desk` | Source/release links |
| URI scheme | `acmedesk` | Protocol activation |
| App ID | `Acme.Desk` | Mutex, identity, and OS integration |
| Settings folder | `AcmeDesk` | Local persisted data |

- [x] Decide which fields are exposed in the first public option set: preserve the existing `name`, `displayName`, `company`, `repo`, and `scheme` symbols; keep namespace and assembly derived from `safeName` for backward-compatible defaults.
- [x] Add defaults derived from the safe name.
- [x] Add validation for each field in `init-template.ps1`; template-engine technical identity remains constrained by the existing symbol model.
- [x] Reject invalid migration-script values with actionable errors.
- [x] Ensure display names support spaces and Unicode where Windows APIs permit them.
- [x] Ensure technical names remain identifier-safe.

## 1.2 Replace literal-token replacement where template symbols are sufficient

- [x] Add stable template symbols for the selected identity fields.
- [x] Replace source and project metadata through template symbols.
- [x] Keep `init-template.ps1` for existing repositories and migration scenarios.
- [x] Make the script use the same safe-name shape as the template and validate repository/scheme inputs.
- [ ] Prevent user values from rewriting unrelated prose.
- [x] Preserve frozen archive and token-documentation behavior.

## 1.3 Validate generated identity

- [x] Add scaffold assertions for project filename, namespace, assembly, display name, protocol, settings path, and repository URL.
- [x] Test names with spaces.
- [x] Test punctuation and invalid leading digits through the existing sanitized-name matrix coverage.
- [x] Test the generated safe-name result through scaffold output assertions.
- [x] Test custom URI schemes.
- [x] Test Unicode display names.
- [ ] Verify no `DevTem`, `devtem://`, or repository leftovers remain in generated application-owned files.

### Phase 1 acceptance criteria

- A generated project can use a different technical namespace, display name, assembly name, and URI scheme without manual search-and-replace.
- Invalid values fail before files are left in an unusable state.
- Existing default invocations remain backward compatible.
- `init-template.ps1` and `dotnet new` produce equivalent identity results.

### Required validation

- Fast tier.
- Matrix tier.
- Identity-focused scaffold assertions.
- Mirror parity.

---

# Phase 2 — Feature dependency model

**Priority:** P0  
**Depends on:** Phase 1  
**Goal:** Make optional features complete, independent where appropriate, and safe to remove.

## 2.1 Define feature boundaries

Create a documented dependency table for:

- tray
- updates
- database
- HTTP client
- logging
- crash reporting
- localization
- attribution
- generated tests
- release workflows
- shell/profile content

- [x] For each feature, list files, packages, symbols, registrations, UI, settings, localization keys, tests, scripts, workflows, and documentation.
- [x] Identify accidental dependencies.
- [x] Identify packages that should not be included when a feature is disabled.
- [x] Identify features that cannot be independently disabled without architectural work.

## 2.2 Separate database and HTTP

- [x] Split the current database option from HTTP client infrastructure.
- [x] Add an independent HTTP option or clearly documented profile.
- [x] Support database off/HTTP on.
- [x] Support database on/HTTP off.
- [x] Remove unused package references, registrations, files, and docs for each disabled combination.
- [x] Keep database schema initialization separate from general settings persistence.

## 2.3 Add diagnostics controls

- [x] Define logging behavior and default.
- [x] Define crash-reporting behavior and default.
- [x] Ensure crash reporting is never silently enabled with a real DSN.
- [x] Make logging and crash-reporting package inclusion conditional where practical.
- [x] Provide explicit configuration seams for DSN, environment, and release.

Current policy: logging remains included and enabled by default because it is
used by global error paths and is part of the template's baseline support
contract. Crash reporting remains package-included but DSN-gated and inactive
when the DSN is empty. Feature-owned package inclusion is conditional where it
is safe (including database, HTTP, tray, and updates); logging and Sentry stay
baseline infrastructure. `AppMetadata.SentryDsn`, `SentryEnvironment`, and
`SentryRelease` are the explicit generated configuration seams.

## 2.4 Add localization controls

- [x] Define whether localization can be disabled.
- [x] Define a supported default-language option.
- [x] Define a safe first language-profile model.
- [x] Ensure disabled localization does not leave invalid bindings or missing resources.
- [x] Keep localization key coverage tests authoritative.

Current policy: localization remains always included in the initial template
contract, with `en-US`, `es-ES`, and `fr-FR` dictionaries and the existing
language selector. No localization-off switch is exposed until bindings and
resource fallback can be removed safely. Adding a language profile remains a
future compatibility change.

## 2.5 Add feature manifest metadata

- [x] Create a machine-readable feature manifest for validation.
- [x] Record feature dependencies and exclusions.
- [x] Use the manifest to validate the generated manifest shape and combine it with matrix assertions for package references and files.
- [x] Keep the manifest declarative; do not duplicate runtime business logic in it.

### Phase 2 acceptance criteria

- Every supported feature combination builds and tests.
- Disabled features leave no broken references or misleading generated documentation.
- Database and HTTP are independently understandable and testable.
- Feature ownership is discoverable from one manifest and one guide.

### Required validation

- Fast tier.
- Expanded matrix tier.
- Mirror parity.
- Package/reference assertions.
- Full unit tests.

---

# Phase 3 — Profiles and coherent starter variants

**Priority:** P1  
**Depends on:** Phase 2  
**Goal:** Avoid exposing a confusing collection of unrelated switches.

## 3.1 Decide the profile strategy

Choose and document one approach:

- [x] One template with composable switches.
- [x] One template with documented named profile presets.
- [ ] Multiple templates or aliases for minimal, desktop, and production variants.

**Decision:** Keep one public template and preserve the existing feature
switches as the compatibility surface. Profiles are documented command
presets rather than a new `--profile` symbol: this keeps explicit feature
options authoritative and avoids a template-engine limitation where a profile
cannot safely change a boolean parameter default while still allowing an
explicit override. The presets are tested as ordinary scaffold combinations.

## 3.2 Define initial profiles

Proposed profiles:

### Minimal

- Home and Settings shell
- MVVM
- localization
- settings
- logging
- no optional distribution services by default

### Desktop

- tray
- notifications
- protocol activation
- localization
- settings

### Production

- tray
- updates
- database
- HTTP
- diagnostics
- localization
- release workflow documentation

- [x] Define exact defaults for each profile.
- [x] Ensure explicit options override profile defaults.
- [x] Document the resulting file and package differences.
- [x] Keep profile names stable after publication.

### Phase 3 acceptance criteria

- A user can choose a coherent starting point without understanding every internal service.
- Explicit options remain available for advanced users.
- Profiles do not create untested or undocumented combinations.

### Required validation

- Matrix tier for every profile.
- Generated README comparison.
- Package install and scaffold test.

---

# Phase 4 — Generated configuration and extension points

**Priority:** P1  
**Depends on:** Phase 2  
**Goal:** Make generated applications easy to customize after scaffolding without editing infrastructure blindly.

## 4.1 Define configuration layers

Document and enforce the distinction between:

- template-time options
- runtime user preferences
- deployment configuration
- developer-local configuration

- [x] Define a generated branding/product configuration file.
- [x] Define deployment placeholders for update feeds, support URLs, privacy URLs, and optional crash reporting.
- [x] Define local development configuration without committing secrets.
- [x] Document precedence and ownership for each setting.

## 4.2 Add stable extension seams

- [x] Add a documented service-registration extension point.
- [x] Add a documented navigation extension point.
- [x] Add a documented settings extension point.
- [x] Add a documented localization extension convention.
- [x] Add a documented update-provider abstraction.
- [x] Separate application data access from template infrastructure.
- [x] Keep generated regions explicit and recognizable.

## 4.3 Protect user edits

- [x] Define generated-region markers.
- [x] Make tooling idempotent.
- [x] Preserve content outside generated regions.
- [x] Detect ambiguous or missing markers.
- [x] Provide dry-run output before writing.
- [x] Add `SupportsShouldProcess` to scripts that write outside the repository.

### Phase 4 acceptance criteria

- A generated app can add a service, route, setting, and localization entry without modifying unrelated infrastructure.
- Re-running supported tooling does not duplicate registrations.
- User-owned code outside generated regions is preserved.
- Configuration examples contain no secrets.

### Required validation

- Fast tier.
- Script parse and pass/reject tests.
- Idempotence tests.
- Matrix tier if template content changes.

---

# Phase 5 — Page template and page-generation workflow

**Priority:** P1  
**Depends on:** Phase 4  
**Goal:** Turn page creation into a reliable, repeatable product workflow.

## 5.1 Define the page contract

- [x] Define page name, title, route, icon, navigation visibility, and test options.
- [x] Define naming validation and reserved-name rules.
- [x] Define generated files and registration behavior.
- [x] Define whether page generation is CLI-only or also Visual Studio-friendly.

## 5.2 Improve `devtem-page`

- [x] Generate page, code-behind, ViewModel, and test stub.
- [x] Generate localization keys through a supported mechanism.
- [x] Add route and DI registration through generated regions.
- [x] Add optional navigation item generation.
- [x] Support custom route and icon choices.
- [x] Prevent duplicate registration on rerun.

## 5.3 Improve `Scripts/add-page.ps1`

- [x] Add `-WhatIf`/dry-run support.
- [x] Add a clear file-change preview.
- [x] Fail safely when insertion markers are absent or ambiguous.
- [x] Reject destructive or conflicting changes rather than overwriting user code.
- [x] Make repeated execution idempotent.
- [x] Produce a concise completion report.

## 5.4 Test multiple pages

- [x] Generate one page.
- [x] Generate two pages.
- [x] Generate a page with an existing route.
- [x] Generate a page with a custom icon.
- [x] Rerun the same command.
- [x] Build and run tests after the supported single-page scenario.

### Phase 5 acceptance criteria

- [x] Page generation is deterministic and repeatable.
- [x] Duplicate routes, services, navigation items, and localization keys are rejected or safely ignored.
- [x] Users can understand every generated change before accepting it.

### Required validation

- [x] Fast tier.
- [x] Matrix tier.
- [x] Script behavior tests.
- [x] Live UI tier for generated navigation behavior.

---

# Phase 6 — Branding and visual customization

**Priority:** P1  
**Depends on:** Phase 4  
**Goal:** Reduce manual rebranding work and keep visual identity consistent.

## 6.1 Define branding inputs

- [x] Display name.
- [x] Company.
- [x] Primary/accent color.
- [x] Website/support/privacy URLs.
- [x] Icon and logo workflow.
- [x] Installer and shortcut identity.

## 6.2 Implement safe branding workflow

- [x] Add a generated branding configuration/resource surface.
- [x] Use branding values consistently in app UI.
- [x] Document supported image dimensions and formats.
- [x] Provide a validated icon replacement script or workflow.
- [x] Ensure installer, tray, shortcut, title bar, splash, and About surfaces agree.
- [x] Do not copy arbitrary files without validating paths and formats.

### Phase 6 acceptance criteria

- Rebranding does not require broad search-and-replace.
- [x] A generated app has one documented source of truth for product-facing identity.
- [x] Invalid asset inputs fail clearly.

### Required validation

- [x] Fast tier.
- [x] Pack tier for manifest/assets changes.
- [x] Live UI tier.
- [x] Generated asset and metadata assertions.

---

# Phase 7 — Profile-aware generated documentation

**Priority:** P1  
**Depends on:** Phases 2–6  
**Goal:** Ensure generated projects describe what they actually contain.

## 7.1 Generate documentation from feature fragments

- [x] Separate base documentation from feature-specific fragments.
- [x] Include only enabled feature instructions.
- [x] Generate an option summary in the project README.
- [x] Generate a “next steps” section based on the selected profile.
- [x] Explain which files are safe to customize.
- [x] Explain flags versus post-scaffold editing.

## 7.2 Add customization cookbook content

- [x] Add service registration example.
- [x] Add page creation example.
- [x] Add localization example.
- [x] Add settings example.
- [x] Add update-provider guidance.
- [x] Add database/API separation guidance.
- [x] Add branding and release configuration guidance.

## 7.3 Add migration guidance

- [x] Document template package upgrades.
- [x] Document app-template regeneration limitations.
- [x] Document manual migration of generated applications.
- [x] Document breaking changes to symbols and profiles.

### Phase 7 acceptance criteria

- A freshly generated README contains no claims for disabled features.
- The guide explains the supported customization path from a clean checkout.
- Migration instructions exist before the first breaking template release.

### Required validation

- [x] Documentation review.
- [x] Generated README assertions across representative profiles.
- [x] `git diff --check`.

---

# Phase 8 — Comprehensive verification and regression protection

**Priority:** P0/P1 continuous requirement  
**Depends on:** Every implementation phase  
**Goal:** Prove the template works as an installed product, not only as source files.

## 8.1 Expand scaffold matrix

- [ ] All features enabled.
- [ ] All optional features disabled.
- [ ] Database only.
- [ ] HTTP only.
- [ ] Tray only.
- [ ] Updates only.
- [ ] Logging/crash-reporting combinations.
- [ ] Localization profiles.
- [ ] Minimal, Desktop, and Production profiles.
- [ ] Attribution on/off.

## 8.2 Add parameter validation tests

- [ ] Safe names.
- [ ] Display names with spaces.
- [ ] Invalid identifiers.
- [ ] Invalid URI schemes.
- [ ] Invalid repository URLs.
- [ ] Empty required values.
- [ ] Unicode display names.
- [ ] Existing output directory.
- [ ] Output directory with spaces.

## 8.3 Validate generated projects

- [ ] Build with zero warnings and errors.
- [ ] Run all generated unit tests.
- [ ] Confirm package references match enabled features.
- [ ] Confirm disabled files are absent.
- [ ] Confirm no stale template identity remains.
- [ ] Confirm project/solution references are valid.
- [ ] Confirm README claims match the profile.
- [ ] Confirm protocol and app metadata use the selected identity.

## 8.4 Validate the actual package

- [ ] Pack `DevTem.Templates`.
- [ ] Inspect the `.nupkg` contents.
- [ ] Install from the package, not only the source directory.
- [ ] Generate each required profile from the installed package.
- [ ] Test package metadata, icon, README, and license.
- [ ] Ensure `bin`, `obj`, and unrelated repository files are excluded.

## 8.5 Validate Visual Studio behavior

- [ ] Verify template metadata appears correctly.
- [ ] Verify parameter labels and defaults are understandable.
- [ ] Verify choices do not expose unsupported combinations.
- [ ] Verify generated output opens in Visual Studio.
- [ ] Verify CLI and Visual Studio produce equivalent projects where promised.

### Phase 8 acceptance criteria

- The installed NuGet template passes the same meaningful checks as the source template.
- Every supported public option has a test.
- A regression in one feature cannot silently break another feature’s generated output.

---

# Phase 9 — Release, compatibility, and maintenance policy

**Priority:** P1  
**Depends on:** Phase 8  
**Goal:** Make future template versions safe to publish and upgrade.

## 9.1 Define compatibility policy

- [ ] Classify changes as patch, minor, or breaking.
- [ ] Protect template `identity` and `shortName`.
- [ ] Document symbol deprecation.
- [ ] Document profile compatibility.
- [ ] Document generated-project migration expectations.

## 9.2 Add release gates

- [ ] Require fast verification.
- [ ] Require matrix and parity validation.
- [ ] Require package validation.
- [ ] Require clean generated-project builds.
- [ ] Require documentation/version consistency.
- [ ] Require changelog entries for public option changes.

## 9.3 Separate app and package releases

- [ ] Keep app versioning independent from template package versioning.
- [ ] Verify tags trigger the intended workflow only.
- [ ] Do not reuse immutable NuGet versions.
- [ ] Document release order and rollback behavior.

### Phase 9 acceptance criteria

- A maintainer can publish a template package using a documented, repeatable process.
- Users can determine whether an update is compatible with an existing generated application.
- No release depends on manually remembering undocumented files or commands.

---

# Decision gates

These choices must be resolved before the related implementation begins.

## Gate A — One template versus multiple templates

- [ ] Decide whether profiles are parameters, aliases, or separate templates.
- [ ] Record the decision in `docs/DECISIONS.md`.
- [ ] Prefer the smallest public surface that covers real use cases.

## Gate B — CLI versus Visual Studio

- [ ] Decide which options must work equally in CLI and Visual Studio.
- [ ] Avoid exposing options that only work through custom scripts unless clearly labeled.

## Gate C — Attribution default

- [ ] Decide whether attribution remains on by default.
- [ ] Explain the choice in generated README documentation.
- [ ] Ensure opting out is complete and leaves no misleading text.

## Gate D — Release workflow inclusion

- [ ] Decide whether GitHub release workflows are part of the production profile only.
- [ ] Ensure updates can be enabled without forcing a specific hosting provider where possible.

## Gate E — Test project inclusion

- [ ] Decide whether unit tests are always generated.
- [ ] Decide whether smoke tests remain repository-only.
- [ ] Document the rationale and generated-project expectations.

## Gate F — Database and API architecture

- [ ] Decide whether database and HTTP are independent options.
- [ ] Preserve the distinction between template infrastructure and application data model.

## Gate G — Localization language model

- [ ] Decide between named language profiles and arbitrary language lists.
- [ ] Prefer named, tested profiles until dynamic resource generation is proven reliable.

---

# Final definition of done

The roadmap is complete when all of the following are true:

- [ ] The public template contract is documented.
- [ ] Identity fields are validated and independently customizable.
- [ ] Features have explicit dependency boundaries.
- [ ] Profiles produce coherent, documented applications.
- [ ] Generated projects expose safe extension points.
- [ ] Page generation is repeatable and idempotent.
- [ ] Branding is documented and reproducible.
- [ ] Generated documentation matches enabled features.
- [ ] The installed NuGet package passes end-to-end scaffold validation.
- [ ] CLI and promised Visual Studio behavior are verified.
- [ ] Compatibility and release policy are documented.
- [ ] `docs/STATE.md` records the completed phases and remaining work.
