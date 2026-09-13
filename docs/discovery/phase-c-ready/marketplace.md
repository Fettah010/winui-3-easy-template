# Visual Studio Marketplace investigation

## Current conclusion

Do not build a VSIX yet. DevTem's NuGet template package is already consumed
by the .NET template engine used by Visual Studio's New Project dialog, so a
VSIX would duplicate distribution and create another versioned surface.

## Verification checklist

- [ ] Install `DevTem.Templates` in a clean developer environment.
- [ ] Confirm `devtem-winui` appears in Visual Studio's New Project dialog.
- [ ] Verify icon, display name, description, and parameter checkboxes.
- [ ] Record the Visual Studio version and Windows App SDK/.NET requirements.
- [ ] If discoverability is insufficient, compare a Marketplace listing
      against improving NuGet metadata before choosing a VSIX.

## Listing copy if a listing becomes justified

**Name:** DevTem WinUI 3 starter templates  
**Summary:** Production-ready WinUI 3 desktop app and MVVM content-page
templates for .NET, distributed through the .NET template engine.  
**Link:** <https://github.com/Fettah010/winui-3-easy-template>

