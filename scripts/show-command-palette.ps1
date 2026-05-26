param(
    [switch]$Reload,
    [switch]$Restart,
    [switch]$RestartPowerToys,
    [switch]$CleanState
)

$ErrorActionPreference = 'Stop'

$showEventName = 'Local\PowerToysCmdPal-ShowEvent-62336fcd-8611-4023-9b30-091a6af4cc5a'
$cmdPalProtocol = if ($Reload -and -not $Restart) { 'x-cmdpal://reload' } else { 'x-cmdpal:' }
$repoRoot = Split-Path -Parent $PSScriptRoot
$catalogVerifier = Join-Path $repoRoot 'scripts\verify-cmdpal-catalog.ps1'
$cmdPalState = Join-Path $env:LOCALAPPDATA 'Packages\Microsoft.CommandPalette_8wekyb3d8bbwe\LocalState'
$cmdPalSettings = Join-Path $cmdPalState 'settings.json'
$cmdPalCache = Join-Path $cmdPalState 'commandProviderCache.json'

function Repair-CmdPalState {
    if (-not (Test-Path -LiteralPath $cmdPalSettings)) {
        Write-Warning "Command Palette settings not found: $cmdPalSettings"
        return
    }

    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    Copy-Item -LiteralPath $cmdPalSettings -Destination (Join-Path $cmdPalState "settings.json.bak-daylog-recover-$stamp") -Force
    if (Test-Path -LiteralPath $cmdPalCache) {
        Copy-Item -LiteralPath $cmdPalCache -Destination (Join-Path $cmdPalState "commandProviderCache.json.bak-daylog-recover-$stamp") -Force
    }

    $settings = Get-Content -LiteralPath $cmdPalSettings -Raw | ConvertFrom-Json
    if ($settings.ProviderSettings -and
        ($settings.ProviderSettings.PSObject.Properties.Name -contains 'DaylogDockExtension_8wekyb3d8bbwe!App!ID')) {
        $settings.ProviderSettings.PSObject.Properties.Remove('DaylogDockExtension_8wekyb3d8bbwe!App!ID')
    }

    $settings.UseLowLevelGlobalHotkey = $true
    $settings.AllowBreakthroughShortcut = $true
    $settings.AllowExternalReload = $true
    $settings.EnableDock = $true
    $settings.ShowSystemTrayIcon = $true
    $settings.SummonOn = 'ToMouse'
    $settings.DockSettings.CenterBands = @(@{
        ProviderId = 'DaylogDockExtension_8wekyb3d8bbwe!App!daylog.dock'
        CommandId = 'daylog.dock.openEditor'
        ShowTitles = $true
        ShowSubtitles = $true
    })

    $settings | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $cmdPalSettings -Encoding UTF8

    if (Test-Path -LiteralPath $cmdPalCache) {
        Remove-Item -LiteralPath $cmdPalCache -Force
    }

    Write-Host "Repaired CmdPal state. Backups use stamp: $stamp"
}

if ($CleanState) {
    Repair-CmdPalState
}

if ($RestartPowerToys) {
    Get-Process -Name PowerToys*, Microsoft.CmdPal*, DaylogDockExtension* -ErrorAction SilentlyContinue |
        Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2

    $powerToysExe = Join-Path $env:LOCALAPPDATA 'PowerToys\PowerToys.exe'
    if (-not (Test-Path -LiteralPath $powerToysExe)) {
        throw "PowerToys executable not found: $powerToysExe"
    }

    Start-Process -FilePath $powerToysExe
    Start-Sleep -Seconds 6
}

if ($Restart) {
    Get-Process -Name Microsoft.CmdPal.UI, DaylogDockExtension -ErrorAction SilentlyContinue |
        Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
}

if (Test-Path -LiteralPath $catalogVerifier) {
    & $catalogVerifier | Out-Host
}

Start-Process $cmdPalProtocol
Start-Sleep -Seconds 1

Add-Type @'
using System.Threading;

public static class CmdPalShowEvent
{
    public static void Signal(string eventName)
    {
        EventWaitHandle handle = EventWaitHandle.OpenExisting(eventName);
        try
        {
            handle.Set();
        }
        finally
        {
            handle.Dispose();
        }
    }
}
'@

$signaled = $false
for ($i = 0; $i -lt 80; $i++) {
    try {
        [CmdPalShowEvent]::Signal($showEventName)
        $signaled = $true
        break
    }
    catch {
        Start-Sleep -Milliseconds 250
    }
}

if (-not $signaled) {
    Write-Warning "Command Palette show event was not available: $showEventName"
    Write-Warning 'Continuing because protocol activation may already have opened Command Palette.'
}

if ($Reload -and $Restart) {
    Start-Process 'x-cmdpal://reload'
}

Start-Sleep -Seconds 3

$cmdPal = Get-Process -Name Microsoft.CmdPal.UI -ErrorAction SilentlyContinue | Select-Object -First 1
if ($null -eq $cmdPal) {
    throw 'Microsoft.CmdPal.UI is not running.'
}

$daylog = Get-Process -Name DaylogDockExtension -ErrorAction SilentlyContinue | Select-Object -First 1
$latestLog = Get-ChildItem "$env:LOCALAPPDATA\Microsoft\PowerToys\CmdPal\Logs" -Recurse -Filter 'Log_*.log' |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

Write-Host "Command Palette PID: $($cmdPal.Id)"
if ($daylog) {
    Write-Host "Daylog extension PID: $($daylog.Id)"
}
else {
    Write-Warning 'Daylog extension host is not running yet. Press Win + Alt + Space once, then rerun this script if CmdPal did not load it.'
}

if ($latestLog) {
    Write-Host "Latest CmdPal log: $($latestLog.FullName)"
    Get-Content -LiteralPath $latestLog.FullName -Tail 80 |
        Select-String -Pattern 'DaylogDockExtension|Loaded .*band|Failed to find band|External Reload|Hello World'
}
