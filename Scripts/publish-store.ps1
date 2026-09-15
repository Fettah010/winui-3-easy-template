# One-command Store release: pre-flight checks, Store upload build,
# Windows App Certification Kit when present, then the Partner Center
# submission page. The Store submission itself stays manual (certification
# takes days and needs a human); this script owns everything before it.
#
#   powershell -File Scripts/publish-store.ps1 -Publisher "CN=Your-ID"
#   powershell -File Scripts/publish-store.ps1 -Publisher "CN=Your-ID" -SkipWack
#
# Needs the Windows SDK (makeappx via build-msix.ps1). -Publisher defaults
# to the manifest default; the placeholder is rejected with a pointer.

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$Publisher = "",
    [switch]$SkipWack
)

$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$buildScript = Join-Path $projectRoot "Scripts\build-msix.ps1"
$submitUrl = "https://partner.microsoft.com/dashboard"

if ([string]::IsNullOrWhiteSpace($Publisher)) {
    [xml]$manifest = Get-Content -LiteralPath (Join-Path $projectRoot "Packaging\Msix\Package.appxmanifest")
    $Publisher = $manifest.Package.Identity.Publisher
}
if ([string]::IsNullOrWhiteSpace($Publisher) -or $Publisher -eq "CN=DevTem") {
    throw "No usable Publisher. Pass -Publisher with your Partner Center Publisher ID (or set it at scaffold time with --publisher)."
}
Write-Host "Store publisher: $Publisher"

if ($PSCmdlet.ShouldProcess("Store upload", "Validate + build")) {
    & $buildScript -Validate -Publisher $Publisher
    if ($LASTEXITCODE -ne 0) { throw "Pre-flight validation failed (exit $LASTEXITCODE)." }

    & $buildScript -StoreUpload -Publisher $Publisher
    if ($LASTEXITCODE -ne 0) { throw "Store upload build failed (exit $LASTEXITCODE)." }
}

$upload = Get-ChildItem -LiteralPath (Join-Path $projectRoot "Releases\Msix") -Filter "*.msixupload" -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1
if (-not $upload) { throw "No .msixupload found under Releases/Msix after the build." }
Write-Host "Upload ready: $($upload.FullName)"

if (-not $SkipWack) {
    $wack = Get-Command "appcert.exe" -ErrorAction SilentlyContinue
    if ($wack) {
        Write-Host "==> Windows App Certification Kit"
        & $wack.Source test -appxpackagepath $upload.FullName -reportoutputpath (Join-Path $projectRoot "wack.xml")
        if ($LASTEXITCODE -ne 0) { throw "WACK failed (exit $LASTEXITCODE). Fix the report, then resubmit." }
        Write-Host "WACK passed."
    }
    else {
        Write-Host "WARNING: appcert.exe not found (no App Certification Kit). Run before submitting:" -ForegroundColor Yellow
        Write-Host "  appcert.exe test -appxpackagepath $($upload.FullName) -reportoutputpath wack.xml"
    }
}

Write-Host "Next: Partner Center > your app > new submission, upload the .msixupload."
if ($PSCmdlet.ShouldProcess($submitUrl, "Open Partner Center")) {
    try { Start-Process $submitUrl } catch { Write-Host $submitUrl }
}
