# Scaffold matrix for the dotnet-new templates: installs both templates from
# this repo, scaffolds all-on / all-off / profiles / single-feature-offs /
# per-update-backend / per-logging-backend / per-distribution / setup-off /
# plus two invalid combos (portable+store, msix+velopack) that must fail the
# build with the MSBuild guard message, wires a page end-to-end inside add-page.ps1, then
# builds (0 warnings, 0 errors) and tests each scaffold.
# Fails the run on the first broken combo.
#
#   powershell -File Scripts/test-templates.ps1                 # full matrix
#   powershell -File Scripts/test-templates.ps1 -Combos allon,alloff
#   powershell -File Scripts/test-templates.ps1 -KeepTemp       # inspect output
#
# Same matrix runs in CI (.github/workflows/templates.yml).

param(
    [string[]]$Combos = @("allon", "alloff", "minimal", "desktop", "production", "notray", "noupd", "updbasic", "logmel", "lognone", "nocrash", "noloc", "notests", "nohttp", "nodiag", "nosetup", "msixapp", "msixstore", "msixnone", "badupd", "badupd2"),
    [switch]$KeepTemp
)

# `powershell -File` passes `-Combos a,b` through as one string; accept both
# comma-joined and real arrays.
$Combos = @($Combos | ForEach-Object { $_ -split ',' } | Where-Object { $_ -ne '' })

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("devtem-matrix-" + [System.Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null

$comboDefs = @{
    "allon"   = @{ Name = "AcmeDesk";    Safe = "AcmeDesk";    Display = "Acme Desk";  Company = "Acme"; Repo = "acme/desk-app"; Scheme = "acme://"; Flags = @(); Http = $true; Database = $true; Tray = $true; Updates = "velopack"; Logging = "serilog"; Health = $true; Attribution = $true; Crash = $true; Localization = $true; Tests = $true; Distribution = "portable"; Setup = $true }
    "alloff"  = @{ Name = "123 Bare App"; Safe = "_Bare_App";   Display = "Bare App";   Company = "Bare"; Repo = "bare/app";      Scheme = "bare://"; Flags = @("--tray", "false", "--updates", "none", "--database", "false"); Http = $true; Database = $false; Tray = $false; Updates = "none"; Logging = "serilog"; Health = $true; Attribution = $true; Crash = $true; Localization = $true; Tests = $true; Distribution = "portable"; Setup = $true }
    "notray"  = @{ Name = "NoTrayApp";   Safe = "NoTrayApp";    Display = "Noé Tray App"; Company = "Nt"; Repo = "nt/app";       Scheme = "nt://";   Flags = @("--tray", "false"); Http = $true; Database = $true; Tray = $false; Updates = "velopack"; Logging = "serilog"; Health = $true; Attribution = $true; Crash = $true; Localization = $true; Tests = $true; Distribution = "portable"; Setup = $true }
    "noupd"   = @{ Name = "NoUpdApp";    Safe = "NoUpdApp";    Display = "NoUpd App";  Company = "Nu"; Repo = "nu/app";        Scheme = "nu://";   Flags = @("--updates", "none"); Http = $true; Database = $true; Tray = $true; Updates = "none"; Logging = "serilog"; Health = $true; Attribution = $true; Crash = $true; Localization = $true; Tests = $true; Distribution = "portable"; Setup = $true }
    "updbasic" = @{ Name = "BasicUpdApp"; Safe = "BasicUpdApp"; Display = "BasicUpd App"; Company = "Bu"; Repo = "bu/app";      Scheme = "bu://";   Flags = @("--updates", "basic"); Http = $true; Database = $true; Tray = $true; Updates = "basic"; Logging = "serilog"; Health = $true; Attribution = $true; Crash = $true; Localization = $true; Tests = $true; Distribution = "portable"; Setup = $true }
    "nohttp"  = @{ Name = "NoHttpApp";   Safe = "NoHttpApp";    Display = "NoHttp App"; Company = "Nh"; Repo = "nh/app";        Scheme = "nh://";   Flags = @("--http", "false"); Http = $false; Database = $true; Tray = $true; Updates = "velopack"; Logging = "serilog"; Health = $true; Attribution = $true; Crash = $true; Localization = $true; Tests = $true; Distribution = "portable"; Setup = $true }
    "nodiag"  = @{ Name = "NoDiagApp";   Safe = "NoDiagApp";    Display = "NoDiag App"; Company = "Nd"; Repo = "nd/app";        Scheme = "nd://";   Flags = @("--health", "false"); Http = $true; Database = $true; Tray = $true; Updates = "velopack"; Logging = "serilog"; Health = $false; Attribution = $true; Crash = $true; Localization = $true; Tests = $true; Distribution = "portable"; Setup = $true }
    "nocrash" = @{ Name = "NoCrashApp";  Safe = "NoCrashApp";   Display = "NoCrash App"; Company = "Nc"; Repo = "nc/app";      Scheme = "nc://";   Flags = @("--crash", "false"); Http = $true; Database = $true; Tray = $true; Updates = "velopack"; Logging = "serilog"; Health = $true; Attribution = $true; Crash = $false; Localization = $true; Tests = $true; Distribution = "portable"; Setup = $true }
    "noloc"   = @{ Name = "NoLocApp";    Safe = "NoLocApp";     Display = "NoLoc App";  Company = "Nlo"; Repo = "nlo/app";     Scheme = "nlo://";  Flags = @("--localization", "false"); Http = $true; Database = $true; Tray = $true; Updates = "velopack"; Logging = "serilog"; Health = $true; Attribution = $true; Crash = $true; Localization = $false; Tests = $true; Distribution = "portable"; Setup = $true }
    "notests" = @{ Name = "NoTestsApp";  Safe = "NoTestsApp";   Display = "NoTests App"; Company = "Nt"; Repo = "ntests/app";  Scheme = "ntests://"; Flags = @("--tests", "false"); Http = $true; Database = $true; Tray = $true; Updates = "velopack"; Logging = "serilog"; Health = $true; Attribution = $true; Crash = $true; Localization = $true; Tests = $false; Distribution = "portable"; Setup = $true }
    "logmel"  = @{ Name = "MelLogApp";   Safe = "MelLogApp";    Display = "MelLog App"; Company = "Ml"; Repo = "ml/app";        Scheme = "ml://";   Flags = @("--logging", "mel"); Http = $true; Database = $true; Tray = $true; Updates = "velopack"; Logging = "mel"; Health = $true; Attribution = $true; Crash = $true; Localization = $true; Tests = $true; Distribution = "portable"; Setup = $true }
    "lognone" = @{ Name = "NoLogApp";    Safe = "NoLogApp";     Display = "NoLog App"; Company = "Nl"; Repo = "nl/app";        Scheme = "nl://";   Flags = @("--logging", "none"); Http = $true; Database = $true; Tray = $true; Updates = "velopack"; Logging = "none"; Health = $true; Attribution = $true; Crash = $true; Localization = $true; Tests = $true; Distribution = "portable"; Setup = $true }
    "minimal" = @{ Name = "MinimalProfile"; Safe = "MinimalProfile"; Display = "Minimal Profile"; Company = "Mp"; Repo = "mp/minimal"; Scheme = "minimal://"; Flags = @("--tray", "false", "--updates", "none", "--database", "false", "--http", "false", "--health", "false", "--logging", "none", "--crash", "false", "--localization", "false", "--tests", "false", "--attribution", "false"); Http = $false; Database = $false; Tray = $false; Updates = "none"; Logging = "none"; Health = $false; Attribution = $false; Crash = $false; Localization = $false; Tests = $false; Distribution = "portable"; Setup = $true }
    "desktop" = @{ Name = "DesktopProfile"; Safe = "DesktopProfile"; Display = "Desktop Profile"; Company = "Dp"; Repo = "dp/desktop"; Scheme = "desktop://"; Flags = @("--updates", "none", "--database", "false", "--http", "false"); Http = $false; Database = $false; Tray = $true; Updates = "none"; Logging = "serilog"; Health = $true; Attribution = $true; Crash = $true; Localization = $true; Tests = $true; Distribution = "portable"; Setup = $true }
    "production" = @{ Name = "ProductionProfile"; Safe = "ProductionProfile"; Display = "Production Profile"; Company = "Pp"; Repo = "pp/production"; Scheme = "production://"; Flags = @(); Http = $true; Database = $true; Tray = $true; Updates = "velopack"; Logging = "serilog"; Health = $true; Attribution = $true; Crash = $true; Localization = $true; Tests = $true; Distribution = "portable"; Setup = $true }
    "nosetup" = @{ Name = "NoSetupApp";  Safe = "NoSetupApp";   Display = "NoSetup App"; Company = "Ns"; Repo = "ns/app";       Scheme = "ns://";   Flags = @("--setup", "false"); Http = $true; Database = $true; Tray = $true; Updates = "velopack"; Logging = "serilog"; Health = $true; Attribution = $true; Crash = $true; Localization = $true; Tests = $true; Distribution = "portable"; Setup = $false }
    "msixapp" = @{ Name = "AppInstallerApp"; Safe = "AppInstallerApp"; Display = "AppInstaller App"; Company = "Ai"; Repo = "ai/app"; Scheme = "ai://"; Flags = @("--distribution", "msix", "--updates", "appinstaller", "--setup", "false"); Http = $true; Database = $true; Tray = $true; Updates = "appinstaller"; Logging = "serilog"; Health = $true; Attribution = $true; Crash = $true; Localization = $true; Tests = $true; Distribution = "msix"; Setup = $false }
    "msixstore" = @{ Name = "StoreApp";  Safe = "StoreApp";     Display = "Store App"; Company = "St"; Repo = "st/app";        Scheme = "st://";   Flags = @("--distribution", "msix", "--updates", "store", "--setup", "false"); Http = $true; Database = $true; Tray = $true; Updates = "store"; Logging = "serilog"; Health = $true; Attribution = $true; Crash = $true; Localization = $true; Tests = $true; Distribution = "msix"; Setup = $false }
    "msixnone" = @{ Name = "MsixNoneApp"; Safe = "MsixNoneApp"; Display = "MsixNone App"; Company = "Mn"; Repo = "mn/app";      Scheme = "mn://";   Flags = @("--distribution", "msix", "--updates", "none", "--setup", "false"); Http = $true; Database = $true; Tray = $true; Updates = "none"; Logging = "serilog"; Health = $true; Attribution = $true; Crash = $true; Localization = $true; Tests = $true; Distribution = "msix"; Setup = $false }
    "badupd"  = @{ Name = "BadUpdApp";   Safe = "BadUpdApp";    Display = "BadUpd App"; Company = "Bu2"; Repo = "bu2/app";      Scheme = "bu2://";  Flags = @("--updates", "store"); Http = $true; Database = $true; Tray = $true; Updates = "store"; Logging = "serilog"; Health = $true; Attribution = $true; Crash = $true; Localization = $true; Tests = $true; Distribution = "portable"; Setup = $true; ExpectBuildFailure = $true; ExpectedError = "cannot be combined with --distribution portable" }
    "badupd2" = @{ Name = "BadUpd2App";  Safe = "BadUpd2App";   Display = "BadUpd2 App"; Company = "Bu3"; Repo = "bu3/app";     Scheme = "bu3://";  Flags = @("--distribution", "msix", "--updates", "velopack", "--setup", "false"); Http = $true; Database = $true; Tray = $true; Updates = "velopack"; Logging = "serilog"; Health = $true; Attribution = $true; Crash = $true; Localization = $true; Tests = $true; Distribution = "msix"; Setup = $false; ExpectBuildFailure = $true; ExpectedError = "cannot be combined with --distribution msix" }
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
    foreach ($name in @("tray", "updates", "database", "http", "health", "crash", "localization", "tests")) {
        if ($null -eq $manifest.features.$name) {
            throw "feature manifest is missing '$name'"
        }
        if ($null -eq $manifest.features.$name.files) {
            throw "feature manifest has no file list for '$name'"
        }
    }
    foreach ($value in @("velopack", "basic", "none")) {
        if ($null -eq $manifest.features.updates.values.$value) {
            throw "feature manifest is missing updates value '$value'"
        }
        if ($null -eq $manifest.features.updates.values.$value.files) {
            throw "feature manifest has no file list for updates value '$value'"
        }
    }
    foreach ($value in @("appinstaller", "store")) {
        if ($null -eq $manifest.features.updates.values.$value) {
            throw "feature manifest is missing updates value '$value'"
        }
        if ($null -eq $manifest.features.updates.values.$value.files) {
            throw "feature manifest has no file list for updates value '$value'"
        }
    }
    if ($null -eq $manifest.features.distribution) {
        throw "feature manifest is missing 'distribution'"
    }
    foreach ($value in @("portable", "msix")) {
        if ($null -eq $manifest.features.distribution.values.$value) {
            throw "feature manifest is missing distribution value '$value'"
        }
        if ($null -eq $manifest.features.distribution.values.$value.files) {
            throw "feature manifest has no file list for distribution value '$value'"
        }
    }
    if ($null -eq $manifest.features.setup) {
        throw "feature manifest is missing 'setup'"
    }
    if ($null -eq $manifest.features.logging) {
        throw "feature manifest is missing 'logging'"
    }
    foreach ($value in @("serilog", "mel", "none")) {
        if ($null -eq $manifest.features.logging.values.$value) {
            throw "feature manifest is missing logging value '$value'"
        }
        if ($null -eq $manifest.features.logging.values.$value.files) {
            throw "feature manifest has no file list for logging value '$value'"
        }
    }
    Write-Host "feature manifest OK"
}

function Assert-ProfileDocumentation([string]$outDir, [hashtable]$definition) {
    $readmePath = Join-Path $outDir "README.md"
    $readme = Get-Content -Raw $readmePath
    foreach ($marker in @("Starter profiles", "--tray false", "--updates none", "--database false", "--http false", "--health false", "--logging none", "--crash false", "--localization false", "--tests false")) {
        if ($readme -notmatch [regex]::Escape($marker)) {
            throw "generated README is missing profile marker '$marker'"
        }
    }
    $features = Get-Content -Raw (Join-Path $outDir "docs\FEATURES.md")
    foreach ($pair in @(
        @("Distribution", $definition.Distribution),
        @("System tray", $definition.Tray),
        @("Auto-updates", $definition.Updates),
        @("Setup wizard", $definition.Setup),
        @("SQLite database", $definition.Database),
        @("Typed HTTP client", $definition.Http),
        @("Diagnostics page", $definition.Health),
        @("Logging backend", $definition.Logging),
        @("Crash reporting", $definition.Crash),
        @("Three-language UI", $definition.Localization),
        @("MSTest suite", $definition.Tests),
        @("DevTem attribution", $definition.Attribution)
    )) {
        $expected = [string]$pair[1]
        if ($features -notmatch ([regex]::Escape("| " + $pair[0] + " | " + $expected + " |"))) {
            throw "feature summary does not report $($pair[0])=$expected"
        }
    }
    $guides = @{
        tray = $definition.Tray
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
    $velopackGuide = Join-Path $outDir "docs\feature-guides\updates-velopack.md"
    if (($definition.Updates -eq "velopack") -ne (Test-Path -LiteralPath $velopackGuide)) {
        throw "updates-velopack guide presence is incorrect for updates=$($definition.Updates)"
    }
    $basicGuide = Join-Path $outDir "docs\feature-guides\updates-basic.md"
    if (($definition.Updates -eq "basic") -ne (Test-Path -LiteralPath $basicGuide)) {
        throw "updates-basic guide presence is incorrect for updates=$($definition.Updates)"
    }
    $appinstallerGuide = Join-Path $outDir "docs\feature-guides\updates-appinstaller.md"
    if (($definition.Updates -eq "appinstaller") -ne (Test-Path -LiteralPath $appinstallerGuide)) {
        throw "updates-appinstaller guide presence is incorrect for updates=$($definition.Updates)"
    }
    $storeGuide = Join-Path $outDir "docs\feature-guides\updates-store.md"
    if (($definition.Updates -eq "store") -ne (Test-Path -LiteralPath $storeGuide)) {
        throw "updates-store guide presence is incorrect for updates=$($definition.Updates)"
    }
    $msixGuide = Join-Path $outDir "docs\feature-guides\distribution-msix.md"
    if (($definition.Distribution -eq "msix") -ne (Test-Path -LiteralPath $msixGuide)) {
        throw "distribution-msix guide presence is incorrect for distribution=$($definition.Distribution)"
    }
    $setupGuide = Join-Path $outDir "docs\feature-guides\setup-wizard.md"
    if (($definition.Setup) -ne (Test-Path -LiteralPath $setupGuide)) {
        throw "setup-wizard guide presence is incorrect for setup=$($definition.Setup)"
    }
    $serilogGuide = Join-Path $outDir "docs\feature-guides\logging-serilog.md"
    if (($definition.Logging -eq "serilog") -ne (Test-Path -LiteralPath $serilogGuide)) {
        throw "logging-serilog guide presence is incorrect for logging=$($definition.Logging)"
    }
    $melGuide = Join-Path $outDir "docs\feature-guides\logging-mel.md"
    if (($definition.Logging -eq "mel") -ne (Test-Path -LiteralPath $melGuide)) {
        throw "logging-mel guide presence is incorrect for logging=$($definition.Logging)"
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
    $hasVelopack = $definition.Updates -eq "velopack"
    $hasBasic = $definition.Updates -eq "basic"
    # BackgroundUpdateService ships only for the file-replace engines
    # (excluded for none/appinstaller/store — the P2 status surface replaces it).
    $hasBackground = $hasVelopack -or $hasBasic
    foreach ($triple in @(
        @("Services\UpdateService.cs", $hasVelopack),
        @("Build\Features.Updates.Velopack.props", $hasVelopack),
        @("Services\BasicGithubUpdateService.cs", $hasBasic),
        @("Build\Features.Updates.Basic.props", $hasBasic),
        @("Services\BackgroundUpdateService.cs", $hasBackground),
        @("Build\Features.Distribution.props", $true),
        @(".github\workflows\release.yml", $hasVelopack)
    )) {
        $present = Test-Path -LiteralPath (Join-Path $outDir $triple[0])
        if ($triple[1] -ne $present) {
            throw "updates=$($definition.Updates): presence is incorrect for $($triple[0])"
        }
    }
    $distProps = [System.IO.File]::ReadAllText((Join-Path $outDir "Build\Features.Distribution.props"))
    if ($distProps -notmatch [regex]::Escape("<DevTemDistribution>" + $definition.Distribution + "</DevTemDistribution>")) {
        throw "distribution props value is not '$($definition.Distribution)'"
    }
    # P3: setup wizard files ship iff setup is on; UpdateCenter ships in
    # every combo (null-tolerant NoEngine status when updates=none).
    foreach ($triple in @(
        @("Pages\SetupWizardPage.xaml", $definition.Setup),
        @("Pages\SetupWizardPage.xaml.cs", $definition.Setup),
        @("ViewModels\SetupWizardViewModel.cs", $definition.Setup),
        @("Services\SetupWizardService.cs", $definition.Setup),
        @("Pages\UpdateCenterPage.xaml", $true),
        @("ViewModels\UpdateCenterViewModel.cs", $true)
    )) {
        $present = Test-Path -LiteralPath (Join-Path $outDir $triple[0])
        if ($triple[1] -ne $present) {
            throw "setup=$($definition.Setup): presence is incorrect for $($triple[0])"
        }
    }
    if ($distProps -notmatch [regex]::Escape("<DevTemSetupWizard>" + [string]$definition.Setup + "</DevTemSetupWizard>")) {
        throw "setup props value is not '$($definition.Setup)'"
    }
    if ($distProps -notmatch [regex]::Escape("<DevTemUpdates>" + $definition.Updates + "</DevTemUpdates>")) {
        throw "updates props value is not '$($definition.Updates)'"
    }
    $isSerilog = $definition.Logging -eq "serilog"
    $isMel = $definition.Logging -eq "mel"
    foreach ($triple in @(
        @("Services\Diagnostics\SerilogLogEntrySink.cs", $isSerilog),
        @("Services\Diagnostics\EventLogSink.cs", $isSerilog),
        @("Services\Diagnostics\ThreadIdEnricher.cs", $isSerilog),
        @("Build\Features.Logging.Serilog.props", $isSerilog),
        @("Services\Diagnostics\EventLogLoggerProvider.cs", $isMel),
        @("Build\Features.Logging.Mel.props", $isMel)
    )) {
        $present = Test-Path -LiteralPath (Join-Path $outDir $triple[0])
        if ($triple[1] -ne $present) {
            throw "logging=$($definition.Logging): presence is incorrect for $($triple[0])"
        }
    }
    $loggingService = [System.IO.File]::ReadAllText((Join-Path $outDir "Services\LoggingService.cs"))
    if ($loggingService -notmatch [regex]::Escape('BackendName = "' + $definition.Logging + '"')) {
        throw "scaffolded BackendName is not '$($definition.Logging)'"
    }
    $hasCrash = $definition.Crash
    foreach ($triple in @(
        @("Services\SentryCrashReporter.cs", $hasCrash),
        @("Build\Features.Crash.props", $hasCrash)
    )) {
        $present = Test-Path -LiteralPath (Join-Path $outDir $triple[0])
        if ($triple[1] -ne $present) {
            throw "crash=${hasCrash}: presence is incorrect for $($triple[0])"
        }
    }
    $hasLoc = $definition.Localization
    foreach ($triple in @(
        @("Services\Localization\EsStrings.cs", $hasLoc),
        @("Services\Localization\FrStrings.cs", $hasLoc)
    )) {
        $present = Test-Path -LiteralPath (Join-Path $outDir $triple[0])
        if ($triple[1] -ne $present) {
            throw "localization=${hasLoc}: presence is incorrect for $($triple[0])"
        }
    }
    $testSources = @(Get-ChildItem -LiteralPath (Join-Path $outDir "Tests") -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Extension -eq '.cs' })
    if ($definition.Tests -and $testSources.Count -eq 0) {
        throw "tests=true: no test sources scaffolded"
    }
    if ((-not $definition.Tests) -and $testSources.Count -gt 0) {
        throw "tests=false: test sources remain ($($testSources.Count) files)"
    }
    $testsCsproj = Join-Path $outDir ("Tests\" + $definition.Safe + ".Tests.csproj")
    if (-not (Test-Path -LiteralPath $testsCsproj)) {
        throw "test project shell is missing"
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
        if (-not $comboDefs.ContainsKey($combo)) { throw "Unknown combo: $combo (allon/alloff/minimal/desktop/production/notray/noupd/updbasic/logmel/lognone/nocrash/noloc/notests/nohttp/nodiag/nosetup/msixapp/msixstore/msixnone/badupd/badupd2)" }
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
        # Invalid combos must fail the build with the MSBuild guard message
        # (the template engine cannot constrain parameter combinations).
        if ($c.ExpectBuildFailure) {
            if ($buildCode -eq 0) {
                Write-Host "FAILED: build $combo unexpectedly succeeded (guard did not fire)" -ForegroundColor Red
                $failed++
                continue
            }
            if ($buildLog -notmatch [regex]::Escape($c.ExpectedError)) {
                Write-Host "FAILED: build $combo failed without the guard message" -ForegroundColor Red
                Write-Host $buildLog
                $failed++
                continue
            }
            Write-Host "build ${combo}: failed as expected with the guard message" -ForegroundColor Green
            Write-Host "PASSED: $combo" -ForegroundColor Green
            continue
        }
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

        $testOut = ""
        if ($c.Tests) {
            $testOut = & dotnet test (Join-Path $outDir "Tests\$($c.Safe).Tests.csproj") -c Debug -p:Platform=x64 --nologo -v q 2>&1 | Out-String
            if ($LASTEXITCODE -ne 0 -or $testOut -notmatch 'Passed!' -or $testOut -match 'Failed:\s+[1-9]') {
                Write-Host "FAILED: tests for $combo" -ForegroundColor Red
                Write-Host $testOut
                $failed++
                continue
            }
        }
        else {
            Write-Host "tests skipped (tests feature off, shell project builds)"
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
