# Microsoft Store updates

This scaffold chose `--updates store`: a packaged app whose updates come
from the Microsoft Store. No updater code, no feed, no release pipeline
is scaffolded — Settings shows a status card (version + an "Open
Microsoft Store" button deep-linking the listing).

## Publishing a release

1. Reserve the app name in Partner Center (one-time; free developer
   account at `storedeveloper.microsoft.com`).
2. Build the Store upload (needs the Windows SDK):

   ```powershell
   powershell -File Scripts/build-msix.ps1 `
     -Publisher "CN=Acme" `
     -StoreUpload
   ```

   This produces `Releases/Msix/<Name>_<version>_<arch>.msixupload`
   (bundle wrapped for submission; no cert needed — the Store signs it).
3. Run the Windows App Certification Kit locally first and fix failures:

   ```powershell
   appcert.exe test -appxpackagepath Releases\Msix\<file>.msixupload -reportoutputpath wack.xml
   ```

4. Partner Center > your app > Start submission:
   - **Packages:** upload the `.msixupload`.
   - **Pricing and availability:** free/paid, markets, audience.
   - **Properties:** category, capabilities; **Age ratings** questionnaire.
   - **Store listings:** description, screenshots, logos.
5. Submit for certification (up to ~3 business days). After it passes,
   the Store **re-signs** your package with a Microsoft certificate —
   no code-signing purchase, no pfx, no hardware token — and customers
   get updates automatically through the Store.

## Release cadence notes

- Version quads must increase (`0.0.5.0` > `0.0.4.0`); the script reads
  `<Version>` from the csproj.
- Staged rollouts and package flights are managed in Partner Center
  (the app needs no code for either).
- Submission automation exists (Store submission API) but is out of
  scope here — submissions are manual, releases are package uploads.
