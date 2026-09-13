# Registers (or removes) the app's deep-link URI scheme for the current user.
#
#   powershell -File Scripts\register-protocol.ps1
#   powershell -File Scripts\register-protocol.ps1 -Unregister
#   powershell -File Scripts\register-protocol.ps1 -Scheme "devtem://" -ExePath "C:\path\DevTemWinUi3.exe"
#   powershell -File Scripts\register-protocol.ps1 -WhatIf   # dry run: prints actions, writes nothing
#
# Unpackaged apps have no manifest, so the scheme (devtem://settings, …)
# only resolves after these HKCU keys exist. The app self-registers on every
# startup (ProtocolService.EnsureRegistered) — this script is for testing the
# scheme without launching the app, or for installs that never run it.
# Per-user (HKCU\Software\Classes): no admin needed. MSIX installs register
# through Package.appxmanifest instead and must NOT run this.
#
# NOTE: spell -Scheme WITH "://" (the default does): both rename engines key
# on the "devtem://" literal, so renamed apps keep working with no edits.

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$Scheme = "devtem://",
    [string]$ExePath = "",
    [switch]$Unregister
)

$ErrorActionPreference = "Stop"

# Bare form: "devtem://" -> "devtem" (matches AppMetadata.ProtocolScheme).
$Scheme = $Scheme.Trim().TrimEnd('/').TrimEnd(':')
if ([string]::IsNullOrWhiteSpace($Scheme)) { throw "Scheme must not be empty." }

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
if ([string]::IsNullOrWhiteSpace($ExePath)) {
    $ExePath = Join-Path $projectRoot "bin\x64\Debug\net10.0-windows10.0.19041.0\DevTemWinUi3.exe"
}
if (-not $Unregister -and -not (Test-Path -LiteralPath $ExePath)) {
    throw "Exe not found: $ExePath (build first, or pass -ExePath)."
}

$baseKey = "HKCU:\Software\Classes\$Scheme"
if ($Unregister) {
    if ($WhatIfPreference) {
        Write-Host "What if: remove ${Scheme}:// (HKCU:\Software\Classes\$Scheme)"
        return
    }
    if (Test-Path -LiteralPath $baseKey) {
        Remove-Item -LiteralPath $baseKey -Recurse -Force
        Write-Host "Unregistered ${Scheme}://"
    }
    else {
        Write-Host "${Scheme}:// was not registered."
    }
    return
}

if ($WhatIfPreference) {
    Write-Host "What if: register ${Scheme}:// -> $ExePath (HKCU:\Software\Classes\$Scheme)"
    return
}

if (-not (Test-Path -LiteralPath $baseKey)) {
    New-Item -Path $baseKey -Force | Out-Null
}
Set-ItemProperty -LiteralPath $baseKey -Name "(Default)" -Value "URL:DevTem-WinUI 3 deep link"
New-ItemProperty -LiteralPath $baseKey -Name "URL Protocol" -Value "" -Force | Out-Null
if (-not (Test-Path -LiteralPath "$baseKey\DefaultIcon")) {
    New-Item -Path "$baseKey\DefaultIcon" -Force | Out-Null
}
Set-ItemProperty -LiteralPath "$baseKey\DefaultIcon" -Name "(Default)" -Value "`"$ExePath`",0"
if (-not (Test-Path -LiteralPath "$baseKey\shell\open\command")) {
    New-Item -Path "$baseKey\shell\open\command" -Force | Out-Null
}
Set-ItemProperty -LiteralPath "$baseKey\shell\open\command" -Name "(Default)" -Value "`"$ExePath`" `"%1`""

Write-Host "Registered ${Scheme}:// -> $ExePath" -ForegroundColor Green
Write-Host "Try: Start-Process `"${Scheme}://settings`"" -ForegroundColor DarkGray
