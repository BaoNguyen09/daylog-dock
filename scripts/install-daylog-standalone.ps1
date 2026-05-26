param(
    [switch]$SkipPublish,
    [switch]$NoStartup
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$editorProject = Join-Path $repoRoot 'DaylogDockExtension.Editor\DaylogDockExtension.Editor.csproj'
$installDir = Join-Path $env:LOCALAPPDATA 'Programs\Daylog'
$publishDir = Join-Path $repoRoot 'DaylogDockExtension.Editor\bin\x64\Release\net9.0-windows10.0.26100.0\win-x64\publish'
$editorExe = Join-Path $installDir 'DaylogDockExtension.Editor.exe'
$iconPath = Join-Path $installDir 'Daylog.ico'

function New-Shortcut {
    param(
        [string]$Path,
        [string]$TargetPath,
        [string]$Arguments,
        [string]$WorkingDirectory,
        [string]$IconLocation,
        [string]$Description
    )

    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($Path)
    $shortcut.TargetPath = $TargetPath
    $shortcut.Arguments = $Arguments
    $shortcut.WorkingDirectory = $WorkingDirectory
    $shortcut.IconLocation = $IconLocation
    $shortcut.Description = $Description
    $shortcut.Save()
}

Push-Location $repoRoot
try {
    if (-not $SkipPublish) {
        & .\scripts\generate-icons.ps1
        dotnet publish $editorProject `
            -c Release `
            -p:Platform=x64 `
            -r win-x64 `
            --self-contained true `
            -p:PublishTrimmed=false
    }

    if (-not (Test-Path -LiteralPath $publishDir)) {
        throw "Publish output missing: $publishDir"
    }

    if (Test-Path -LiteralPath $installDir) {
        Remove-Item -LiteralPath $installDir -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $installDir | Out-Null
    Copy-Item -Path (Join-Path $publishDir '*') -Destination $installDir -Recurse -Force

    if (-not (Test-Path -LiteralPath $editorExe)) {
        throw "Editor executable missing after install: $editorExe"
    }

    $programsMenu = [Environment]::GetFolderPath('Programs')
    $daylogMenu = Join-Path $programsMenu 'Daylog'
    New-Item -ItemType Directory -Force -Path $daylogMenu | Out-Null

    New-Shortcut `
        -Path (Join-Path $daylogMenu 'Daylog.lnk') `
        -TargetPath $editorExe `
        -Arguments '' `
        -WorkingDirectory $installDir `
        -IconLocation "$iconPath,0" `
        -Description 'Daylog daily journal'

    if (-not $NoStartup) {
        $startupFolder = [Environment]::GetFolderPath('Startup')
        New-Shortcut `
            -Path (Join-Path $startupFolder 'Daylog.lnk') `
            -TargetPath $editorExe `
            -Arguments '--startup' `
            -WorkingDirectory $installDir `
            -IconLocation "$iconPath,0" `
            -Description 'Daylog (starts with Windows, ready on the dock band)'

        Write-Host 'Startup shortcut created (runs minimized near the dock).'
    }

    Write-Host "Installed Daylog to $installDir"
    Write-Host "Start menu: $daylogMenu\Daylog.lnk"
    Write-Host ''
    Write-Host 'The editor stays running in the background. Command Palette dock band toggles the same window.'
    Write-Host 'Manual launch: .\scripts\launch-daylog.ps1'
    Write-Host 'Re-register the extension if needed: .\scripts\register-extension.ps1'

    $startupShortcut = Join-Path ([Environment]::GetFolderPath('Startup')) 'Daylog.lnk'
    if (Test-Path -LiteralPath $startupShortcut) {
        $sh = New-Object -ComObject WScript.Shell
        $startupLink = $sh.CreateShortcut($startupShortcut)
        if (-not (Test-Path -LiteralPath $startupLink.TargetPath)) {
            throw "Startup shortcut target is missing: $($startupLink.TargetPath)"
        }
    }
}
finally {
    Pop-Location
}
