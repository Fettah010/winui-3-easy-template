# Scaffold matrix for the dotnet-new templates: installs both templates from
# this repo, scaffolds all-on / all-off / no-tray / no-updates / no-diagnostics,
# wires a page end-to-end inside all-on via add-page.ps1, then builds
# (0 warnings, 0 errors) and tests each scaffold. Fails the run on the first
# broken combo.
#
#   powershell -File Scripts/test-templates.ps1                 # full matrix
#   powershell -File Scripts/test-templates.ps1 -Combos allon,alloff
#   powershell -File Scripts/test-templates.ps1 -KeepTemp       # inspect output
#
# Same matrix runs in CI (.github/workflows/templates.yml).

param(
    [string[]]$Combos = @("allon", "alloff", "minimal", "desktop", "production", "notray", "noupd", "nohttp", "nodiag"),
    [switch]$KeepTemp
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("devtem-matrix-" + [System.Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null

$comboDefs = @{
    "allon"   = @{ Name = "AcmeDesk";    Safe = "AcmeDesk";    Display = "Acme Desk";  Company = "Acme"; Repo = "acme/desk-app"; Scheme = "acme://"; Flags = @(); Http = $true; Database = $true; Tray = $true; Updates = $true; Health = $true; Attribution = $true }
    "alloff"  = @{ Name = "123 Bare App"; Safe = "_Bare_App";   Display = "Bare App";   Company = "Bare"; Repo = "bare/app";      Scheme = "bare://"; Flags = @("--tray", "false", "--updates", "false", "--database", "false"); Http = $true; Database = $false; Tray = $false; Updates = $false; Health = $true; Attribution = $true }
    "notray"  = @{ Name = "NoTrayApp";   Safe = "NoTrayApp";    Display = "Noé Tray App"; Company = "Nt"; Repo = "nt/app";       Scheme = "nt://";   Flags = @("--tray", "false"); Http = $true; Database = $true; Tray = $false; Updates = $true; Health = $true; Attribution = $true }
    "noupd"   = @{ Name = "NoUpdApp";    Safe = "NoUpdApp";     Display = "NoUpd App";  Company = "Nu"; Repo = "nu/app";        Scheme = "nu://";   Flags = @("--updates", "false"); Http = $true; Database = $true; Tray = $true; Updates = $false; Health = $true; Attribution = $true }
    "nohttp"  = @{ Name = "NoHttpApp";   Safe = "NoHttpApp";    Display = "NoHttp App"; Company = "Nh"; Repo = "nh/app";        Scheme = "nh://";   Flags = @("--http", "false"); Http = $false; Database = $true; Tray = $true; Updates = $true; Health = $true; Attribution = $true }
    "nodiag"  = @{ Name = "NoDiagApp";   Safe = "NoDiagApp";    Display = "NoDiag App"; Company = "Nd"; Repo = "nd/app";        Scheme = "nd://";   Flags = @("--health", "false"); Http = $true; Database = $true; Tray = $true; Updates = $true; Health = $false; Attribution = $true }
    "minimal" = @{ Name = "MinimalProfile"; Safe = "MinimalProfile"; Display = "Minimal Profile"; Company = "Mp"; Repo = "mp/minimal"; Scheme = "minimal://"; Flags = @("--tray", "false", "--updates", "false", "--database", "false", "--http", "false", "--health", "false", "--attribution", "false"); Http = $false; Database = $false; Tray = $false; Updates = $false; Health = $false; Attribution = $false }
    "desktop" = @{ Name = "DesktopProfile"; Safe = "DesktopProfile"; Display = "Desktop Profile"; Company = "Dp"; Repo = "dp/desktop"; Scheme = "desktop://"; Flags = @("--updates", "false", "--database", "false", "--http", "false"); Http = $false; Database = $false; Tray = $true; Updates = $false; Health = $true; Attribution = $true }
    "production" = @{ Name = "ProductionProfile"; Safe = "ProductionProfile"; Display = "Production Profile"; Company = "Pp"; Repo = "pp/production"; Scheme = "production://"; Flags = @(); Http = $true; Database = $true; Tray = $true; Updates = $true; Health = $true; Attribution = $true }
}

$failed = 0

function Invoke-Step([string]$label, [scriptblock]$body) {
    Write-Host "`n==> $label" -ForegroundColor Cyan
    & $body
    if ($LASTEXITCODE -ne 0) {
        Write-Host "FAILED: $label (exit $LASTEXITCODE)" -ForegroundColor Red
        $script:failed++
        return $false
    }
    return $true
}

# Rename-engine proof: the scaffold must carry the requested scheme in every
# surface the engines touch (dotnet-new `scheme`/`schemeName` symbols).
function Assert-FeatureManifest([string]$outDir) {
    $manifestPath = Join-Path $outDir "docs\template-features.json"
    if (-not (Test-Path -LiteralPath $manifestPath)) {
        throw "feature manifest is missing"
    }
    $manifest = Get-Content -Raw $manifestPath | ConvertFrom-Json
    foreach ($name in @("tray", "updates", "database", "http", "health")) {
        if ($null -eq $manifest.features.$name) {
            throw "feature manifest is missing '$name'"
        }
        if ($null -eq $manifest.features.$name.files) {
            throw "feature manifest has no file list for '$name'"
        }
    }
    Write-Host "feature manifest OK"
}

function Assert-ProfileDocumentation([string]$outDir, [hashtable]$definition) {
    $readmePath = Join-Path $outDir "README.md"
    $readme = Get-Content -Raw $readmePath
    foreach ($marker in @("Starter profiles", "--tray false", "--updates false", "--database false", "--http false", "--health false")) {
        if ($readme -notmatch [regex]::Escape($marker)) {
            throw "generated README is missing profile marker '$marker'"
        }
    }
    $features = Get-Content -Raw (Join-Path $outDir "docs\FEATURES.md")
    foreach ($pair in @(
        @("System tray", $definition.Tray),
        @("Velopack updates", $definition.Updates),
        @("SQLite database", $definition.Database),
        @("Typed HTTP client", $definition.Http),
        @("Diagnostics page", $definition.Health),
        @("DevTem attribution", $definition.Attribution)
    )) {
        $expected = [string]$pair[1]
        if ($features -notmatch ([regex]::Escape("| " + $pair[0] + " | " + $expected + " |"))) {
            throw "feature summary does not report $($pair[0])=$expected"
        }
    }
    $guides = @{
        tray = $definition.Tray
        updates = $definition.Updates
        database = $definition.Database
        http = $definition.Http
        diagnostics = $definition.Health
    }
    foreach ($guide in $guides.GetEnumerator()) {
        $guidePath = Join-Path $outDir ("docs\feature-guides\" + $guide.Key + ".md")
        if ($guide.Value -ne (Test-Path -LiteralPath $guidePath)) {
            throw "feature guide presence is incorrect for $($guide.Key)"
        }
    }
    Write-Host "profile documentation OK"
}

function Assert-ScaffoldIdentity([string]$outDir, [hashtable]$definition) {
    $scheme = $definition.Scheme
    $safe = $definition.Safe
    $display = $definition.Display
    $repo = $definition.Repo
    $bare = $scheme.TrimEnd('/').TrimEnd(':')
    $manifest = Join-Path $outDir "Packaging\Msix\Package.appxmanifest"
    $manifestText = [System.IO.File]::ReadAllText($manifest)
    if ($manifestText -notmatch [regex]::Escape('Name="' + $bare + '"')) {
        throw "manifest protocol Name is not '$bare'"
    }

    if ($manifestText -notmatch [regex]::Escape("<DisplayName>$display</DisplayName>")) {
        throw "manifest display name is not '$display'"
    }
    $metadata = [System.IO.File]::ReadAllText((Join-Path $outDir "Services\Helpers\AppMetadata.cs"))
    if ($metadata -notmatch [regex]::Escape('"' + $scheme + '"')) {
        throw "AppMetadata protocol prefix is not '$scheme'"
    }
    if ($metadata -notmatch [regex]::Escape('SafeName = "' + $safe + '"')) {
        throw "AppMetadata safe name is not '$safe'"
    }
    if ($metadata -notmatch [regex]::Escape('AppName = "' + $display + '"')) {
        throw "AppMetadata display name is not '$display'"
    }
    if ($metadata -notmatch [regex]::Escape('RepoUrl = "https://github.com/' + $repo + '"')) {
        throw "AppMetadata repository is not '$repo'"
    }
    $csproj = [System.IO.File]::ReadAllText((Join-Path $outDir "$safe.csproj"))
    if ($csproj -notmatch [regex]::Escape("<RootNamespace>$safe</RootNamespace>")) {
        throw "RootNamespace is not '$safe'"
    }
    if ($csproj -notmatch [regex]::Escape("<AssemblyName>$safe</AssemblyName>")) {
        throw "AssemblyName is not '$safe'"
    }
    if ($metadata -notmatch [regex]::Escape('AppDataFolder = "' + $safe + '"')) {
        throw "settings folder is not '$safe'"
    }
    $apiPath = Join-Path $outDir "Services\ApiService.cs"
    $httpPropsPath = Join-Path $outDir "Build\Features.Http.props"
    if ($definition.Http) {
        if (-not (Test-Path -LiteralPath $apiPath) -or -not (Test-Path -LiteralPath $httpPropsPath)) {
            throw "HTTP feature files or package import are missing"
        }
    }
    elseif ((Test-Path -LiteralPath $apiPath) -or (Test-Path -LiteralPath $httpPropsPath)) {
        throw "HTTP feature files or package import remain disabled"
    }
    $dbPath = Join-Path $outDir "Services\DatabaseService.cs"
    $dbPropsPath = Join-Path $outDir "Build\Features.Database.props"
    if ($definition.Database) {
        if (-not (Test-Path -LiteralPath $dbPath) -or -not (Test-Path -LiteralPath $dbPropsPath)) {
            throw "database feature files or package import are missing"
        }
    }
    elseif ((Test-Path -LiteralPath $dbPath) -or (Test-Path -LiteralPath $dbPropsPath)) {
        throw "database feature files or package import remain disabled"
    }
    $diagPagePath = Join-Path $outDir "Pages\DiagnosticsPage.xaml"
    $diagCrashPath = Join-Path $outDir "Services\Native\CrashDialogNative.cs"
    $diagGuidePath = Join-Path $outDir "docs\feature-guides\diagnostics.md"
    if ($definition.Health) {
        if (-not (Test-Path -LiteralPath $diagPagePath) -or -not (Test-Path -LiteralPath $diagCrashPath)) {
            throw "diagnostics feature files are missing"
        }
    }
    elseif ((Test-Path -LiteralPath $diagPagePath) -or (Test-Path -LiteralPath $diagCrashPath) -or (Test-Path -LiteralPath $diagGuidePath)) {
        throw "diagnostics feature files remain when disabled"
    }
    Write-Host "identity OK ($safe / $display / $scheme / $repo)"
}

# init-template proof: re-brand a scratch copy of THIS repo and let the
# script's own leftover verification pass or fail the run (throws).
# Excludes build output, VCS, and generated dirs for copy speed.
function Invoke-InitTemplateScratchTest {
    $scratch = Join-Path ([System.IO.Path]::GetTempPath()) ("devtem-init-" + [System.Guid]::NewGuid().ToString("N"))
    try {
        $exclude = @("bin", "obj", ".git", ".vs", "TestResults", "Logs", "nupkgs", "Releases", ".icon-backup")
        $items = Get-ChildItem -LiteralPath $repoRoot -Force |
            Where-Object { $exclude -notcontains $_.Name }
        New-Item -ItemType Directory -Path $scratch -Force | Out-Null
        foreach ($item in $items) {
            Copy-Item -LiteralPath $item.FullName -Destination (Join-Path $scratch $item.Name) -Recurse -Force
        }
        & (Join-Path $scratch "Scripts\init-template.ps1") `
            -AppName "Renamed App" -Company "Renamed" `
            -RepoUrl "https://github.com/re/named" -Scheme "renamed+beta://"
        Write-Host "init-template scratch re-brand OK"
    }
    finally {
        Remove-Item -LiteralPath $scratch -Recurse -Force -ErrorAction SilentlyContinue
    }
}

try {
    # Clean slate, then install both templates from this repo.
    # (Uninstalls fail when nothing is installed — expected, ignored.)
    # The NuGet package name is uninstalled too: a stale DevTem.Templates
    # would otherwise shadow the path install at scaffold time (same
    # template identity) and silently test old content.
    try { & dotnet new uninstall DevTem.Templates 2>&1 | Out-Null } catch { }
    try { & dotnet new uninstall (Join-Path $repoRoot "Templates\Project") 2>&1 | Out-Null } catch { }
    try { & dotnet new uninstall (Join-Path $repoRoot "Templates\Page") 2>&1 | Out-Null } catch { }
    if (-not (Invoke-Step "install devtem-winui" { & dotnet new install (Join-Path $repoRoot "Templates\Project") })) { throw "install failed" }
    if (-not (Invoke-Step "install devtem-page" { & dotnet new install (Join-Path $repoRoot "Templates\Page") })) { throw "install failed" }

    foreach ($combo in $Combos) {
        if (-not $comboDefs.ContainsKey($combo)) { throw "Unknown combo: $combo (allon/alloff/minimal/desktop/production/notray/noupd/nohttp/nodiag)" }
        $c = $comboDefs[$combo]
        $outDir = Join-Path $tempRoot "$combo\$($c.Name)"

        $scaffoldArgs = @("devtem-winui", "-n", $c.Name, "--displayName", $c.Display,
            "--company", $c.Company, "--repo", $c.Repo, "--scheme", $c.Scheme,
            "--output", $outDir) + $c.Flags
        if (-not (Invoke-Step "scaffold $combo" { & dotnet new @scaffoldArgs })) { continue }

        # Rename-engine proof for this combo's scheme (cheap, fail fast).
        try { Assert-ScaffoldIdentity $outDir $c }
        catch {
            Write-Host "FAILED: identity $combo : $_" -ForegroundColor Red
            $failed++
            continue
        }
        try { Assert-FeatureManifest $outDir }
        catch {
            Write-Host "FAILED: feature manifest $combo : $_" -ForegroundColor Red
            $failed++
            continue
        }
        try { Assert-ProfileDocumentation $outDir $c }
        catch {
            Write-Host "FAILED: profile documentation $combo : $_" -ForegroundColor Red
            $failed++
            continue
        }

        # Item-template proof: add-page.ps1 wires a page end-to-end inside the
        # all-on app (scaffold + strings + DI + route/nav + its own build +
        # test). Not a git repo down here, so snapshot rollback applies.
        if ($combo -eq "allon") {
            if (-not (Invoke-Step "add-page smoke in allon" {
                    & (Join-Path $repoRoot "Scripts\add-page.ps1") -RepoRoot $outDir -TemplateSource (Join-Path $repoRoot "Templates\Page") -Name OrdersSmoke -Route smoke-orders -Title "Smoke Orders" -Icon Shop
                })) { continue }
            if (-not (Invoke-Step "add second page in allon" {
                    & (Join-Path $repoRoot "Scripts\add-page.ps1") -RepoRoot $outDir -TemplateSource (Join-Path $repoRoot "Templates\Page") -Name ReportsSmoke -Route smoke-reports -Title "Smoke Reports" -Icon Calendar
                })) { continue }
            Write-Host "`n==> duplicate-route rejection" -ForegroundColor Cyan
                $duplicateOut = ""
                $duplicateFailed = $false
                try {
                    $duplicateOut = & (Join-Path $repoRoot "Scripts\add-page.ps1") -RepoRoot $outDir -TemplateSource (Join-Path $repoRoot "Templates\Page") -Name DuplicateOrders -Route smoke-orders -Title "Duplicate Orders" -Icon Calendar 2>&1 | Out-String
                }
                catch {
                    $duplicateFailed = $true
                    $duplicateOut += $_.Exception.Message
                }
                if (-not $duplicateFailed -or ($duplicateOut -notmatch "already registered")) {
                    Write-Host "FAILED: duplicate route was not rejected" -ForegroundColor Red
                    $failed++
                    continue
            }
            if (Test-Path -LiteralPath (Join-Path $outDir "Pages\DuplicateOrdersPage.xaml")) {
                Write-Host "FAILED: duplicate route rollback left generated files" -ForegroundColor Red
                $failed++
                continue
            }
            Write-Host "duplicate route rejected and rolled back" -ForegroundColor Green
        }

        $buildLog = ""
        $buildCode = 0
        Push-Location $outDir
        try {
            $buildLog = & dotnet build -c Debug -p:Platform=x64 --no-incremental -v q 2>&1 | Out-String
            $buildCode = $LASTEXITCODE
        }
        finally { Pop-Location }
        if ($buildCode -ne 0) {
            Write-Host "FAILED: build $combo (exit $buildCode)" -ForegroundColor Red
            Write-Host $buildLog
            $failed++
            continue
        }
        $warn = ([regex]::Matches($buildLog, '(\d+) Warning\(s\)') | Select-Object -Last 1).Groups[1].Value
        $err = ([regex]::Matches($buildLog, '(\d+) Error\(s\)') | Select-Object -Last 1).Groups[1].Value
        if ($warn -ne "0" -or $err -ne "0") {
            Write-Host "FAILED: $combo has $warn warning(s), $err error(s) (must be 0/0)" -ForegroundColor Red
            $failed++
            continue
        }
        Write-Host "build ${combo}: 0 warnings, 0 errors" -ForegroundColor Green

        $testOut = & dotnet test (Join-Path $outDir "Tests\$($c.Safe).Tests.csproj") -c Debug -p:Platform=x64 --nologo -v q 2>&1 | Out-String
        if ($LASTEXITCODE -ne 0 -or $testOut -notmatch 'Passed!' -or $testOut -match 'Failed:\s+[1-9]') {
            Write-Host "FAILED: tests for $combo" -ForegroundColor Red
            Write-Host $testOut
            $failed++
            continue
        }
        Write-Host "PASSED: $combo" -ForegroundColor Green
    }

    # init-template proof (all combos green or not, this is independent):
    # re-brand a scratch repo copy; the script's own leftover check decides.
    Write-Host "`n==> init-template scratch" -ForegroundColor Cyan
    try {
        Invoke-InitTemplateScratchTest | Out-Null
        Write-Host "PASSED: init-template scratch" -ForegroundColor Green
    }
    catch {
        Write-Host "FAILED: init-template scratch: $_" -ForegroundColor Red
        $failed++
    }
}
finally {
    try { & dotnet new uninstall (Join-Path $repoRoot "Templates\Project") 2>&1 | Out-Null } catch { }
    try { & dotnet new uninstall (Join-Path $repoRoot "Templates\Page") 2>&1 | Out-Null } catch { }
    if (-not $KeepTemp -and (Test-Path -LiteralPath $tempRoot)) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
    elseif ($KeepTemp) {
        Write-Host "Temp scaffolds kept at: $tempRoot"
    }
}

if ($failed -gt 0) {
    Write-Host "`nMATRIX FAILED ($failed)" -ForegroundColor Red
    exit 1
}
Write-Host "`nMATRIX PASSED" -ForegroundColor Green
