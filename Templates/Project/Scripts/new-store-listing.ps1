# Generates Partner Center listing drafts from the repo README:
# Store/Listing/description.txt (edit, then paste into the submission),
# screenshots-checklist.md, and submission-checklist.md. Generated output
# is git-ignored (like Releases/); re-run after README changes.
#
#   powershell -File Scripts/new-store-listing.ps1
#   powershell -File Scripts/new-store-listing.ps1 -WhatIf

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$OutputDir = ""
)

$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$readme = Join-Path $projectRoot "README.md"
if (-not (Test-Path -LiteralPath $readme)) { throw "README.md not found at $readme." }
if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $projectRoot "Store\Listing"
}

$title = ""
$blurb = @()
$bullets = @()
$inFence = $false
foreach ($line in (Get-Content -LiteralPath $readme)) {
    $t = $line.Trim()
    if ($t -match '^\s*```') { $inFence = -not $inFence; continue }
    if ($inFence) { continue }
    if ([string]::IsNullOrWhiteSpace($title) -and $t -match '^#\s+(.+)') {
        $title = $Matches[1].Trim()
        continue
    }
    if ($blurb.Count -lt 3 -and $t -match '\S' -and $t -notmatch '^[#<>!\[`]' -and $t -notmatch '^\s*```') {
        $blurb += $t
        continue
    }
    if ($t -match '^-\s+(.+)') { $bullets += $Matches[1].Trim() }
}
if ([string]::IsNullOrWhiteSpace($title)) { $title = "My App" }

$desc = @()
$desc += $title
$desc += ""
foreach ($b in $blurb) { $desc += $b; $desc += "" }
if ($bullets.Count -gt 0) {
    $desc += "Features:"
    foreach ($b in $bullets) { $desc += "- " + $b }
    $desc += ""
}
$desc += "(Draft generated from README.md - edit before pasting into Partner Center. Limit: 10000 characters.)"
$description = $desc -join "`r`n"

$screenshots = @(
    "# Screenshots checklist",
    "",
    "- At least 1 screenshot (4+ recommended) per device family you target.",
    "- Minimum 1366x768; 1920x1080 or 2560x1440 preferred. PNG or JPG.",
    "- Show the real app (Home, Settings, Update Center) - no mockups, no",
    "  letterboxing, no marketing overlays covering UI.",
    "- Capture light AND dark theme once each if you support both.",
    "- File names: screenshot-01-home.png, screenshot-02-settings.png, ..."
)

$submission = @(
    "# Submission checklist (Partner Center > app > new submission)",
    "",
    "- [ ] Packages: upload the .msixupload from Releases/Msix.",
    "- [ ] Pricing and availability: price, markets, audience.",
    "- [ ] Properties: category, capabilities (justify each), age rating",
    "  questionnaire (answer for the actual content - updates change it).",
    "- [ ] Store listings: paste description.txt, upload screenshots.",
    "- [ ] Privacy policy URL (required for most categories).",
    "- [ ] Submit; certification takes up to ~3 business days."
)
$screenshotsText = $screenshots -join "`r`n"
$submissionText = $submission -join "`r`n"

if ($PSCmdlet.ShouldProcess($OutputDir, "Generate Store listing drafts")) {
    if (-not (Test-Path -LiteralPath $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    }
    Set-Content -LiteralPath (Join-Path $OutputDir "description.txt") -Value $description -Encoding UTF8
    Set-Content -LiteralPath (Join-Path $OutputDir "screenshots-checklist.md") -Value $screenshotsText -Encoding UTF8
    Set-Content -LiteralPath (Join-Path $OutputDir "submission-checklist.md") -Value $submissionText -Encoding UTF8
    Write-Host "Listing drafts written to $OutputDir (edit description.txt, then paste into Partner Center)."
}
