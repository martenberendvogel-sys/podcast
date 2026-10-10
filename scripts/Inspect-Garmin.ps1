# Read-only inventory of a connected Garmin device on Windows 11.
$ErrorActionPreference = 'Stop'

$pnp = @(Get-PnpDevice -PresentOnly |
    Where-Object { $_.FriendlyName -match 'Garmin|Forerunner' } |
    Select-Object Class, FriendlyName, Status)

$shell = New-Object -ComObject Shell.Application
$thisPc = $shell.Namespace(17)
$portable = @($thisPc.Items() |
    Where-Object { $_.Name -match 'Garmin|Forerunner' } |
    ForEach-Object { [pscustomobject]@{ Name = $_.Name; Type = $_.Type } })

$volumes = @(Get-Volume |
    Where-Object { $_.FileSystemLabel -match 'Garmin|Forerunner' } |
    Select-Object DriveLetter, FileSystemLabel, FileSystem)

[pscustomobject]@{
    Detected = ($pnp.Count + $portable.Count + $volumes.Count) -gt 0
    PnpDevices = $pnp
    ExplorerDevices = $portable
    Volumes = $volumes
} | ConvertTo-Json -Depth 4
