# Builds an MSIX package for sideloading or Store submission.
#
#   powershell -File Scripts/build-msix.ps1 -DryRun
#   powershell -File Scripts/build-msix.ps1
#   powershell -File Scripts/build-msix.ps1 -CertificatePath C:\certs\app.pfx -CertificatePassword "secret" -Publisher "CN=Acme"
#
# Needs the Windows SDK (makeappx; signtool for signing). DryRun validates
# everything short of makeappx, so template/CI edits can be checked anywhere.
# Publisher MUST match the signing certificate subject. Unsigned packages
# validate the pipeline but cannot be installed (sign them, even self-signed).
# Velopack users: MSIX replaces the Velopack installer, not the app — the
# in-app update UI reports "not installed" under MSIX by design.

param(
    [string]$Version = "",
    [ValidateSet("x64", "arm64")][string]$Platform = "x64",
    [string]$Publisher = "CN=DevTem",
    [string]$CertificatePath = "",
    [string]$CertificatePassword = "",
    [string]$TimestampUrl = "http://timestamp.digicert.com",
    [string]$OutputDir = "",
    [switch]$DryRun
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

    if ($DryRun) {
        Write-Host "DryRun: skipping makeappx (and signing). Staging kept at: $appDir"
        $appDir = $null  # keep staging for inspection
        return
    }

    if (-not (Test-Path -LiteralPath $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    }
    $msix = Join-Path $OutputDir ("DevTemWinUi3_" + $Version + "_" + $arch + ".msix")
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
}
finally {
    if ($appDir -and (Test-Path -LiteralPath (Split-Path $appDir))) {
        Remove-Item -LiteralPath (Split-Path $appDir) -Recurse -Force -ErrorAction SilentlyContinue
    }
}
