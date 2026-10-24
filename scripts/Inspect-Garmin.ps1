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

function Find-FolderItem {
    param($Parent, [string] $Name)
    if ($null -eq $Parent) { return $null }
    $matches = @($Parent.Items() | Where-Object { $_.IsFolder -and $_.Name -eq $Name })
    if ($matches.Count -ne 1) { return $null }
    return $matches[0]
}

$deviceItems = @($thisPc.Items() | Where-Object { $_.IsFolder -and $_.Name -eq 'Forerunner 265' })
$storage = $null
$podcasts = $null
$recent = $null
if ($deviceItems.Count -eq 1) {
    $storage = Find-FolderItem $deviceItems[0].GetFolder 'Internal Storage'
    if ($null -ne $storage) {
        $podcasts = Find-FolderItem $storage.GetFolder 'Podcasts'
        if ($null -ne $podcasts) {
            $recent = Find-FolderItem $podcasts.GetFolder 'Recente podcasts'
        }
    }
}
$recentMp3Count = $null
if ($null -ne $recent) {
    $recentMp3Count = @($recent.GetFolder.Items() | Where-Object { -not $_.IsFolder -and $_.Name -like '*.mp3' }).Count
}

$volumes = @(Get-Volume |
    Where-Object { $_.FileSystemLabel -match 'Garmin|Forerunner' } |
    Select-Object DriveLetter, FileSystemLabel, FileSystem)

[pscustomobject]@{
    Detected = ($pnp.Count + $portable.Count + $volumes.Count) -gt 0
    PnpDevices = $pnp
    ExplorerDevices = $portable
    Volumes = $volumes
    PodcastTarget = [pscustomobject]@{
        DeviceFound = $deviceItems.Count -eq 1
        StorageFound = $null -ne $storage
        PodcastsFound = $null -ne $podcasts
        RecentPodcastsFound = $null -ne $recent
        RecentPodcastsMp3Count = $recentMp3Count
    }
} | ConvertTo-Json -Depth 4
