$ErrorActionPreference = 'Stop'

Get-AppxPackage -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -like '*DaylogDockExtension*' } |
    ForEach-Object {
        Write-Host "Removing $($_.PackageFullName)"
        Remove-AppxPackage -Package $_.PackageFullName
    }

Write-Host 'Daylog extension removed. Restart PowerToys if Command Palette still fails:'
Write-Host '  .\scripts\restart-powertoys.ps1'
