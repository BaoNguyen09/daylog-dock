$ErrorActionPreference = 'Stop'

Write-Host 'Stopping Daylog and extension host processes (taskkill)...'
foreach ($name in @('DaylogDockExtension', 'DaylogDockExtension.Editor')) {
    cmd.exe /c "taskkill /F /IM `"$name.exe`" /T >nul 2>&1"
}
Start-Sleep -Seconds 2
Write-Host 'Daylog processes stopped (or were not running).'

Write-Host ''
Write-Host 'Next:'
Write-Host '  1. Restart PowerToys from the tray (Quit, then start again)'
Write-Host '  2. Re-register: .\scripts\register-extension.ps1'
Write-Host '  3. Reload Command Palette extensions'
Write-Host ''
Write-Host 'If CmdPal is still slow, temporarily remove Daylog: .\scripts\unregister-extension.ps1'
