# Full Command Palette recovery: stop hosts, remove Daylog MSIX, restart PowerToys.
# Run from DaylogDockExtension folder: .\scripts\recover-cmdpal.ps1
$ErrorActionPreference = 'Stop'
$scriptsRoot = $PSScriptRoot

Write-Host '=== Step 1: Force-stop Daylog and CmdPal processes (taskkill) ==='
foreach ($name in @(
        'DaylogDockExtension',
        'DaylogDockExtension.Editor',
        'Microsoft.CmdPal.UI',
        'Microsoft.CmdPal.ExtensionHost',
        'PowerToys'
    )) {
    cmd.exe /c "taskkill /F /IM `"$name.exe`" /T >nul 2>&1"
}
Start-Sleep -Seconds 2

Write-Host '=== Step 2: Remove Daylog extension package ==='
& (Join-Path $scriptsRoot 'unregister-extension.ps1')

Write-Host '=== Step 3: Restart PowerToys ==='
& (Join-Path $scriptsRoot 'restart-powertoys.ps1')

Write-Host ''
Write-Host '=== Done ==='
Write-Host '1. Wait ~10s, then press Win+Alt+Space (or your CmdPal shortcut).'
Write-Host '2. If palette opens, re-add Daylog when ready:'
Write-Host '     .\scripts\register-extension.ps1'
Write-Host '     Command Palette -> Settings -> Extensions -> Reload'
Write-Host '3. If palette still will not open: reboot Windows, then run this script again.'
Write-Host '4. If still broken: repair PowerToys in Settings -> Apps -> PowerToys -> Modify'
