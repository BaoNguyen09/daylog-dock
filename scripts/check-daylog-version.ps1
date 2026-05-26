$ErrorActionPreference = 'Stop'

$pkg = Get-AppxPackage -Name '*DaylogDockExtension*' | Select-Object -First 1
if ($null -eq $pkg) {
    Write-Host 'Daylog extension is NOT installed.'
    Write-Host 'Run: .\scripts\register-extension.ps1'
    exit 1
}

Write-Host "Installed: $($pkg.PackageFullName)"
Write-Host "Version:   $($pkg.Version)"
Write-Host "Location:  $($pkg.InstallLocation)"
Write-Host ''

$assets = Join-Path $pkg.InstallLocation 'Assets'
$public = Join-Path $pkg.InstallLocation 'Public'
Write-Host 'Key icon files:'
foreach ($file in @(
    (Join-Path $assets 'StoreLogo.png'),
    (Join-Path $assets 'Square44x44Logo.png'),
    (Join-Path $public 'icon.png')
)) {
    if (Test-Path -LiteralPath $file) {
        $item = Get-Item -LiteralPath $file
        Write-Host "  OK  $($item.Name)  $($item.Length) bytes  $($item.LastWriteTime)"
    }
    else {
        Write-Host "  MISSING  $file"
    }
}

Write-Host ''
Write-Host 'Expected after latest register: Version 0.0.8.0'
Write-Host 'Then: Command Palette -> Settings -> Extensions -> Reload'
Write-Host 'If icon still wrong: fully quit PowerToys from the tray, start it again, reload extensions.'
