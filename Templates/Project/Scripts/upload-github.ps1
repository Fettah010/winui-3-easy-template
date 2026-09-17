# ------------------------------------------------------------------------------
# Upload an already-packed Releases/ folder to GitHub Releases.
# Use this when you packed locally (vpk pack) and just want to ship it.
#
#   .\upload-github.ps1 -Channel stable
#
# Requires: $env:GITHUB_TOKEN (PAT with repo-scope), gh CLI.
# Uploads in parallel (see Invoke-GithubParallelUpload.ps1): serial
# uploads of ~270MB assets crawl at ~2-3 Mbps (~1h); parallel lands in
# ~15 min.
# ------------------------------------------------------------------------------

param(
    [string]$Channel = "stable",
    [switch]$Publish,       # flip switch: create as published release instead of draft
    [switch]$PreRelease,
    [string]$Tag = ""       # release tag (default: derived from the newest full nupkg)
)

$ErrorActionPreference = "Stop"

# ──────────────────────────────────────────────────────────────────────────────
#  PROJECT SETTINGS — the repo that hosts releases on GitHub.
# ──────────────────────────────────────────────────────────────────────────────
$GithubRepoUrl = "https://github.com/Fettah010/winui-3-easy-template"
$GithubToken   = $env:GITHUB_TOKEN      # <- export GITHUB_TOKEN=<token> first
# ──────────────────────────────────────────────────────────────────────────────

$ProjectRoot  = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$ReleasesDir  = Join-Path $ProjectRoot "Releases"

if ([string]::IsNullOrWhiteSpace($GithubToken)) {
    throw "GITHUB_TOKEN is not set. Run: `$env:GITHUB_TOKEN = 'ghp_...'"
}
$env:GITHUB_TOKEN = $GithubToken
if (-not (Test-Path $ReleasesDir)) {
    throw "No Releases folder found at $ReleasesDir. Run .\build-and-release.ps1 -SkipBuild first."
}

# Everything shippable in the folder (nupkgs, setup, portable, feed json).
$files = @(Get-ChildItem -LiteralPath $ReleasesDir -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -like "*.nupkg" -or $_.Name -like "*Setup.exe" -or $_.Name -like "*Portable.zip" -or $_.Name -like "releases.*.json" } |
    ForEach-Object { $_.FullName })
if ($files.Count -eq 0) { throw "No release assets found under $ReleasesDir." }

if ([string]::IsNullOrWhiteSpace($Tag)) {
    $full = @($files | Where-Object { $_ -like "*-full.nupkg" } | Select-Object -First 1)
    if ($full.Count -eq 0) { throw "Cannot derive the tag: no *-full.nupkg under $ReleasesDir. Pass -Tag explicitly." }
    $base = [System.IO.Path]::GetFileNameWithoutExtension($full[0])
    # DevTemWinUi3-<version>-<channel>-full -> v<version>
    $parts = $base -split "-"
    if ($parts.Count -lt 4) { throw "Cannot parse a version from $base. Pass -Tag explicitly." }
    $Tag = "v" + ($parts[1..($parts.Count - 3)] -join "-")
    Write-Host "Derived tag $Tag from $base."
}

$uploadArgs = @{
    Tag = $Tag
    Title = "DevTem-WinUI 3 $Tag"
    Files = $files
    Repo = ($GithubRepoUrl -replace "^https://github\.com/", "")
}
if ($Publish)    { $uploadArgs["Publish"] = $true }
if ($PreRelease) { $uploadArgs["PreRelease"] = $true }

Write-Host "==> Uploading $($files.Count) asset(s) from $ReleasesDir to $GithubRepoUrl (channel: $Channel)" -ForegroundColor Cyan
& (Join-Path $PSScriptRoot "Invoke-GithubParallelUpload.ps1") @uploadArgs
if ($LASTEXITCODE -ne 0) { throw "Parallel upload failed with exit code $LASTEXITCODE" }

Write-Host "==> Done." -ForegroundColor Green