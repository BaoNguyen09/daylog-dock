$ErrorActionPreference = 'Stop'

function Stop-DaylogProcesses {
    Get-Process -Name 'DaylogDockExtension*' -ErrorAction SilentlyContinue |
        Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 500
}

function Remove-AllDaylogPackages {
    $packages = Get-AppxPackage -ErrorAction SilentlyContinue |
        Where-Object {
            $_.Name -like '*Daylog*' -or
            $_.DisplayName -like '*Daylog*' -or
            $_.PackageFullName -like '*Daylog*'
        }

    foreach ($package in $packages) {
        Write-Host "Removing $($package.PackageFullName)"
        Remove-AppxPackage -Package $package.PackageFullName -ErrorAction SilentlyContinue
    }
}

Stop-DaylogProcesses
Remove-AllDaylogPackages

$remaining = Get-AppxPackage -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -like '*Daylog*' -or $_.DisplayName -like '*Daylog*' }

if ($remaining) {
    throw "Daylog packages still registered: $($remaining.PackageFullName -join ', ')"
}

Write-Host 'All Daylog package registrations removed.'
Write-Host 'Next: run .\scripts\register-extension.ps1, reload Command Palette, and unpin any stale Daylog bands before pinning the new one.'
