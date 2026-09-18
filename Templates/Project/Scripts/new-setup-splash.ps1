# Renders the Velopack Setup.exe splash art for a release: a Windows 11
# light card (soft backdrop, inset rounded card, logo, name, version pill,
# channel + payload size, "Installing…" caption) sized 640x400. Velopack's
# installer chrome only exposes the splash image + progress-bar color, so
# every per-release fact (version, branch, size) is baked into the art at
# pack time by build-and-release.ps1 — the committed
# Assets/SetupSplash.png stays the timeless fallback (regenerate it with
# -Static after art changes).
#
#   powershell -File Scripts/new-setup-splash.ps1 -Version 0.0.16-beta -Channel beta -SizeMB 232
#   powershell -File Scripts/new-setup-splash.ps1 -Static -Output Assets/SetupSplash.png
#
# Needs Windows (System.Drawing) + Segoe UI, both present on release
# runners and dev machines. Never throws without a clear message.

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [string]$Channel = "beta",
    [double]$SizeMB = 0,
    [string]$Output = "",
    [switch]$Static
)

$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
if ([string]::IsNullOrWhiteSpace($Output)) {
    $Output = Join-Path $projectRoot "Releases\setup-splash.png"
}
$logoPath = Join-Path $projectRoot "Assets\Logo.png"
if (-not (Test-Path -LiteralPath $logoPath)) { throw "Logo not found: $logoPath" }

function New-RoundedPath([int]$x, [int]$y, [int]$w, [int]$h, [int]$r) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2
    $path.AddArc($x, $y, $d, $d, 180, 90)
    $path.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $path.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $path.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    return $path
}

$channelLabel = "Stable channel"
if ($Channel -eq "beta") { $channelLabel = "Beta channel" }

$metaLine = $channelLabel
if (-not $Static -and $SizeMB -gt 0) {
    # [char] escapes (not literals): Windows PowerShell 5.1 parses
    # scripts in the system codepage and mangles non-ASCII literals.
    $metaLine = $channelLabel + "  " + [char]0xB7 + "  ~" + ([math]::Round($SizeMB)) + " MB"
}

if ($PSCmdlet.ShouldProcess($Output, "Render setup splash ($Version)")) {
    $outDir = Split-Path -Parent $Output
    if (-not [string]::IsNullOrWhiteSpace($outDir) -and -not (Test-Path -LiteralPath $outDir)) {
        New-Item -ItemType Directory -Path $outDir -Force | Out-Null
    }

    Add-Type -AssemblyName System.Drawing
    $w = 640
    $h = 400
    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    try {
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        try {
            $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit

            # Soft Win11-light backdrop.
            $backRect = New-Object System.Drawing.Rectangle(0, 0, $w, $h)
            $back = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
                $backRect,
                [System.Drawing.Color]::FromArgb(251, 252, 254),
                [System.Drawing.Color]::FromArgb(230, 238, 247), 90)
            try { $g.FillRectangle($back, $backRect) } finally { $back.Dispose() }

            # Inset rounded card + soft drop shadow.
            $cardPath = New-RoundedPath 44 36 552 316 28
            try {
                $shadowPath = New-RoundedPath 44 42 552 316 28
                try {
                    $shadow = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(28, 0, 0, 0))
                    try { $g.FillPath($shadow, $shadowPath) } finally { $shadow.Dispose() }
                }
                finally { $shadowPath.Dispose() }
                $cardFill = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 255, 255))
                try { $g.FillPath($cardFill, $cardPath) } finally { $cardFill.Dispose() }
            }
            finally { $cardPath.Dispose() }

            # Logo, centered.
            $logo = [System.Drawing.Image]::FromFile($logoPath)
            try {
                $ls = 100
                $g.DrawImage($logo, [int](($w - $ls) / 2), 58, $ls, $ls)
            }
            finally { $logo.Dispose() }

            $fmt = New-Object System.Drawing.StringFormat
            try {
                $fmt.Alignment = [System.Drawing.StringAlignment]::Center
                $fmt.LineAlignment = [System.Drawing.StringAlignment]::Center
                $ink = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(27, 27, 27))
                $muted = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(97, 97, 97))
                $accent = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(0, 120, 212))
                $paper = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 255, 255))
                $nameFont = New-Object System.Drawing.Font("Segoe UI Semibold", 27)
                $pillFont = New-Object System.Drawing.Font("Segoe UI Semibold", 14)
                $metaFont = New-Object System.Drawing.Font("Segoe UI", 14)
                try {
                    $g.DrawString("DevTem-WinUI 3", $nameFont, $ink,
                        (New-Object System.Drawing.RectangleF(0, 168, $w, 44)), $fmt)
                    if (-not $Static) {
                        # Version pill, auto-width around the text.
                        $pillText = "v" + $Version
                        $pillSize = $g.MeasureString($pillText, $pillFont)
                        $pillW = [int]($pillSize.Width + 28)
                        $pillH = 30
                        $pillX = [int](($w - $pillW) / 2)
                        $pillY = 218
                        $pillPath = New-RoundedPath $pillX $pillY $pillW $pillH 15
                        try { $g.FillPath($accent, $pillPath) } finally { $pillPath.Dispose() }
                        $g.DrawString($pillText, $pillFont, $paper,
                            (New-Object System.Drawing.RectangleF(0, $pillY, $w, $pillH)), $fmt)
                        $g.DrawString($metaLine, $metaFont, $muted,
                            (New-Object System.Drawing.RectangleF(0, 254, $w, 28)), $fmt)
                    }
                    # Progress caption above the bar Velopack overlays at the
                    # bottom; the strip below it stays empty background.
                    $g.DrawString("Installing" + [char]0x2026, $metaFont, $muted,
                        (New-Object System.Drawing.RectangleF(0, 296, $w, 28)), $fmt)
                }
                finally {
                    $ink.Dispose(); $muted.Dispose(); $accent.Dispose(); $paper.Dispose()
                    $nameFont.Dispose(); $pillFont.Dispose(); $metaFont.Dispose()
                }
            }
            finally { $fmt.Dispose() }
        }
        finally { $g.Dispose() }
        $bmp.Save($Output, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $bmp.Dispose() }
    Write-Host "Setup splash rendered: $Output"
}
