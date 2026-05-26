param(
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$workspaceRoot = Split-Path -Parent $repoRoot
$exe = Join-Path $repoRoot 'DaylogDockExtension\bin\x64\Debug\net9.0-windows10.0.26100.0\win-x64\DaylogDockExtension.Editor.exe'
$manifest = Join-Path $repoRoot 'DaylogDockExtension\bin\x64\Debug\net9.0-windows10.0.26100.0\win-x64\AppxManifest.xml'
$settingsDir = Join-Path $env:LOCALAPPDATA 'DaylogDock'
$settingsPath = Join-Path $settingsDir 'settings.json'
$verifyRoot = Join-Path $env:TEMP 'DaylogDockVerify'
$today = Get-Date -Format 'yyyy-MM-dd'
$month = Get-Date -Format 'yyyy-MM'
$expectedFile = Join-Path $verifyRoot "Daylog\$month\$today.md"
$marker = "Daylog Dock autosave verify $(Get-Date -Format o)"
$originalSettings = if (Test-Path -LiteralPath $settingsPath) {
    Get-Content -LiteralPath $settingsPath -Raw
}
else {
    $null
}

function Stop-DaylogProcesses {
    Get-Process -Name 'DaylogDockExtension' -ErrorAction SilentlyContinue |
        Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 500
}

Push-Location $repoRoot
try {
    Stop-DaylogProcesses

    if (-not $SkipBuild) {
        & .\scripts\validate.ps1
    }
    else {
        & .\scripts\verify-static.ps1
        dotnet run --project .\DaylogDockExtension.LogicSmokeTests\DaylogDockExtension.LogicSmokeTests.csproj | Out-Null
    }

    if (-not (Test-Path -LiteralPath $manifest)) {
        throw "Missing AppxManifest.xml. Build first: .\scripts\validate.ps1"
    }

    Add-AppxPackage -Register -ForceApplicationShutdown -Path $manifest | Out-Null

    New-Item -ItemType Directory -Force -Path $settingsDir, $verifyRoot | Out-Null
    @{ RootFolder = $verifyRoot } | ConvertTo-Json | Set-Content -LiteralPath $settingsPath -Encoding utf8

    if (Test-Path -LiteralPath $expectedFile) {
        Remove-Item -LiteralPath $expectedFile -Force
    }

    $proc = Start-Process -FilePath $exe -PassThru
    Start-Sleep -Seconds 5

    if ($proc.HasExited) {
        throw "WinUI editor exited before UI verification (exit code $($proc.ExitCode)). Close other DaylogDockExtension instances and retry."
    }

    Add-Type -AssemblyName UIAutomationClient
    Add-Type -AssemblyName UIAutomationTypes

    $nameCondition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty,
        'Daylog')
    $window = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst(
        [System.Windows.Automation.TreeScope]::Children,
        $nameCondition)

    if ($null -eq $window) {
        throw 'WinUI editor window "Daylog" was not found.'
    }

    $editType = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Edit)
    $edit = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $editType)

    if ($null -eq $edit) {
        throw 'Editor text control was not found in the WinUI window.'
    }

    $pattern = $edit.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
    $valuePattern = [System.Windows.Automation.ValuePattern]$pattern
    $valuePattern.SetValue($marker)

    $saved = $false
    for ($i = 0; $i -lt 12; $i++) {
        Start-Sleep -Milliseconds 500
        if ((Test-Path -LiteralPath $expectedFile) -and ((Get-Content -LiteralPath $expectedFile -Raw) -like "*$marker*")) {
            $saved = $true
            break
        }
    }

    if (-not $saved) {
        throw "Autosave did not write expected marker to $expectedFile within 6 seconds."
    }

    Write-Host "Deploy verification passed."
    Write-Host "Wrote: $expectedFile"
}
finally {
    if ($proc -and -not $proc.HasExited) {
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    }

    if (Test-Path -LiteralPath $verifyRoot) {
        Remove-Item -LiteralPath $verifyRoot -Recurse -Force -ErrorAction SilentlyContinue
    }

    if ($null -ne $originalSettings) {
        $originalSettings | Set-Content -LiteralPath $settingsPath -Encoding utf8
    }
    elseif (Test-Path -LiteralPath $settingsPath) {
        Remove-Item -LiteralPath $settingsPath -Force -ErrorAction SilentlyContinue
    }
    Pop-Location
}
