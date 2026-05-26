param(
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$manifest = Join-Path $repoRoot 'DaylogDockExtension\bin\x64\Debug\net9.0-windows10.0.26100.0\win-x64\AppxManifest.xml'
$editorExe = Join-Path $repoRoot 'DaylogDockExtension\bin\x64\Debug\net9.0-windows10.0.26100.0\win-x64\DaylogDockExtension.Editor.exe'

function Stop-DaylogProcesses {
    foreach ($name in @('DaylogDockExtension', 'DaylogDockExtension.Editor')) {
        cmd.exe /c "taskkill /F /IM `"$name.exe`" /T >nul 2>&1"
    }
    Start-Sleep -Milliseconds 500
}

function Remove-DaylogPackages {
    Get-AppxPackage -ErrorAction SilentlyContinue |
        Where-Object {
            $_.Name -like '*Daylog*' -or
            $_.DisplayName -like '*Daylog*' -or
            $_.PackageFullName -like '*Daylog*'
        } |
        ForEach-Object {
            Write-Host "Removing $($_.PackageFullName)"
            Remove-AppxPackage -Package $_.PackageFullName -ErrorAction SilentlyContinue
        }
}

Push-Location $repoRoot
try {
    Stop-DaylogProcesses
    Remove-DaylogPackages
    & .\scripts\generate-icons.ps1

    if (-not $SkipBuild) {
        & .\scripts\validate.ps1
    }

    if (-not (Test-Path -LiteralPath $manifest)) {
        throw "Missing package manifest: $manifest"
    }

    if (-not (Test-Path -LiteralPath $editorExe)) {
        throw "Missing editor executable in package layout: $editorExe"
    }

    Add-AppxPackage -Register -ForceApplicationShutdown -Path $manifest | Out-Null

    $pkg = Get-AppxPackage -Name 'DaylogDockExtension*' | Select-Object -First 1
    if ($null -eq $pkg) {
        throw 'Daylog Dock package did not register.'
    }

    $manifestXml = [xml](Get-Content -LiteralPath $manifest)
    $ns = New-Object System.Xml.XmlNamespaceManager($manifestXml.NameTable)
    $ns.AddNamespace('uap3', 'http://schemas.microsoft.com/appx/manifest/uap/windows10/3')
    $cmdPalNode = $manifestXml.SelectSingleNode('//uap3:AppExtension[@Name="com.microsoft.commandpalette"]', $ns)
    $cmdPalId = if ($null -ne $cmdPalNode) { $cmdPalNode.Id } else { $null }
    if ([string]::IsNullOrWhiteSpace($cmdPalId)) {
        throw 'com.microsoft.commandpalette app extension missing from AppxManifest.xml.'
    }

    Write-Host "Registered: $($pkg.PackageFullName)"
    Write-Host "Install: $($pkg.InstallLocation)"
    Write-Host "CmdPal extension id: $cmdPalId"
    Write-Host "Editor bundled: $(Test-Path -LiteralPath $editorExe)"
    Write-Host ''
    Write-Host 'Check version: .\scripts\check-daylog-version.ps1'
    Write-Host 'Standalone + startup: .\scripts\install-daylog-standalone.ps1'
    Write-Host ''
    Write-Host 'Next:'
    Write-Host '  1. Start PowerToys again (tray icon) if it was closed'
    Write-Host '  2. Open Command Palette with Win + Alt + Space'
    Write-Host '  3. Command Palette -> Settings -> Extensions -> Reload, if Daylog is not listed yet'
    Write-Host '  4. Unpin/repin the Daylog dock band'
    Write-Host '  5. Version in check script should show 0.0.8.0'
}
finally {
    Pop-Location
}
