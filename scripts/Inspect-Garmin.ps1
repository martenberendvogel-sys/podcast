# Read-only inventory of a connected Garmin device on Windows 11.
param([string] $StagingDirectory)

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

$syncPlan = $null
if (-not [string]::IsNullOrWhiteSpace($StagingDirectory)) {
    if ($null -eq $recent) { throw 'De Garmin-doelmap ontbreekt; er kan geen sync-overzicht worden gemaakt.' }
    $stagingPath = (Resolve-Path -LiteralPath $StagingDirectory -ErrorAction Stop).Path
    $manifestPath = Join-Path $stagingPath 'podcast-sync-manifest.json'
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        throw 'De stagingmap bevat geen Podcast Sync-manifest.'
    }
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    if ($manifest.Version -ne 1) { throw 'Onbekende manifestversie.' }

    $selected = @($manifest.SelectedKeys)
    foreach ($key in $selected) {
        if ($key -cnotmatch '^[0-9a-f]{64}$') { throw 'Het manifest bevat een ongeldige geselecteerde sleutel.' }
    }
    $known = @()
    $prepared = @()
    $localProblems = @()
    foreach ($file in $manifest.Files) {
        if ($file.Key -cnotmatch '^[0-9a-f]{64}$' -or $file.Sha256 -cnotmatch '^[0-9a-f]{64}$') {
            throw 'Het manifest bevat een ongeldige sleutel of controlesom.'
        }
        $known += $file.Key
        $audioPath = Join-Path $stagingPath "$($file.Key).mp3"
        if (-not (Test-Path -LiteralPath $audioPath -PathType Leaf)) {
            $localProblems += $file.Key
            continue
        }
        $audio = Get-Item -LiteralPath $audioPath
        $hash = (Get-FileHash -LiteralPath $audioPath -Algorithm SHA256).Hash
        if ($audio.Length -ne $file.Bytes -or $hash -ine $file.Sha256) {
            $localProblems += $file.Key
        } else {
            $prepared += $file.Key
        }
    }

    # MTP hides the extension in FolderItem.Name on this device.
    $deviceKeys = @($recentFiles | ForEach-Object {
        $name = $_.Name -replace '\.mp3$', ''
        if ($name -match '^[0-9a-f]{64}$') { $name.ToLowerInvariant() }
    } | Select-Object -Unique)
    $onDevice = @($selected | Where-Object { $deviceKeys -contains $_ })
    $needCopy = @($selected | Where-Object { $prepared -contains $_ -and $deviceKeys -notcontains $_ })
    $needDownload = @($selected | Where-Object { $prepared -notcontains $_ })
    $removeCandidates = @($deviceKeys | Where-Object { $known -contains $_ -and $selected -notcontains $_ })
    $unknownDeviceKeys = @($deviceKeys | Where-Object { $known -notcontains $_ })
    $syncPlan = [pscustomobject]@{
        SelectedCount = $selected.Count
        PreparedCount = $prepared.Count
        OnGarminCount = $onDevice.Count
        NeedsDownloadCount = $needDownload.Count
        NeedsCopyCount = $needCopy.Count
        RemovalCandidateCount = $removeCandidates.Count
        UnknownDeviceKeyCount = $unknownDeviceKeys.Count
        LocalProblemCount = $localProblems.Count
        NeedsCopyKeys = $needCopy
        RemovalCandidateKeys = $removeCandidates
        Warning = 'Alleen planning. Geen verwijdering op basis van een oude back-up of zonder controle van MTP-overdracht.'
    }
}

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
    SyncPlan = $syncPlan
} | ConvertTo-Json -Depth 6
