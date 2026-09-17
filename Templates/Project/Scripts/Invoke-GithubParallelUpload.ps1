# Uploads release assets to a GitHub release in PARALLEL background jobs,
# then publishes it. Used by build-and-release.ps1 (CI + local) and
# upload-github.ps1 (local top-up of an already-packed folder).
#
# Why parallel: single-file uploads to GitHub crawl at ~2-3 Mbps from any
# machine (measured with both vpk and gh), so four serial ~270MB assets
# take ~1h and trip step timeouts; parallel lands in ~15 min with
# per-file progress in the log. vpk's own uploader is serial, hence gh.
#
#   powershell -File Scripts/Invoke-GithubParallelUpload.ps1 `
#     -Tag v0.0.4-beta -Title "DevTem-WinUI 3 0.0.4-beta" `
#     -Files @("Releases/a.nupkg", "Releases/b.zip") -Publish
#
# Requires: gh CLI logged in (or GITHUB_TOKEN set), an existing or new tag.
# Re-runnable: existing assets are overwritten (--clobber), missing release
# is created as a draft first. Never throws without a clear message.

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)]
    [string]$Tag,
    [Parameter(Mandatory = $true)]
    [string]$Title,
    [Parameter(Mandatory = $true)]
    [string[]]$Files,
    [string]$NotesFile = "",
    [string]$Repo = "Fettah010/winui-3-easy-template",
    [switch]$Publish,
    [switch]$PreRelease
)

$ErrorActionPreference = "Stop"

if ($Files.Count -eq 0) { throw "No files to upload." }
foreach ($file in $Files) {
    if (-not (Test-Path -LiteralPath $file)) { throw "Asset not found: $file" }
}

$haveGh = $false
try { & gh --version | Out-Null; if ($LASTEXITCODE -eq 0) { $haveGh = $true } } catch { }
if (-not $haveGh) { throw "gh CLI not found. Install it: https://cli.github.com/" }

# Draft-first: assets can only land on an existing release.
# Every gh call carries -R: background jobs start outside any checkout,
# where gh cannot resolve the repo from git remotes.
$releaseExists = $true
try {
    & gh release view $Tag --repo $Repo --json tagName 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) { $releaseExists = $false }
}
catch { $releaseExists = $false }

if (-not $releaseExists) {
    if ($PSCmdlet.ShouldProcess($Tag, "Create draft release")) {
        $createArgs = @("release", "create", $Tag, "--repo", $Repo, "--draft", "--title", $Title)
        if ($PreRelease) { $createArgs += "--prerelease" }
        if (-not [string]::IsNullOrWhiteSpace($NotesFile) -and (Test-Path -LiteralPath $NotesFile)) {
            $createArgs += @("--notes-file", $NotesFile)
        }
        & gh @createArgs
        if ($LASTEXITCODE -ne 0) { throw "gh release create failed for $Tag." }
        Write-Host "Created draft release $Tag."
    }
}
elseif ($PreRelease) {
    if ($PSCmdlet.ShouldProcess($Tag, "Mark pre-release")) {
        & gh release edit $Tag --repo $Repo --prerelease
        if ($LASTEXITCODE -ne 0) { throw "gh release edit --prerelease failed for $Tag." }
    }
}

# One background job per file (Start-Job works on PS 5.1 and 7 alike).
# Jobs inherit the environment, so GITHUB_TOKEN flows to gh automatically.
Write-Host "Uploading $($Files.Count) asset(s) in parallel to $Tag ..."
$jobs = @()
foreach ($file in $Files) {
    $item = Get-Item -LiteralPath $file
    $mb = [math]::Round($item.Length / 1MB, 1)
    Write-Host "  queue: $($item.Name) ($mb MB)"
    if ($PSCmdlet.ShouldProcess($item.FullName, "Upload to $Tag")) {
        $jobs += Start-Job -Name $item.Name -ScriptBlock {
            param($jobTag, $jobRepo, $assetPath)
            & gh release upload $jobTag $assetPath --repo $jobRepo --clobber 2>&1
            if ($LASTEXITCODE -ne 0) { throw "gh release upload failed for $assetPath." }
        } -ArgumentList @($Tag, $Repo, $item.FullName)
    }
}

$failed = @()
foreach ($job in $jobs) {
    $null = Wait-Job $job
    # Receive-Job rethrows a failed job's exception: catch it so the log
    # keeps every job's output and the failure list stays complete.
    $lines = @()
    try { $lines = @(Receive-Job $job 2>&1) }
    catch { $lines = @("job error: " + (($_.Exception.Message | Out-String).Trim())) }
    foreach ($line in $lines) { Write-Host ("[" + $job.Name + "] " + $line) }
    if ($job.State -ne "Completed") { $failed += $job.Name }
    try { Remove-Job $job -Force -ErrorAction SilentlyContinue } catch { }
}
if ($failed.Count -gt 0) { throw ("Parallel upload failed for: " + ($failed -join ", ")) }
Write-Host ("All " + $Files.Count + " asset(s) uploaded to $Tag.")

if ($Publish) {
    if ($PSCmdlet.ShouldProcess($Tag, "Publish release (un-draft)")) {
        & gh release edit $Tag --draft=false
        if ($LASTEXITCODE -ne 0) { throw "gh release edit --draft=false failed for $Tag." }
        Write-Host "Published $Tag."
    }
}
