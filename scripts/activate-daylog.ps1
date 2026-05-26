# Register Daylog, restart PowerToys/CmdPal, warm extension host. No manual steps.
$ErrorActionPreference = 'Stop'
$scriptsRoot = $PSScriptRoot

function Stop-ProcessNames {
    param([string[]]$Names)
    foreach ($name in $Names) {
        cmd.exe /c "taskkill /F /IM `"$name.exe`" /T >nul 2>&1"
    }
    Start-Sleep -Seconds 2
}

Write-Host '=== Register Daylog extension ==='
& (Join-Path $scriptsRoot 'register-extension.ps1')

Write-Host '=== Restart PowerToys and Command Palette ==='
Stop-ProcessNames @(
    'DaylogDockExtension',
    'DaylogDockExtension.Editor',
    'Microsoft.CmdPal.UI',
    'Microsoft.CmdPal.ExtensionHost',
    'PowerToys'
)

$powerToysExe = Join-Path $env:LOCALAPPDATA 'PowerToys\PowerToys.exe'
if (-not (Test-Path -LiteralPath $powerToysExe)) {
    $powerToysExe = Join-Path ${env:ProgramFiles} 'PowerToys\PowerToys.exe'
}
if (-not (Test-Path -LiteralPath $powerToysExe)) {
    throw 'PowerToys.exe not found.'
}

Start-Process -FilePath $powerToysExe | Out-Null
Start-Sleep -Seconds 8

$cmdPalUi = Get-ChildItem -LiteralPath (Join-Path $env:LOCALAPPDATA 'PowerToys') -Recurse -Filter 'Microsoft.CmdPal.UI.exe' -ErrorAction SilentlyContinue |
    Select-Object -First 1
if ($cmdPalUi) {
    Write-Host "Warming CmdPal: $($cmdPalUi.FullName)"
    Start-Process -FilePath $cmdPalUi.FullName | Out-Null
    Start-Sleep -Seconds 3
}

$pkg = Get-AppxPackage -Name 'DaylogDockExtension*' -ErrorAction SilentlyContinue | Select-Object -First 1

Write-Host '=== Status ==='
cmd.exe /c 'tasklist /FI "IMAGENAME eq PowerToys.exe"'
cmd.exe /c 'tasklist /FI "IMAGENAME eq Microsoft.CmdPal.UI.exe"'
if ($pkg) {
    Write-Host "Daylog package: $($pkg.PackageFullName)"
}
else {
    Write-Host 'Daylog package: NOT REGISTERED'
}

Write-Host '=== Open Command Palette (Win+Alt+Space) ==='
& (Join-Path $scriptsRoot 'open-cmdpal.ps1')
Start-Sleep -Seconds 2

Write-Host 'Activation finished.'
