param(
    [Parameter(Mandatory = $true)]
    [string] $StagingDirectory,

    [Parameter(Mandatory = $true)]
    [string] $OutputDirectory
)

$ErrorActionPreference = 'Stop'

$stagingPath = (Resolve-Path -LiteralPath $StagingDirectory -ErrorAction Stop).Path
$manifestPath = Join-Path $stagingPath 'podcast-sync-manifest.json'
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw 'De stagingmap bevat geen Podcast Sync-manifest.'
}
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($manifest.Version -ne 1) { throw 'Onbekende manifestversie.' }

$selected = @($manifest.SelectedKeys)
if ($selected.Count -lt 1) { throw 'De playlist is leeg.' }
if (@($selected | Select-Object -Unique).Count -ne $selected.Count) {
    throw 'Het manifest bevat dubbele geselecteerde sleutels.'
}
$files = @{}
foreach ($file in $manifest.Files) {
    if ($file.Key -cnotmatch '^[0-9a-f]{64}$' -or $file.Sha256 -cnotmatch '^[0-9a-f]{64}$') {
        throw 'Ongeldige sleutel of controlesom in manifest.'
    }
    if ($files.ContainsKey($file.Key)) { throw 'Dubbele bestandssleutel in manifest.' }
    $files[$file.Key] = $file
}

$episodes = @{}
foreach ($episode in $manifest.SelectedEpisodes) {
    if ($episodes.ContainsKey($episode.Key)) { throw 'Dubbele afleveringssleutel in manifest.' }
    $episodes[$episode.Key] = $episode
}

$totalBytes = [long] 0
foreach ($key in $selected) {
    if ($key -cnotmatch '^[0-9a-f]{64}$' -or -not $files.ContainsKey($key) -or -not $episodes.ContainsKey($key)) {
        throw "Een geselecteerde aflevering ontbreekt in de stagingmap: $key"
    }
    $source = Join-Path $stagingPath "$key.mp3"
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "MP3 ontbreekt: $source" }
    $audio = Get-Item -LiteralPath $source
    $hash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
    if ($audio.Length -ne $files[$key].Bytes -or $hash -ine $files[$key].Sha256) {
        throw "De MP3 wijkt af van het manifest: $key"
    }
    $totalBytes += $audio.Length
}

$outputPath = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $outputPath) {
    throw "De uitvoermap bestaat al: $outputPath. Kies een nieuwe map; niets wordt overschreven."
}
$availableBytes = [IO.DriveInfo]::new([IO.Path]::GetPathRoot($outputPath)).AvailableFreeSpace
if ($availableBytes -lt ($totalBytes + 50MB)) {
    throw 'Onvoldoende vrije schijfruimte voor aparte Garmin-kopieen.'
}
$ffmpeg = (Get-Command ffmpeg -ErrorAction Stop).Source

New-Item -ItemType Directory -Path $outputPath -ErrorAction Stop | Out-Null
$readyFiles = @()
for ($index = 0; $index -lt $selected.Count; $index++) {
    $key = $selected[$index]
    $source = Join-Path $stagingPath "$key.mp3"
    $destination = Join-Path $outputPath "$key.mp3"
    # Garmin plays this shared album in descending track-number order.
    # Reverse the tags so the first desired episode gets the highest number.
    $trackNumber = $selected.Count - $index
    $track = '{0}/{1}' -f $trackNumber, $selected.Count

    & $ffmpeg -hide_banner -loglevel error -nostdin -n -i $source -map '0:a:0' -map_metadata 0 -c:a copy -metadata 'album=Recente podcasts' -metadata "track=$track" -id3v2_version 3 $destination
    if ($LASTEXITCODE -ne 0) {
        throw "FFmpeg stopte bij playlistpositie $($index + 1). Originele MP3's zijn niet gewijzigd."
    }
    $ready = Get-Item -LiteralPath $destination
    $readyFiles += [pscustomobject]@{
        Rank = $index + 1
        TrackNumber = $trackNumber
        Key = $key
        Podcast = $episodes[$key].Podcast
        Title = $episodes[$key].Title
        Bytes = $ready.Length
        Sha256 = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    Write-Host "Garmin-kopie $($index + 1)/$($selected.Count): $($episodes[$key].Title)"
}

[pscustomobject]@{
    Version = 1
    Album = 'Recente podcasts'
    OrderMode = 'GarminPodcastDescendingTrack'
    SelectedKeys = $selected
    Files = $readyFiles
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $outputPath 'podcast-sync-garmin-ready.json') -Encoding UTF8

Write-Host "Klaar: $($selected.Count) aparte MP3-kopieen in $outputPath. Niets is naar de Garmin geschreven."
