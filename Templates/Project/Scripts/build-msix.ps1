# Builds an MSIX package for sideloading or Store submission.
#
#   powershell -File Scripts/build-msix.ps1 -DryRun
#   powershell -File Scripts/build-msix.ps1
#   powershell -File Scripts/build-msix.ps1 -CertificatePath C:\certs\app.pfx -CertificatePassword "secret" -Publisher "CN=Acme"
#   powershell -File Scripts/build-msix.ps1 -AppInstaller -InstallUrl "https://example.com/msix/"
#   powershell -File Scripts/build-msix.ps1 -StoreUpload -CertificatePath C:\certs\app.pfx -Publisher "CN=Acme"
#
# Pre-flight checks without the SDK or a publish (placeholder Publisher,
# version quad, manifest, tile source):
#
#   powershell -File Scripts/build-msix.ps1 -Validate -Publisher "CN=Acme"
#
# Needs the Windows SDK (makeappx; signtool for signing). DryRun validates
# everything short of makeappx (including .appinstaller XML emission), so
# template/CI edits can be checked anywhere.
# Publisher MUST match the signing certificate subject. Unsigned packages
# validate the pipeline but cannot be installed (sign them, even self-signed).
# Velopack users: MSIX replaces the Velopack installer, not the app — the
# in-app update UI reports "not installed" under MSIX by design.
# -AppInstaller emits a 2021-schema .appinstaller feed file next to the
# package (native on-launch + background updates for sideloaded installs).
# -StoreUpload bundles the package and wraps it as .msixupload (Store
# submission format; the Store signs it — no cert needed for that path).

param(
    [string]$Version = "",
    [ValidateSet("x64", "arm64")][string]$Platform = "x64",
    [string]$Publisher = "CN=DevTem",
    [string]$CertificatePath = "",
    [string]$CertificatePassword = "",
    [string]$TimestampUrl = "http://timestamp.digicert.com",
    [string]$OutputDir = "",
    [switch]$DryRun,
    [switch]$AppInstaller,
    [string]$InstallUrl = "",
    [int]$HoursBetweenUpdateChecks = 12,
    [switch]$StoreUpload,
    [switch]$Validate
)

$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$csproj = Join-Path $projectRoot "DevTemWinUi3.csproj"
$manifestSrc = Join-Path $projectRoot "Packaging\Msix\Package.appxmanifest"
$logoSrc = Join-Path $projectRoot "Assets\Logo.png"
if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $projectRoot "Releases\Msix"
}

$rid = if ($Platform -eq "arm64") { "win-arm64" } else { "win-x64" }
$arch = $Platform

function Get-AppVersion {
    [xml]$xml = Get-Content -LiteralPath $csproj
    foreach ($group in $xml.Project.PropertyGroup) {
        if (-not [string]::IsNullOrWhiteSpace($group.Version)) {
            $v = $group.Version -replace '-.*$', ''
            $parts = @($v.Split('.') | ForEach-Object { $_ })
            while ($parts.Count -lt 4) { $parts += "0" }
            foreach ($p in $parts) {
                $n = 0
                if (-not [int]::TryParse($p, [ref]$n) -or $n -lt 0 -or $n -gt 65534) {
                    throw "Version part '$p' is not valid for MSIX (0-65534 per quad)."
                }
            }
            return ($parts[0..3] -join ".")
        }
    }
    throw "No <Version> found in $csproj."
}

function Get-AssemblyName {
    [xml]$xml = Get-Content -LiteralPath $csproj
    foreach ($group in $xml.Project.PropertyGroup) {
        if (-not [string]::IsNullOrWhiteSpace($group.AssemblyName)) {
            return $group.AssemblyName
        }
    }
    throw "No <AssemblyName> found in $csproj."
}

function New-TileArt([string]$source, [string]$destDir) {
    Add-Type -AssemblyName System.Drawing
    $specs = @(
        @("StoreLogo.png", 50, 50),
        @("Square44x44Logo.png", 44, 44),
        @("Square150x150Logo.png", 150, 150),
        @("Wide310x150Logo.png", 310, 150),
        @("SplashScreen.png", 620, 300)
    )
    if (-not (Test-Path -LiteralPath $destDir)) {
        New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    }
    $src = [System.Drawing.Bitmap]::FromFile($source)
    try {
        foreach ($spec in $specs) {
            $bmp = New-Object System.Drawing.Bitmap($spec[1], $spec[2])
            try {
                $g = [System.Drawing.Graphics]::FromImage($bmp)
                try {
                    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                    $g.Clear([System.Drawing.Color]::Transparent)
                    $g.DrawImage($src, 0, 0, $spec[1], $spec[2])
                }
                finally { $g.Dispose() }
                $bmp.Save((Join-Path $destDir $spec[0]), [System.Drawing.Imaging.ImageFormat]::Png)
            }
            finally { $bmp.Dispose() }
        }
    }
    finally { $src.Dispose() }
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = Get-AppVersion
}
Write-Host "MSIX version: $Version ($arch), publisher: $Publisher"

