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
$recentItemCount = $null
$recentFiles = @()
if ($null -ne $recent) {
    $items = $recent.GetFolder.Items()
    $recentItemCount = $items.Count
    $recentFiles = @($items | Where-Object { -not $_.IsFolder } |
        ForEach-Object { [pscustomobject]@{ Name = $_.Name; Type = $_.Type; Size = $_.Size } })
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
        RecentPodcastsItemCount = $recentItemCount
        RecentPodcastsFileCount = $recentFiles.Count
        FirstFiles = @($recentFiles | Select-Object -First 10)
    }
} | ConvertTo-Json -Depth 5
