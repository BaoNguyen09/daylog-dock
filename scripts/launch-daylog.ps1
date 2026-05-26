$ErrorActionPreference = 'Stop'

$candidates = @(
    (Join-Path $env:LOCALAPPDATA 'Programs\Daylog\DaylogDockExtension.Editor.exe'),
    (Join-Path (Split-Path -Parent $PSScriptRoot) 'DaylogDockExtension\bin\x64\Debug\net9.0-windows10.0.26100.0\win-x64\DaylogDockExtension.Editor.exe'),
    (Join-Path (Split-Path -Parent $PSScriptRoot) 'DaylogDockExtension.Editor\bin\x64\Debug\net9.0-windows10.0.26100.0\win-x64\DaylogDockExtension.Editor.exe')
)

foreach ($path in $candidates) {
    if (Test-Path -LiteralPath $path) {
        Write-Host "Launching $path"
        Start-Process -FilePath $path
        return
    }
}

throw 'Daylog editor not found. Run .\scripts\install-daylog-standalone.ps1 or .\scripts\register-extension.ps1'