foreach ($path in @($csproj, $manifestSrc, $logoSrc)) {
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing required file: $path" }
}

# Pre-flight validation: fast, no SDK, no publish. Fails the run on
# placeholder identity and bad versions; tag comparison is best-effort
# (warns when git or tags are unavailable, e.g. fresh scaffolds).
if ($Validate) {
    if ($Publisher -eq "CN=DevTem") {
        throw "Publisher is still the CN=DevTem placeholder. Pass -Publisher with your Partner Center Publisher ID (Store) or signing cert subject (sideload)."
    }
    $quad = $Version -replace '-.*$', ''
    $quadParts = @($quad.Split('.'))
    while ($quadParts.Count -lt 4) { $quadParts += "0" }
    foreach ($p in $quadParts) {
        $n = 0
        if (-not [int]::TryParse($p, [ref]$n) -or $n -lt 0 -or $n -gt 65534) {
            throw "Version part '$p' is not valid for MSIX (0-65534 per quad)."
        }
    }
    $quad = ($quadParts[0..3] -join ".")
    Write-Host "Version quad OK: $quad"
    [xml]$checkManifest = Get-Content -LiteralPath $manifestSrc
    if ([string]::IsNullOrWhiteSpace($checkManifest.Package.Identity.Name)) { throw "Manifest Identity Name is empty." }
    if ([string]::IsNullOrWhiteSpace($checkManifest.Package.Identity.Publisher)) { throw "Manifest Identity Publisher is empty." }
    Write-Host "Manifest OK: $($checkManifest.Package.Identity.Name) / $($checkManifest.Package.Identity.Publisher)"
    $tagError = ""
    try {
        $tags = @(git tag --list "v*" 2>$null)
        $max = $null
        foreach ($t in $tags) {
            $m = [regex]::Match($t, '^v?(\d+)\.(\d+)\.(\d+)')
            if (-not $m.Success) { continue }
            $cand = [Version]"$($m.Groups[1]).$($m.Groups[2]).$($m.Groups[3]).0"
            if ($max -eq $null -or $cand -gt $max) { $max = $cand }
        }
        if ($max -ne $null) {
            if ([Version]"$quad" -le $max) {
                throw "Version quad $quad does not increase over latest tag ($max). Bump it first (Scripts/bump-version.ps1)."
            }
            Write-Host "Version increases over latest tag ($max)."
        }
        else { Write-Host "WARNING: no v* tags found, skipping increase check." -ForegroundColor Yellow }
    }
    catch {
        $tagError = $_.Exception.Message
    }
    if ($tagError -like "Version quad*") { throw $tagError }
    if (-not [string]::IsNullOrWhiteSpace($tagError)) {
        Write-Host "WARNING: tag comparison skipped ($tagError)" -ForegroundColor Yellow
    }
    Write-Host "Validate PASSED (publisher, version, manifest, tile source)." -ForegroundColor Green
    return
}

