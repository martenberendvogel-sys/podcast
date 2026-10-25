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

$groups = @($manifest.SelectedEpisodes | Group-Object Podcast)
if ($groups.Count -lt 2) { throw 'Voor deze proef zijn minstens twee verschillende podcasts nodig.' }
$examples = @($groups | Select-Object -First 2 | ForEach-Object { $_.Group[0] })
$ffmpeg = (Get-Command ffmpeg -ErrorAction Stop).Source

$outputPath = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $outputPath) {
    throw "De testmap bestaat al: $outputPath. Kies een nieuwe map; bestaande bestanden worden niet overschreven."
}

# Validate both inputs before creating output, so an incomplete staging file
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
for ($index = 0; $index -lt 2; $index++) {
    $episode = $examples[$index]
    $source = Join-Path $stagingPath "$($episode.Key).mp3"
    $outputFile = Join-Path $outputPath ('{0:D2} - {1}.mp3' -f ($index + 1), $episode.Key.Substring(0, 8))
    $track = '{0}/2' -f ($index + 1)

    & $ffmpeg -hide_banner -loglevel error -nostdin -n -i $source -map '0:a:0' -map_metadata 0 -c:a copy -metadata 'album=Podcast Sync Test' -metadata "track=$track" -id3v2_version 3 $outputFile
    if ($LASTEXITCODE -ne 0) {
        throw "FFmpeg stopte bij testbestand $($index + 1). De originele MP3's zijn niet gewijzigd."
    }
    Write-Host "Testbestand $($index + 1): $($episode.Podcast) -> $outputFile"
}

Write-Host 'Klaar. Beide kopieen hebben album Podcast Sync Test en behouden hun eigen afleveringstitel.'
Write-Host 'Deze proef schrijft niets naar de Garmin.'
