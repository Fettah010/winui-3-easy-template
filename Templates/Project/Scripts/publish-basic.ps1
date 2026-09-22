# Publishes a basic-mode release: self-contained single-file Setup .exe plus
# its .sha256 checksum, attached to a (draft) GitHub release. This is the
# producer for the checksum convention Services/BasicGithubUpdateService.cs
# enforces: the app refuses to launch an .exe without a matching .sha256.
#
#   powershell -File Scripts\publish-basic.ps1 -Version 0.0.4 -WhatIf  # preview
#   powershell -File Scripts\publish-basic.ps1 -Version 0.0.4          # build + checksum only
#   powershell -File Scripts\publish-basic.ps1 -Version 0.0.4 -Publish # + draft GitHub release
#
# -Version  Release version (0.0.4 or v0.0.4; -beta suffix allowed).
# -Publish  Create a DRAFT GitHub release (gh) and upload both assets.
#           Without it, artifacts stay local for inspection.
# -RepoRoot Exists for the template matrix; normal use omits it.
#
# Single source of truth for basic releases (runnable locally, like
# build-and-release.ps1 is for Velopack). Needs the .NET SDK; -Publish
# additionally needs the gh CLI (authenticated).

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [switch]$Publish,

    # NOTE: $PSScriptRoot is NOT visible in parameter defaults (they evaluate
    # in the caller's scope), so path defaults resolve in the body below.
    [string]$RepoRoot = ""
)

$ErrorActionPreference = "Stop"

$Version = $Version.Trim()
if ($Version.StartsWith("v", [System.StringComparison]::OrdinalIgnoreCase)) { $Version = $Version.Substring(1) }
if ($Version -notmatch '^\d+\.\d+\.\d+(-[A-Za-z0-9.]+)?$') {
    throw "Version '$Version' must look like 0.0.4 or 0.0.4-beta."
}
$tag = "v$Version"

if ($RepoRoot -eq "") { $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path }
$RepoRoot = (Resolve-Path $RepoRoot).Path

$appCsproj = Get-ChildItem -LiteralPath $RepoRoot -Filter "*.csproj" |
    Where-Object { $_.FullName -notlike "*\Tests\*" -and $_.FullName -notlike "*\Packaging\*" } |
    Select-Object -First 1
if ($null -eq $appCsproj) { throw "No app .csproj found in $RepoRoot." }
$csprojText = [System.IO.File]::ReadAllText($appCsproj.FullName, [System.Text.Encoding]::UTF8)
$nsMatch = [regex]::Match($csprojText, "<AssemblyName>([^<]+)</AssemblyName>")
$safeName = if ($nsMatch.Success) { $nsMatch.Groups[1].Value } else { [System.IO.Path]::GetFileNameWithoutExtension($appCsproj.Name) }

$outDir = Join-Path $RepoRoot "ReleasesBasic\$tag"
$assetName = "$safeName-Setup-$Version.exe"
$assetPath = Join-Path $outDir $assetName
$checksumPath = "$assetPath.sha256"

if ($WhatIfPreference) {
    Write-Host "WhatIf: would publish $appCsproj to $assetPath (self-contained single-file win-x64)."
    Write-Host "WhatIf: would write SHA-256 to $checksumPath."
    if ($Publish) { Write-Host "WhatIf: would create draft GitHub release $tag with both assets." }
    return
}

Write-Host "==> publish $safeName $tag (Release/x64, single-file)" -ForegroundColor Cyan
Push-Location $RepoRoot
try {
    # EnableMsixTooling: WinAppSDK single-file publish needs it for the
    # embedded resources.pri generation (no MSIX output is produced).
    & dotnet publish $appCsproj.FullName -c Release -p:Platform=x64 -r win-x64 --self-contained `
        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableMsixTooling=true -o $outDir 2>&1 |
        Select-Object -Last 5
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }
}
finally { Pop-Location }

$built = Join-Path $outDir "$safeName.exe"
if (-not (Test-Path -LiteralPath $built)) {
    throw "Publish produced no $safeName.exe in $outDir."
}
if ($PSCmdlet.ShouldProcess($assetPath, "Rename published exe")) {
    Move-Item -LiteralPath $built -Destination $assetPath -Force
}

if ($PSCmdlet.ShouldProcess($checksumPath, "Write SHA-256")) {
    $hash = (Get-FileHash -LiteralPath $assetPath -Algorithm SHA256).Hash.ToLowerInvariant()
    [System.IO.File]::WriteAllText($checksumPath, "$hash  $assetName", (New-Object System.Text.UTF8Encoding($false)))
    Write-Host "Checksum: $hash" -ForegroundColor DarkGray
}

# Prove the pair before any upload: re-read and compare, exactly as the app does.
$recheck = (Get-FileHash -LiteralPath $assetPath -Algorithm SHA256).Hash.ToLowerInvariant()
$recorded = ([System.IO.File]::ReadAllText($checksumPath, [System.Text.Encoding]::UTF8).Trim() -split '\s+')[0].ToLowerInvariant()
if ($recheck -ne $recorded) { throw "Checksum self-check failed for $assetPath." }
Write-Host "Self-check passed: $assetName + $assetName.sha256" -ForegroundColor Green

if ($Publish) {
    $gh = Get-Command gh -ErrorAction SilentlyContinue
    if ($null -eq $gh) { throw "gh CLI not found. Install it and run 'gh auth login' first." }
    if ($PSCmdlet.ShouldProcess($tag, "Create draft GitHub release")) {
        Push-Location $RepoRoot
        try {
            & gh release create $tag $assetPath $checksumPath --title $tag --draft --notes "Basic-mode release $tag (Setup .exe + SHA-256)."
            if ($LASTEXITCODE -ne 0) { throw "gh release create failed with exit code $LASTEXITCODE" }
        }
        finally { Pop-Location }
        Write-Host "Draft release $tag created. Review assets, then publish it in the browser." -ForegroundColor Green
    }
}
else {
    Write-Host "Local only (no -Publish): review $outDir, then re-run with -Publish." -ForegroundColor Yellow
}