function Find-SdkTool([string]$exeName) {
    # The Windows SDK is never on PATH (neither locally nor on CI runners):
    # probe PATH first, then every installed Kits version (newest first).
    $found = Get-Command "$exeName.exe" -ErrorAction SilentlyContinue
    if ($found) { return $found.Source }
    $kitsRoots = @(
        (Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin"),
        (Join-Path $env:ProgramFiles "Windows Kits\10\bin")
    )
    foreach ($kits in $kitsRoots) {
        if (-not (Test-Path -LiteralPath $kits)) { continue }
        $candidate = Get-ChildItem -LiteralPath $kits -Directory -ErrorAction SilentlyContinue |
            Sort-Object Name -Descending |
            ForEach-Object { Join-Path (Join-Path $_.FullName "x64") "$exeName.exe" } |
            Where-Object { Test-Path -LiteralPath $_ } |
            Select-Object -First 1
        if ($candidate) { return $candidate }
    }
    return $null
}

$makeappx = Find-SdkTool "makeappx"
if (-not $makeappx -and -not $DryRun) {
    throw "makeappx.exe not found. Install the Windows SDK (or run with -DryRun to validate staging only)."
}

$staging = Join-Path ([System.IO.Path]::GetTempPath()) ("msix-stage-" + [System.Guid]::NewGuid().ToString("N"))
$publishDir = Join-Path $staging "publish"
$appDir = Join-Path $staging "app"
try {
    Write-Host "==> dotnet publish ($rid)"
    & dotnet publish $csproj -c Release -p:Platform=x64 -r $rid --self-contained -o $publishDir
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit $LASTEXITCODE)." }

    $exeName = (Get-AssemblyName) + ".exe"
    $exe = Get-ChildItem -LiteralPath $publishDir -Filter $exeName -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $exe) { throw "Main exe '$exeName' not found in publish output." }
    Write-Host "Main exe: $($exe.Name)"

    if (-not (Test-Path -LiteralPath $appDir)) {
        New-Item -ItemType Directory -Path $appDir -Force | Out-Null
    }
    Copy-Item -Path (Join-Path $publishDir "*") -Destination $appDir -Recurse -Force
    New-TileArt $logoSrc (Join-Path $appDir "Assets")

    [xml]$manifest = Get-Content -LiteralPath $manifestSrc
    $manifest.Package.Identity.Version = $Version
    $manifest.Package.Identity.ProcessorArchitecture = $arch
    $manifest.Package.Identity.Publisher = $Publisher
    $manifest.Save((Join-Path $appDir "AppxManifest.xml"))

    # Staging validation (also the whole point of -DryRun).
    $required = @("AppxManifest.xml", $exe.Name,
        "Assets\StoreLogo.png", "Assets\Square44x44Logo.png",
        "Assets\Square150x150Logo.png", "Assets\Wide310x150Logo.png",
        "Assets\SplashScreen.png")
    foreach ($rel in $required) {
        if (-not (Test-Path -LiteralPath (Join-Path $appDir $rel))) {
            throw "Staging incomplete, missing: $rel"
        }
    }
    try { [xml](Get-Content -LiteralPath (Join-Path $appDir "AppxManifest.xml")) | Out-Null }
    catch { throw "Staged manifest is not well-formed XML: $_" }
    Write-Host "Staging valid: $appDir"

    # .appinstaller feed content is fully determined at stage time (manifest
    # + version/arch/publisher + deterministic package file name), so it is
    # built and validated here — DryRun proves it without makeappx.
    $msixFileName = "DevTemWinUi3_" + $Version + "_" + $arch + ".msix"
    $installerXml = $null
    $installerFileName = [System.IO.Path]::GetFileNameWithoutExtension($msixFileName) + ".appinstaller"
    if ($AppInstaller) {
        $feedBase = $InstallUrl
        if ([string]::IsNullOrWhiteSpace($feedBase)) {
            if ($DryRun) { $feedBase = "https://example.com/msix/" }
            else { throw "-AppInstaller needs -InstallUrl (public base URL hosting the .msix, trailing slash added if missing)." }
        }
        if (-not $feedBase.EndsWith("/")) { $feedBase += "/" }
        [xml]$staged = Get-Content -LiteralPath (Join-Path $appDir "AppxManifest.xml")
        $pkgName = $staged.Package.Identity.Name
        if ([string]::IsNullOrWhiteSpace($pkgName)) { throw "Staged manifest has no Identity Name." }
        $mainUri = $feedBase + $msixFileName
        # Pre-escape (plain $vars below): subexpressions inside
        # expandable strings trip the 5.1 parser.
        $escUri = [System.Security.SecurityElement]::Escape($mainUri)
        $escVer = [System.Security.SecurityElement]::Escape($Version)
        $escName = [System.Security.SecurityElement]::Escape($pkgName)
        $escPub = [System.Security.SecurityElement]::Escape($Publisher)
        $escArch = [System.Security.SecurityElement]::Escape($arch)
        $installerXml = @"
<?xml version="1.0" encoding="utf-8"?>
<AppInstaller Uri="$escUri" Version="$escVer" xmlns="http://schemas.microsoft.com/appx/appinstaller/2021">
  <MainPackage Name="$escName" Publisher="$escPub" Version="$escVer" ProcessorArchitecture="$escArch" Uri="$escUri" />
  <UpdateSettings>
    <OnLaunch HoursBetweenUpdateChecks="$HoursBetweenUpdateChecks" ShowPrompt="true" UpdateBlocksActivation="false" />
    <AutomaticBackgroundTask />
  </UpdateSettings>
</AppInstaller>
"@
        # Here-strings start with a newline: strict XML parsers reject any
        # content before the declaration, so trim (then validate + write
        # BOM-free UTF-8 below).
        $installerXml = $installerXml.TrimStart()
        try { [xml]$installerXml | Out-Null }
        catch { throw "Generated .appinstaller is not well-formed XML: $_" }
        Write-Host "AppInstaller feed valid ($installerFileName -> $mainUri)"
    }

    if ($StoreUpload -and $DryRun) {
        $dryBase = [System.IO.Path]::GetFileNameWithoutExtension($msixFileName)
        Write-Host "DryRun: would bundle $dryBase.msixbundle and wrap .msixupload (needs makeappx)."
    }

    if ($DryRun) {
        Write-Host "DryRun: skipping makeappx (and signing). Staging kept at: $appDir"
        $appDir = $null  # keep staging for inspection
        return
    }

    if (-not (Test-Path -LiteralPath $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    }
    $msix = Join-Path $OutputDir $msixFileName
    Write-Host "==> makeappx pack"
    & $makeappx pack /d $appDir /p $msix /nv | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "makeappx failed (exit $LASTEXITCODE)." }
    Write-Host "Packed: $msix"

    if (-not [string]::IsNullOrWhiteSpace($CertificatePath)) {
        $signtool = Find-SdkTool "signtool"
        if (-not $signtool) { throw "signtool.exe not found (Windows SDK required for signing)." }
        $signArgs = @("sign", "/fd", "SHA256", "/f", $CertificatePath, "/tr", $TimestampUrl, "/td", "SHA256")
        if (-not [string]::IsNullOrWhiteSpace($CertificatePassword)) {
            $signArgs += @("/p", $CertificatePassword)
        }
        $signArgs += $msix
        Write-Host "==> signtool sign"
        & $signtool @signArgs | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "signtool failed (exit $LASTEXITCODE)." }
        Write-Host "Signed: $msix"
    }
    else {
        Write-Host "WARNING: unsigned package (installable only after signing, even self-signed)." -ForegroundColor Yellow
    }

    if ($AppInstaller -and -not $DryRun) {
        if (-not (Test-Path -LiteralPath $OutputDir)) {
            New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
        }
        $installerPath = Join-Path $OutputDir $installerFileName
        [System.IO.File]::WriteAllText($installerPath, $installerXml)
        Write-Host "Wrote: $installerPath"
    }

    if ($StoreUpload -and -not $DryRun) {
        $bundleBase = [System.IO.Path]::GetFileNameWithoutExtension($msixFileName)
        $msixLeaf = [System.IO.Path]::GetFileName($msix)
        $bundleDir = Join-Path $staging "bundle"
        if (-not (Test-Path -LiteralPath $bundleDir)) {
            New-Item -ItemType Directory -Path $bundleDir -Force | Out-Null
        }
        Copy-Item -LiteralPath $msix -Destination (Join-Path $bundleDir $msixLeaf) -Force
        $bundle = Join-Path $OutputDir ($bundleBase + ".msixbundle")
        Write-Host "==> makeappx bundle"
        & $makeappx bundle /d $bundleDir /p $bundle | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "makeappx bundle failed (exit $LASTEXITCODE)." }
        Write-Host "Bundled: $bundle"
        if (-not [string]::IsNullOrWhiteSpace($CertificatePath)) {
            Write-Host "==> signtool sign (bundle)"
            $bundleSignArgs = @("sign", "/fd", "SHA256", "/f", $CertificatePath, "/tr", $TimestampUrl, "/td", "SHA256")
            if (-not [string]::IsNullOrWhiteSpace($CertificatePassword)) {
                $bundleSignArgs += @("/p", $CertificatePassword)
            }
            $bundleSignArgs += $bundle
            & $signtool @bundleSignArgs | Out-Null
            if ($LASTEXITCODE -ne 0) { throw "signtool (bundle) failed (exit $LASTEXITCODE)." }
            Write-Host "Signed: $bundle"
        }
        $upload = Join-Path $OutputDir ($bundleBase + ".msixupload")
        if (Test-Path -LiteralPath $upload) { Remove-Item -LiteralPath $upload -Force }
        Compress-Archive -LiteralPath $bundle -DestinationPath $upload
        Write-Host "Store upload wrapped: $upload (bundle only - the Store signs it; add .appxsym before zipping if you ship symbols)"
    }
}
finally {
    if ($appDir -and (Test-Path -LiteralPath (Split-Path $appDir))) {
        Remove-Item -LiteralPath (Split-Path $appDir) -Recurse -Force -ErrorAction SilentlyContinue
    }
}
