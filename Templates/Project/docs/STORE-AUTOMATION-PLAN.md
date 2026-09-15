# Store Automation Plan — DevTem-WinUI 3

Goal: make Store distribution (MSIX, Microsoft-signed, no paid cert)
a one-command flow for devs scaffolding from this template.

## Now (this session)

- [x] **1. One-command release wrapper** — `Scripts/publish-store.ps1`:
      validate → `build-msix.ps1 -StoreUpload` → WACK when the kit is
      present (exact command printed when absent) → open the Partner
      Center submission page. Mirrored to `Templates/Project/Scripts/`.
- [x] **2. Pre-flight validation** — `build-msix.ps1 -Validate`: fast,
      no-SDK checks (placeholder Publisher rejected, version quad valid
      and increasing vs. latest `v*` tag, manifest well-formed with
      Name/Publisher, tile source present). Same flag in both trees.
- [x] **3. Version-bump automation** — `Scripts/bump-version.ps1 -Version X`:
      updates the app csproj (all four props) + template csproj +
      `CITATION.cff` + inserts a `CHANGELOG.md` stub when the section is
      missing. Skips files absent from a scaffold. Mirrored verbatim
      (auto-locates the WinExe csproj, so renames flow).
- [x] **4. Publisher plumbing** — new `publisher` template symbol
      (`replaces: "CN=DevTem"`): manifest `Publisher` attribute +
      `build-msix.ps1` default arrive pre-filled at scaffold time.
      Placeholder still rejected by `-Validate` with a Partner Center
      pointer. Proved by the `msixstore` matrix combo (custom publisher
      asserted in the staged manifest).

## Future (separate session, needs a real tenant)

Done this session against documented API shapes (no live tenant to
prove the round-trip — first real submission is the live proof):

- [x] **5. Store submission-API automation** — `Scripts/submit-store.ps1`
      (create → upload → commit → optional poll; `DEVTEM_STORE_*` env
      creds, `-WhatIf` clean) + manual-dispatch `store-submit.yml`
      (msix scaffolds only; secrets as repo secrets). Staged rollouts
      stay Partner Center-managed. Mirrored verbatim.
- [x] **6. Listing drafts from the repo** — `Scripts/new-store-listing.ps1`
      generates git-ignored `Store/Listing/` (description from README,
      screenshot + submission checklists). Manual form stays copy-paste.
      Mirrored verbatim.

## Verification

Fast (`dotnet build` 0/0 + `dotnet test`) · parity OK (new scripts +
manifest comment mirrored, `publisher` has no `#if` so no guard entry)
· matrix subset (`msixstore` publisher assert, `allon`, `nosetup`) ·
Workflow tier for scripts (`pwsh` parse + pass/reject logic runs).
`msix.yml` CI re-proves pack+sign whenever packaging inputs change.
