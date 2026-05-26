$ErrorActionPreference = 'Stop'

$powerToysExe = Join-Path $env:LOCALAPPDATA 'PowerToys\PowerToys.exe'
if (-not (Test-Path -LiteralPath $powerToysExe)) {
    $powerToysExe = Join-Path ${env:ProgramFiles} 'PowerToys\PowerToys.exe'
}

if (-not (Test-Path -LiteralPath $powerToysExe)) {
    throw "PowerToys.exe not found. Open PowerToys from the Start menu manually."
}

Write-Host 'Stopping PowerToys, Command Palette, and Daylog extension hosts...'
foreach ($name in @(
        'DaylogDockExtension',
        'DaylogDockExtension.Editor',
        'Microsoft.CmdPal.UI',
        'Microsoft.CmdPal.ExtensionHost',
        'PowerToys'
    )) {
    cmd.exe /c "taskkill /F /IM `"$name.exe`" /T >nul 2>&1"
}
Start-Sleep -Seconds 3

Write-Host "Starting $powerToysExe"
Start-Process -FilePath $powerToysExe
Start-Sleep -Seconds 5

cmd.exe /c 'tasklist /FI "IMAGENAME eq PowerToys.exe"'
cmd.exe /c 'tasklist /FI "IMAGENAME eq Microsoft.CmdPal.UI.exe"'
