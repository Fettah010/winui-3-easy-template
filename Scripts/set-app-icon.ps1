# Regenerates every app icon asset from one square source PNG.
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File Scripts\set-app-icon.ps1 -Source C:\art\logo.png
#   powershell -File Scripts\set-app-icon.ps1 -Source C:\art\logo.png -WhatIf
#
# Source requirements: square PNG, at least 512x512 (1024 recommended),
# transparent background, logo drawn inside ~80% safe margins.
#
# Produces (in Assets/ by default, or -AssetsDir for dry runs):
#   app.ico     multi-entry PNG-compressed ICO {16,24,32,48,64,128,256}
#               (taskbar, title bar, installer, tray, shortcuts)
#   app-light.ico / app-dark.ico  copies of app.ico (theme-aware tray/title
#               icons pick these when present, app.ico fallback otherwise;
#               re-render with theme-specific art when you have it)
#   Logo.png    256  (splash, Home hero, About)
#   Logo-64/48/32/16.png  exact-size copies (title bar, cards)
#
# Replaced files are backed up to .icon-backup\<timestamp>\ (git-ignored).
# MSIX tiles are NOT produced here: build-msix.ps1 renders them at pack time
# from Logo.png, so they follow automatically.
#
# Run in Windows PowerShell 5.1 (System.Drawing is built in). -WhatIf previews
# every write without touching disk.

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true, HelpMessage = "Square source PNG, >= 512px per side.")]
    [string]$Source,

    # NOTE: $PSScriptRoot is NOT visible in parameter defaults (they evaluate
    # in the caller's scope), so the default resolves in the body below.
    [string]$AssetsDir = ""
)

$ErrorActionPreference = "Stop"

if ($AssetsDir -eq "") { $AssetsDir = Join-Path $PSScriptRoot "..\Assets" }
$AssetsDir = (Resolve-Path $AssetsDir).Path
$MinSourceSize = 512
$IcoSizes = @(16, 24, 32, 48, 64, 128, 256)
$LogoSize = 256
$SmallLogos = @(64, 48, 32, 16)

try {
    Add-Type -AssemblyName System.Drawing
}
catch {
    throw "System.Drawing is unavailable. Run this script in Windows PowerShell 5.1."
}

if (-not (Test-Path -LiteralPath $Source)) {
    throw "Source not found: $Source"
}

$sourceImage = $null
try {
    $sourceImage = [System.Drawing.Image]::FromFile((Resolve-Path -LiteralPath $Source).Path)
    if ($sourceImage.Width -ne $sourceImage.Height) {
        throw "Source must be square, got $($sourceImage.Width)x$($sourceImage.Height)."
    }
    if ($sourceImage.Width -lt $MinSourceSize) {
        throw "Source must be at least ${MinSourceSize}x${MinSourceSize}, got $($sourceImage.Width)x$($sourceImage.Height). Upscaling would blur every asset."
    }
    Write-Host "Source: $($sourceImage.Width)x$($sourceImage.Height)" -ForegroundColor Green
}
catch {
    if ($null -ne $sourceImage) { $sourceImage.Dispose() }
    throw
}

