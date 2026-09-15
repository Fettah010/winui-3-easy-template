# Submits a .msixupload to the Microsoft Store via the Partner Center
# submission API: create submission, upload package, commit, optional
# status poll. Needs a one-time Azure AD app (tenant + client id/secret
# with submission rights); credentials come from params or DEVTEM_STORE_*
# env vars and are never logged. Listings/description stay manual
# (Scripts/new-store-listing.ps1 drafts them; the API clones the previous
# submission's listings by default). Certification itself takes days -
# the script commits and returns; pass -Wait to poll instead.
#
#   powershell -File Scripts/submit-store.ps1 -MsixUpload Releases/Msix/App_0.0.5.0_x64.msixupload
#   powershell -File Scripts/submit-store.ps1 -MsixUpload <file> -Wait -WhatIf
#
# Setup: Azure Portal > Entra ID > app registration > secret; Partner
# Center > Account settings > Users > add the app with Manager role.
# Staged rollouts and flights stay Partner Center-managed (see
# docs/feature-guides/updates-store.md).

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$MsixUpload = "",
    [string]$AppId = "",
    [string]$TenantId = "",
    [string]$ClientId = "",
    [string]$ClientSecret = "",
    [ValidateSet("Immediate", "Manual")][string]$TargetPublishMode = "Immediate",
    [string]$CertificationNotes = "",
    [switch]$Wait,
    [int]$PollTimeoutSec = 1200
)

$ErrorActionPreference = "Stop"

function Resolve-Cred([string]$value, [string]$envName, [string]$label) {
    if ([string]::IsNullOrWhiteSpace($value)) { $value = [Environment]::GetEnvironmentVariable($envName) }
    if ([string]::IsNullOrWhiteSpace($value)) { throw "Missing $label. Pass -$label or set $envName." }
    return $value
}

function Invoke-StoreApi([string]$method, [string]$url, [string]$token, $body) {
    $headers = @{ Authorization = "Bearer $token" }
    $params = @{ Uri = $url; Method = $method; Headers = $headers; ContentType = "application/json" }
    if ($body -ne $null) { $params["Body"] = ($body | ConvertTo-Json -Depth 10) }
    try {
        return Invoke-RestMethod @params
    }
    catch {
        $detail = $_.ErrorDetails.Message
        if ([string]::IsNullOrWhiteSpace($detail)) { $detail = $_.Exception.Message }
        throw "Store API $method failed: $detail"
    }
}

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
if ([string]::IsNullOrWhiteSpace($MsixUpload)) {
    $MsixUpload = Get-ChildItem -LiteralPath (Join-Path $projectRoot "Releases\Msix") -Filter "*.msixupload" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1 -ExpandProperty FullName
}
if ([string]::IsNullOrWhiteSpace($MsixUpload) -or -not (Test-Path -LiteralPath $MsixUpload)) {
    throw "No .msixupload found. Build one first (Scripts/publish-store.ps1) or pass -MsixUpload."
}
$AppId = Resolve-Cred $AppId "DEVTEM_STORE_APP_ID" "AppId"
$TenantId = Resolve-Cred $TenantId "DEVTEM_STORE_TENANT_ID" "TenantId"
$ClientId = Resolve-Cred $ClientId "DEVTEM_STORE_CLIENT_ID" "ClientId"
$ClientSecret = Resolve-Cred $ClientSecret "DEVTEM_STORE_CLIENT_SECRET" "ClientSecret"

$fileName = [System.IO.Path]::GetFileName($MsixUpload)
$base = "https://manage.devcenter.microsoft.com/v1.2/my/applications/$AppId"

if ($PSCmdlet.ShouldProcess($fileName, "Create Store submission")) {
    Write-Host "Authenticating tenant (secret never logged)..."
    $tokenResponse = Invoke-RestMethod -Uri "https://login.microsoftonline.com/$TenantId/oauth2/token" `
        -Method Post -ContentType "application/x-www-form-urlencoded" `
        -Body "grant_type=client_credentials&client_id=$ClientId&client_secret=$ClientSecret&resource=https://manage.devcenter.microsoft.com"
    $token = $tokenResponse.access_token
    if ([string]::IsNullOrWhiteSpace($token)) { throw "Authentication returned no token." }

    Write-Host "Creating submission for $fileName..."
    $submission = Invoke-StoreApi "Post" "$base/submissions" $token $null
    $submissionId = $submission.id
    if ([string]::IsNullOrWhiteSpace($submissionId)) { throw "Submission creation returned no id." }
    Write-Host "Submission: $submissionId"

    $package = @{ fileName = $fileName }
    if (-not [string]::IsNullOrWhiteSpace($CertificationNotes)) {
        $update = @{ applicationPackages = @($package); notesForCertification = $CertificationNotes }
    }
    else {
        $update = @{ applicationPackages = @($package) }
    }
    Write-Host "Registering package..."
    $updated = Invoke-StoreApi "Put" "$base/submissions/$submissionId" $token $update
    $uploadUrl = $updated.fileUploadUrl
    if ([string]::IsNullOrWhiteSpace($uploadUrl)) { throw "Submission update returned no fileUploadUrl." }

    Write-Host "Uploading package ($fileName)..."
    try {
        Invoke-RestMethod -Uri $uploadUrl -Method Put -Headers @{ "x-ms-blob-type" = "BlockBlob" } `
            -InFile $MsixUpload -ContentType "application/octet-stream" | Out-Null
    }
    catch {
        $detail = $_.ErrorDetails.Message
        if ([string]::IsNullOrWhiteSpace($detail)) { $detail = $_.Exception.Message }
        throw "Package upload failed: $detail"
    }

    Write-Host "Committing (publish mode: $TargetPublishMode)..."
    Invoke-StoreApi "Post" "$base/submissions/$submissionId/commit" $token @{ targetPublishMode = $TargetPublishMode } | Out-Null
    Write-Host "Committed submission $submissionId."

    if ($Wait) {
        $deadline = (Get-Date).AddSeconds($PollTimeoutSec)
        while ((Get-Date) -lt $deadline) {
            Start-Sleep -Seconds 60
            $status = Invoke-StoreApi "Get" "$base/submissions/$submissionId/status" $token $null
            Write-Host "Status: $($status.status)"
            if ($status.status -eq "Published") { Write-Host "Published."; return }
            if ($status.status -like "*Fail*") { throw "Submission failed with status $($status.status)." }
        }
        Write-Host "WARNING: poll timed out; check Partner Center for submission $submissionId." -ForegroundColor Yellow
    }
    else {
        Write-Host "Track certification in Partner Center (submission $submissionId)."
    }
}
