# WebView2 content (opt-in, never default)

Web content is a per-app decision: this scaffold ships **zero WebView2 bytes**
by default. Follow this guide only when a page must render web content.

## Add it

1. Add the package (check the latest stable first):
   ```powershell
   dotnet add package Microsoft.Web.WebView2
   ```
2. Add a page (`add-page.ps1 -Kind page`) and drop a `WebView2` control in it:
   ```xml
   <WebView2 x:Name="ContentWeb" Source="https://example.com" />
   ```
3. Ensure the runtime is present: WebView2 needs the evergreen runtime on the
   machine (most Windows 10/11 machines have it via Edge). Handle
   `CoreWebView2InitializationCompleted` failures by showing the empty state,
   not a crash (house rule: external calls stay never-throw at the UI seam).

## Weight

WebView2 adds ~100MB+ to publish output (runtime loader + projections). Record
the delta with `Scripts/measure-publish-weight.ps1` before and after; if the CI
weight gate trips, that is the gate working - re-baseline with the reason in
`docs/DECISIONS.md`, never silently.

## Privacy

Web content fetches remote resources: set a navigation policy (allowlist your
origins), keep cache/data under `Services/AppPaths.cs` `DataFolder` (writable
for both distributions - the packaged install directory is read-only), and
document what loads remote content in your privacy posture (see
`docs/feature-guides/crash.md` for the posture pattern).

## Packaged vs portable

Portable: works as-is (evergreen runtime resolved from the system). MSIX: the
runtime resolves the same way; no manifest change is needed for https content.
Custom schemes or local-file bridges need per-app review - keep them out of
the shared template.

## Remove it

Delete the page (see `TEMPLATE-GUIDE.md` whole-page checklist), remove the
package ref, re-run build + tests. Base-scaffold weight returns to baseline.

## Why not default

Weight (~100MB+ for every scaffold), privacy surface (remote content in a
template every app inherits), and app-domain fit (most desktop apps never embed
the web). TemplateStudio ships a WebView2 page by default; DevTem answers it as
an opt-in recipe so the default stays lean.
