param(
    [switch]$SkipBuild,
    [switch]$Clean
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$workspaceRoot = Split-Path -Parent $repoRoot

$env:DOTNET_CLI_HOME = Join-Path $workspaceRoot '.dotnet-home'
$env:NUGET_PACKAGES = Join-Path $workspaceRoot '.nuget-packages'
$env:APPDATA = Join-Path $workspaceRoot '.appdata'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

New-Item -ItemType Directory -Force -Path $env:DOTNET_CLI_HOME, $env:NUGET_PACKAGES, $env:APPDATA | Out-Null

function Invoke-CheckedNative {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$FilePath failed with exit code $LASTEXITCODE"
    }
}

function Stop-DaylogProcesses {
    Get-Process -Name 'DaylogDockExtension' -ErrorAction SilentlyContinue |
        Stop-Process -Force -ErrorAction SilentlyContinue
    Get-Process -Name 'DaylogDockExtension.Editor' -ErrorAction SilentlyContinue |
        Stop-Process -Force -ErrorAction SilentlyContinue
    Get-Process -Name 'Microsoft.UI.Xaml.Markup.Compiler' -ErrorAction SilentlyContinue |
        Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 500
}

Push-Location $repoRoot
try {
    Stop-DaylogProcesses
    & .\scripts\generate-icons.ps1

    if ($Clean) {
        $generatedPaths = @(
            '.\DaylogDockExtension\bin',
            '.\DaylogDockExtension\obj',
            '.\DaylogDockExtension.LogicSmokeTests\bin',
            '.\DaylogDockExtension.LogicSmokeTests\obj'
        )

        foreach ($path in $generatedPaths) {
            if (Test-Path -LiteralPath $path) {
                Remove-Item -LiteralPath $path -Recurse -Force
            }
        }
    }

    & .\scripts\verify-static.ps1
    Invoke-CheckedNative dotnet @(
        'run',
        '--project',
        '.\DaylogDockExtension.LogicSmokeTests\DaylogDockExtension.LogicSmokeTests.csproj'
    )

    if (-not $SkipBuild) {
        Invoke-CheckedNative dotnet @(
            'build',
            '.\DaylogDockExtension.sln',
            '-m:1',
            '-p:Platform=x64',
            '-p:Configuration=Debug',
            '-p:WindowsSdkPath=C:\Program Files (x86)\Windows Kits\10',
            '-p:TargetPlatformSdkRootOverride=C:\Program Files (x86)\Windows Kits\10',
            '-p:TargetPlatformDisplayName=Windows 10.0.26100.0'
        )
    }
}
finally {
    Pop-Location
}
