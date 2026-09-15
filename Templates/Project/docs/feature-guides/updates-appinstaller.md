# AppInstaller auto-updates

This scaffold chose `--updates appinstaller`: native Windows auto-updates
for a sideloaded packaged app, driven by an `.appinstaller` feed file.
No updater SDK, no custom UI — the update prompts are Microsoft-owned.
In the app, Settings shows a status card (version + a button opening
Windows Apps settings, where the feed can be managed).

## Publishing a release

1. Pack, sign, and emit the feed in one run (needs the Windows SDK;
   `-DryRun` validates everything short of `makeappx`):

   ```powershell
   powershell -File Scripts/build-msix.ps1 `
     -Publisher "CN=Acme" `
     -CertificatePath C:\certs\app.pfx -CertificatePassword "secret" `
     -AppInstaller -InstallUrl "https://example.com/msix/"
   ```

   This produces `Releases/Msix/`:
   `<Name>_<version>_<arch>.msix` + the same-named `.appinstaller`
   pointing at it (schema 2021: `OnLaunch` check every
   `-HoursBetweenUpdateChecks` hours (default 12) with a prompt that
   never blocks launch, plus a background task).
2. Upload **both files** to the `-InstallUrl` location, keeping the
   relative layout (the feed references the package by URL).
3. Users install once from the `.appinstaller` URL. Windows checks the
   feed on launch and in the background; `ForceUpdateFromAnyVersion`
   is off, so only upgrades apply (no silent downgrades).

## Signing and trust

Sideloaded packages must be signed with a cert the machine trusts:

- Testing: self-signed (`New-SelfSignedCertificate`), install the
  `.cer` into Trusted People / Trusted Root on the test machine.
- Real users: a cert chaining to a Trusted Root CA (Azure Artifact
  Signing ~$10/month, or an OV cert). Expect SmartScreen friction for
  unknown publishers until reputation builds.

The `Publisher` passed to the script MUST match the cert subject,
or installation fails.

## Manual round-trip checklist (per release)

1. Fresh VM/machine: install the cert, double-click the
   `.appinstaller` URL, confirm the app installs and runs.
2. Publish a higher version to the same URL; relaunch the app and
   confirm the update prompt appears and applies.
3. Settings > Apps > Installed apps > your app > Advanced options:
   confirm the AppInstaller source URL is listed.

## Limits (by design)

- No custom update UI, no deltas, no channels — Windows owns the flow.
- Moving the feed URL later requires reinstalling from the new URL.
- The app itself cannot trigger or skip checks; it only reports status.
