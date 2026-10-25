# Read-only comparison of a Garmin-ready album with MTP file metadata.
param(
    [Parameter(Mandatory = $true)]
    [string] $GarminReadyDirectory
)

$ErrorActionPreference = 'Stop'

function Find-FolderItem {
    param($Parent, [string] $Name)
    if ($null -eq $Parent) { return $null }
    $matches = @($Parent.Items() | Where-Object { $_.IsFolder -and $_.Name -eq $Name })
    if ($matches.Count -ne 1) { return $null }
    return $matches[0]
}

function Read-MediaProperty {
    param($Folder, $Item, [string] $PropertyName, [string[]] $ColumnNames)

    try {
        $value = $Item.ExtendedProperty($PropertyName)
        if ($null -ne $value -and -not [string]::IsNullOrWhiteSpace([string] $value)) {
            return [string] $value
        }
    } catch {
        # Some MTP drivers do not expose the Windows property system.
    }

    foreach ($index in $script:mediaColumnIndexes) {
        if ($ColumnNames -contains $script:mediaHeadings[$index]) {
            try {
                $value = $Folder.GetDetailsOf($Item, $index)
                if (-not [string]::IsNullOrWhiteSpace([string] $value)) { return [string] $value }
            } catch {
                # Treat missing Shell metadata as unknown, not as a failed sync.
            }
        }
    }
    return $null
}

$readyPath = (Resolve-Path -LiteralPath $GarminReadyDirectory -ErrorAction Stop).Path
$manifestPath = Join-Path $readyPath 'podcast-sync-garmin-ready.json'
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw 'De Garmin-klare map bevat geen manifest.'
}
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($manifest.Version -ne 1 -or $manifest.OrderMode -ne 'GarminPodcastDescendingTrack' -or $manifest.Album -ne 'Recente podcasts') {
    throw 'Dit is geen ondersteunde Garmin-klare set met omgekeerde trackvolgorde.'
}
$selected = @($manifest.SelectedKeys)
$files = @($manifest.Files)
if ($selected.Count -lt 1 -or $files.Count -ne $selected.Count -or @($selected | Select-Object -Unique).Count -ne $selected.Count) {
    throw 'Het Garmin-klare manifest is onvolledig of bevat dubbele sleutels.'
}
$expected = @{}
foreach ($file in $files) {
    if ($file.Key -cnotmatch '^[0-9a-f]{64}$' -or $file.Sha256 -cnotmatch '^[0-9a-f]{64}$' -or
        $expected.ContainsKey($file.Key) -or $selected -notcontains $file.Key) {
        throw 'Het Garmin-klare manifest bevat een ongeldige of dubbele bestandssleutel.'
    }
    $expected[$file.Key] = $file
}
for ($index = 0; $index -lt $selected.Count; $index++) {
    $file = $expected[$selected[$index]]
    if ($file.Rank -ne $index + 1 -or $file.TrackNumber -ne $selected.Count - $index) {
        throw 'De tracknummers in het manifest komen niet overeen met de gewenste playlistvolgorde.'
    }
}

$shell = New-Object -ComObject Shell.Application
$thisPc = $shell.Namespace(17)
$devices = @($thisPc.Items() | Where-Object { $_.IsFolder -and $_.Name -eq 'Forerunner 265' })
if ($devices.Count -ne 1) { throw 'Precies één aangesloten Forerunner 265 is vereist.' }
$storage = Find-FolderItem $devices[0].GetFolder 'Internal Storage'
$podcasts = if ($null -ne $storage) { Find-FolderItem $storage.GetFolder 'Podcasts' } else { $null }
$recent = if ($null -ne $podcasts) { Find-FolderItem $podcasts.GetFolder 'Recente podcasts' } else { $null }
if ($null -eq $recent) { throw 'De Garmin-map Podcasts\Recente podcasts is niet gevonden.' }
$folder = $recent.GetFolder
$mediaHeadings = @{}
$mediaColumnIndexes = @()
for ($index = 0; $index -lt 350; $index++) {
    try { $heading = $folder.GetDetailsOf($null, $index) } catch { continue }
    if (@('#', 'Tracknummer', 'Track number', 'Album') -contains $heading) {
        $mediaHeadings[$index] = $heading
        $mediaColumnIndexes += $index
    }
}

$observed = @()
foreach ($item in @($folder.Items() | Where-Object { -not $_.IsFolder })) {
    $key = $item.Name -replace '(?i)\.mp3$', ''
    $known = $key -cmatch '^[0-9a-f]{64}$' -and $expected.ContainsKey($key)
    $observed += [pscustomobject]@{
        Name = $item.Name
        Selected = $known
        ExpectedRank = if ($known) { $expected[$key].Rank } else { $null }
        ExpectedTrack = if ($known) { $expected[$key].TrackNumber } else { $null }
        DeviceTrack = Read-MediaProperty $folder $item 'System.Music.TrackNumber' @('#', 'Tracknummer', 'Track number')
        DeviceAlbum = Read-MediaProperty $folder $item 'System.Music.AlbumTitle' @('Album')
    }
}
$presentKeys = @($observed | Where-Object Selected | ForEach-Object { $_.Name -replace '(?i)\.mp3$', '' })
[pscustomobject]@{
    ExpectedCount = $selected.Count
    DeviceFileCount = $observed.Count
    MissingKeys = @($selected | Where-Object { $presentKeys -notcontains $_ })
    UnexpectedNames = @($observed | Where-Object { -not $_.Selected } | ForEach-Object Name)
    Files = $observed
    Warning = 'Alleen lezen. MTP-metadata kan ontbreken of verouderd zijn; naam en tags bewijzen geen volledige kopie. Er is niets gekopieerd of verwijderd.'
} | ConvertTo-Json -Depth 5
