$ErrorActionPreference = 'Stop'

$installDir = Join-Path $env:LOCALAPPDATA 'Programs\Daylog'
$startupShortcut = Join-Path ([Environment]::GetFolderPath('Startup')) 'Daylog.lnk'
$startMenuShortcut = Join-Path ([Environment]::GetFolderPath('Programs')) 'Daylog\Daylog.lnk'

Get-Process -Name 'DaylogDockExtension.Editor' -ErrorAction SilentlyContinue |
    Stop-Process -Force -ErrorAction SilentlyContinue

foreach ($path in @($startupShortcut, $startMenuShortcut)) {
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Force
        Write-Host "Removed $path"
    }
}

if (Test-Path -LiteralPath $installDir) {
    Remove-Item -LiteralPath $installDir -Recurse -Force
    Write-Host "Removed $installDir"
}

Write-Host 'Daylog standalone app uninstalled.'
