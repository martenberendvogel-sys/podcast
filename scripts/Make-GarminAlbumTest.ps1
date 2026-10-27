param(
    [Parameter(Mandatory = $true)]
    [string] $StagingDirectory,

    [Parameter(Mandatory = $true)]
    [string] $OutputDirectory,

    [switch] $ReverseTrack,

    [switch] $ArtistGroupingTest
)

$ErrorActionPreference = 'Stop'

$stagingPath = (Resolve-Path -LiteralPath $StagingDirectory -ErrorAction Stop).Path
$manifestPath = Join-Path $stagingPath 'podcast-sync-manifest.json'
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw 'De stagingmap bevat geen Podcast Sync-manifest.'
}
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($manifest.Version -ne 1) { throw 'Onbekende manifestversie.' }

if ($ArtistGroupingTest) {
    if ($ReverseTrack) { throw 'Kies de artiestproef of de volgordeproef, niet beide.' }
    $selectedKeys = @($manifest.SelectedKeys)
    if ($selectedKeys.Count -lt 2) { throw 'Voor de artiestproef zijn minstens twee afleveringen nodig.' }
    $examples = @()
    foreach ($key in ($selectedKeys | Select-Object -First 2)) {
        $matches = @($manifest.SelectedEpisodes | Where-Object { $_.Key -eq $key })
        if ($matches.Count -ne 1) { throw "Geen unieke aflevering voor $key." }
        $examples += $matches[0]
    }
    if ($examples[0].Podcast -eq $examples[1].Podcast) {
        throw 'Voor de artiestproef moeten de eerste twee afleveringen uit verschillende podcasts komen.'
    }
    $album = 'Podcast Sync Artiest Test'
} else {
    $groups = @($manifest.SelectedEpisodes | Group-Object Podcast)
    if ($groups.Count -lt 2) { throw 'Voor deze proef zijn minstens twee verschillende podcasts nodig.' }
    if ($ReverseTrack) {
        if ($groups[0].Count -lt 2) { throw 'Voor de volgordeproef zijn twee afleveringen uit de eerste podcast nodig.' }
        $examples = @($groups[0].Group | Select-Object -First 2) + @($groups[1].Group[0])
        $album = 'Podcast Sync Volgorde Test'
    } else {
        $examples = @($groups | Select-Object -First 2 | ForEach-Object { $_.Group[0] })
        $album = 'Podcast Sync Test'
    }
}
$ffmpeg = (Get-Command ffmpeg -ErrorAction Stop).Source

$outputPath = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $outputPath) {
    throw "De testmap bestaat al: $outputPath. Kies een nieuwe map; bestaande bestanden worden niet overschreven."
}

# Validate all inputs before creating output, so an incomplete staging file
# cannot be mistaken for a successful Garmin test copy.
foreach ($episode in $examples) {
    if ($episode.Key -cnotmatch '^[0-9a-f]{64}$') { throw 'Ongeldige afleveringssleutel.' }
    $known = @($manifest.Files | Where-Object { $_.Key -eq $episode.Key })
    if ($known.Count -ne 1) { throw "Geen uniek voorbereid bestand voor $($episode.Key)." }
    $source = Join-Path $stagingPath "$($episode.Key).mp3"
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "MP3 ontbreekt: $source" }
    $file = Get-Item -LiteralPath $source
    $hash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
    if ($file.Length -ne $known[0].Bytes -or $hash -ine $known[0].Sha256) {
        throw "De MP3 wijkt af van het manifest: $source"
    }
}

New-Item -ItemType Directory -Path $outputPath -ErrorAction Stop | Out-Null
for ($index = 0; $index -lt $examples.Count; $index++) {
    $episode = $examples[$index]
    $source = Join-Path $stagingPath "$($episode.Key).mp3"
    $outputFile = Join-Path $outputPath ('{0:D2} - {1}.mp3' -f ($index + 1), $episode.Key.Substring(0, 8))
    $trackNumber = if ($ReverseTrack -or $ArtistGroupingTest) { $examples.Count - $index } else { $index + 1 }
    $track = '{0}/{1}' -f $trackNumber, $examples.Count

    $artistTags = @()
    if ($ArtistGroupingTest) {
        $artistTags = @('-metadata', 'artist=Podcast Sync', '-metadata', 'album_artist=Podcast Sync')
    }
    & $ffmpeg -hide_banner -loglevel error -nostdin -n -i $source -map '0:a:0' -map_metadata 0 -c:a copy -metadata "album=$album" @artistTags -metadata "track=$track" -id3v2_version 3 $outputFile
    if ($LASTEXITCODE -ne 0) {
        throw "FFmpeg stopte bij testbestand $($index + 1). De originele MP3's zijn niet gewijzigd."
    }
    Write-Host "Testbestand $($index + 1), track ${track}: $($episode.Podcast) -> $outputFile"
}

Write-Host "Klaar. $($examples.Count) kopieen hebben album $album en behouden hun eigen afleveringstitel."
Write-Host 'Deze proef schrijft niets naar de Garmin.'