function Resize-Image([System.Drawing.Image]$image, [int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = $null
    try {
        $graphics = [System.Drawing.Graphics]::FromImage($bmp)
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
        $graphics.DrawImage($image, 0, 0, $size, $size)
        return $bmp
    }
    catch {
        $bmp.Dispose()
        throw
    }
    finally {
        if ($null -ne $graphics) { $graphics.Dispose() }
    }
}

function Save-Png([System.Drawing.Bitmap]$bmp, [string]$path) {
    if ($PSCmdlet.ShouldProcess($path, "Write PNG")) {
        $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
}

# .NET cannot emit multi-size ICOs natively, so the directory is written by
# hand: ICONDIR + one ICONDIRENTRY per size, each pointing at PNG data.
# (Vista+ reads PNG-compressed entries; 256 is stored as byte 0.)
function Save-Ico([System.Drawing.Image]$image, [int[]]$sizes, [string]$path) {
    $pngBlobs = @()
    foreach ($size in $sizes) {
        $bmp = $null
        $stream = $null
        try {
            $bmp = Resize-Image $image $size
            $stream = New-Object System.IO.MemoryStream
            $bmp.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
            $pngBlobs += @{
                Size = $size
                Data = $stream.ToArray()
            }
        }
        finally {
            if ($null -ne $stream) { $stream.Dispose() }
            if ($null -ne $bmp) { $bmp.Dispose() }
        }
    }

    if ($PSCmdlet.ShouldProcess($path, "Write ICO ($($sizes -join ','))")) {
        $file = $null
        $writer = $null
        try {
            $file = [System.IO.File]::Create($path)
            $writer = New-Object System.IO.BinaryWriter($file)
            $writer.Write([uint16]0)              # reserved
            $writer.Write([uint16]1)              # type: ICO
            $writer.Write([uint16]$pngBlobs.Count)
            $offset = 6 + 16 * $pngBlobs.Count
            foreach ($blob in $pngBlobs) {
                $dim = if ($blob.Size -ge 256) { 0 } else { $blob.Size }
                $writer.Write([byte]$dim)         # width
                $writer.Write([byte]$dim)         # height
                $writer.Write([byte]0)            # palette
                $writer.Write([byte]0)            # reserved
                $writer.Write([uint16]1)          # planes
                $writer.Write([uint16]32)         # bit depth
                $writer.Write([uint32]$blob.Data.Length)
                $writer.Write([uint32]$offset)
                $offset += $blob.Data.Length
            }
            foreach ($blob in $pngBlobs) {
                $writer.Write($blob.Data)
            }
        }
        finally {
            if ($null -ne $writer) { $writer.Dispose() }
            if ($null -ne $file) { $file.Dispose() }
        }
    }
}

function Backup-File([string]$path, [string]$backupDir) {
    if (-not (Test-Path -LiteralPath $path)) {
        return
    }
    if ($PSCmdlet.ShouldProcess($path, "Back up to $backupDir")) {
        if (-not (Test-Path -LiteralPath $backupDir)) {
            New-Item -ItemType Directory -Path $backupDir | Out-Null
        }
        Copy-Item -LiteralPath $path -Destination (Join-Path $backupDir (Split-Path $path -Leaf))
    }
}

try {
    $targets = @(
        (Join-Path $AssetsDir "app.ico")
        (Join-Path $AssetsDir "app-light.ico")
        (Join-Path $AssetsDir "app-dark.ico")
        (Join-Path $AssetsDir "Logo.png")
    )
    foreach ($size in $SmallLogos) {
        $targets += (Join-Path $AssetsDir "Logo-$size.png")
    }

    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    # Backup sits next to the target Assets dir (repo root for real runs),
    # so -AssetsDir dry runs never touch the working tree.
    $backupDir = Join-Path (Split-Path $AssetsDir -Parent) ".icon-backup\$stamp"
    foreach ($target in $targets) {
        Backup-File $target $backupDir
    }

    Save-Ico $sourceImage $IcoSizes (Join-Path $AssetsDir "app.ico")
    foreach ($variant in @("app-light.ico", "app-dark.ico")) {
        if ($PSCmdlet.ShouldProcess((Join-Path $AssetsDir $variant), "Write ICO copy")) {
            Copy-Item -LiteralPath (Join-Path $AssetsDir "app.ico") -Destination (Join-Path $AssetsDir $variant) -Force
        }
    }

    $logo = $null
    try {
        $logo = Resize-Image $sourceImage $LogoSize
        Save-Png $logo (Join-Path $AssetsDir "Logo.png")
    }
    finally {
        if ($null -ne $logo) { $logo.Dispose() }
    }

    foreach ($size in $SmallLogos) {
        $small = $null
        try {
            $small = Resize-Image $sourceImage $size
            Save-Png $small (Join-Path $AssetsDir "Logo-$size.png")
        }
        finally {
            if ($null -ne $small) { $small.Dispose() }
        }
    }

    if (-not $WhatIfPreference) {
        # Read-back: every PNG at its exact size, every ICO entry present.
        $failures = @()
        $checks = @{ "Logo.png" = $LogoSize }
        foreach ($size in $SmallLogos) { $checks["Logo-$size.png"] = $size }
        foreach ($entry in $checks.GetEnumerator()) {
            $probe = $null
            try {
                $probe = [System.Drawing.Image]::FromFile((Join-Path $AssetsDir $entry.Key))
                if ($probe.Width -ne $entry.Value -or $probe.Height -ne $entry.Value) {
                    $failures += "$($entry.Key) is $($probe.Width)x$($probe.Height), expected $($entry.Value)x$($entry.Value)"
                }
            }
            finally {
                if ($null -ne $probe) { $probe.Dispose() }
            }
        }
        $icoBytes = [System.IO.File]::ReadAllBytes((Join-Path $AssetsDir "app.ico"))
        $icoCount = [BitConverter]::ToUInt16($icoBytes, 4)
        $icoSizes = @()
        for ($i = 0; $i -lt $icoCount; $i++) {
            $w = $icoBytes[6 + $i * 16]
            $icoSizes += if ($w -eq 0) { 256 } else { $w }
        }
        foreach ($size in $IcoSizes) {
            if ($icoSizes -notcontains $size) {
                $failures += "app.ico is missing the ${size}px entry (has $($icoSizes -join ','))"
            }
        }
        if ($failures.Count -gt 0) {
            throw "Verification failed:`n  $($failures -join "`n  ")"
        }
        Write-Host "Verified: app.ico [$($icoSizes -join ',')] + $($checks.Count) PNGs at exact sizes." -ForegroundColor Green
        Write-Host "Backup: $backupDir" -ForegroundColor DarkGray
        Write-Host "MSIX tiles render from Logo.png at pack time (build-msix.ps1); nothing else to update." -ForegroundColor DarkGray
    }
}
finally {
    $sourceImage.Dispose()
}
