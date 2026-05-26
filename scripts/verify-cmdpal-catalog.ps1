$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot '.tmp\CatalogCheck\CatalogCheck.csproj'

if (-not (Test-Path -LiteralPath $project)) {
    throw "Missing catalog diagnostic project: $project"
}

$output = dotnet run --project $project
if ($LASTEXITCODE -ne 0) {
    throw "Catalog diagnostic failed to run. dotnet exit code: $LASTEXITCODE"
}

$output

$joinedOutput = $output -join [Environment]::NewLine
if ($joinedOutput -notmatch 'Id=daylog\.dock') {
    throw 'Windows AppExtension catalog does not currently expose Daylog to Command Palette.'
}

Write-Host ''
Write-Host 'Catalog verification passed: daylog.dock is discoverable by Command Palette.'
