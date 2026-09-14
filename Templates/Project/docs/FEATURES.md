# Selected features

This file is generated from the options passed to `dotnet new devtem-winui`.
The feature-specific guides under `docs/feature-guides/` exist only when the
corresponding feature is enabled.

| Feature | Selected |
| --- | --- |
| System tray | __TRAY__ |
| Velopack updates | __UPDATES__ |
| SQLite database | __DATABASE__ |
| Typed HTTP client | __HTTP__ |
| Diagnostics page | __HEALTH__ |
| DevTem attribution | __ATTRIBUTION__ |

## Next steps

- Replace the generated assets with your own branding.
- Review `Services/Configuration/ProductConfiguration.cs`.
- Add pages with `Scripts/add-page.ps1`.
- Add localization keys to all three dictionaries.
- Run the build and tests before publishing.

Feature-specific setup is generated under `docs/feature-guides/` only for
features whose `Selected` value is `True`.
