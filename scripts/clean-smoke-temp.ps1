$ErrorActionPreference = 'Stop'

$paths = @(
    (Join-Path $env:TEMP 'daylog-dock-smoke-root'),
    (Join-Path $env:TEMP 'daylog-dock-smoke-settings')
)

foreach ($path in $paths) {
    Write-Host "Checking $path"
    if (Test-Path -LiteralPath $path) {
        Write-Host "Would remove $path"
    }
}

Write-Host 'No files were removed. Re-run manually with Remove-Item if you want to delete these temp smoke-test folders.'
