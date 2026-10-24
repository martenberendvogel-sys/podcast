param(
    [Parameter(Mandatory = $true)]
    [string] $StagingDirectory,

    [switch] $Apply
)

$ErrorActionPreference = 'Stop'

function Find-FolderItem {
    param($Parent, [string] $Name)
    if ($null -eq $Parent) { return $null }
    $matches = @($Parent.Items() | Where-Object { $_.IsFolder -and $_.Name -eq $Name })
    if ($matches.Count -ne 1) { return $null }
    return $matches[0]
}

$stagingPath = (Resolve-Path -LiteralPath $StagingDirectory -ErrorAction Stop).Path
$manifestPath = Join-Path $stagingPath 'podcast-sync-manifest.json'
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw 'De stagingmap bevat geen Podcast Sync-manifest.'
}
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($manifest.Version -ne 1) { throw 'Onbekende manifestversie.' }

$known = @{}
foreach ($file in $manifest.Files) {
    if ($file.Key -cnotmatch '^[0-9a-f]{64}$' -or $file.Sha256 -cnotmatch '^[0-9a-f]{64}$') {
        throw 'Het manifest bevat een ongeldige sleutel of controlesom.'
    }
    if ($known.ContainsKey($file.Key)) { throw 'Het manifest bevat een dubbele afleveringssleutel.' }
    $known[$file.Key] = $file
}

$shell = New-Object -ComObject Shell.Application
$thisPc = $shell.Namespace(17)
$devices = @($thisPc.Items() | Where-Object { $_.IsFolder -and $_.Name -eq 'Forerunner 265' })
if ($devices.Count -ne 1) { throw 'Precies één aangesloten Forerunner 265 is vereist.' }
$storage = Find-FolderItem $devices[0].GetFolder 'Internal Storage'
$podcasts = if ($null -ne $storage) { Find-FolderItem $storage.GetFolder 'Podcasts' } else { $null }
$recent = if ($null -ne $podcasts) { Find-FolderItem $podcasts.GetFolder 'Recente podcasts' } else { $null }
if ($null -eq $recent) { throw 'De bestaande Garmin-map Podcasts\Recente podcasts is niet gevonden. Er wordt niets aangemaakt.' }
$destination = $recent.GetFolder

foreach ($key in $manifest.SelectedKeys) {
    if ($key -cnotmatch '^[0-9a-f]{64}$') { throw 'Het manifest bevat een ongeldige geselecteerde sleutel.' }
    if (-not $known.ContainsKey($key)) { continue }

    $existing = @($destination.Items() | Where-Object { $_.Name -eq $key -or $_.Name -eq "$key.mp3" })
    if ($existing.Count -gt 0) { continue }

    $source = Join-Path $stagingPath "$key.mp3"
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Voorbereide MP3 ontbreekt: $key" }
    $file = Get-Item -LiteralPath $source
    if ($file.Length -ne $known[$key].Bytes) { throw "Bestandsgrootte wijkt af van manifest: $key" }
    $hash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
    if ($hash -ine $known[$key].Sha256) { throw "Controlesom wijkt af van manifest: $key" }

    if (-not $Apply) {
        Write-Host "Proef zonder kopiëren: $source -> Forerunner 265\Internal Storage\Podcasts\Recente podcasts"
        Write-Host 'Voeg -Apply toe om precies deze ene ontbrekende MP3 te kopiëren. Er wordt niets verwijderd of overschreven.'
        return
    }

    # Windows Shell performs MTP copies asynchronously. Do not claim byte-level
    # verification: this Garmin reports Size=0 even for a playable MP3.
    $destination.CopyHere($source, 0)
    Write-Host "Kopieeropdracht gestart voor $key. Wacht tot Windows klaar is en controleer daarna op het horloge."
    return
}

Write-Host 'Geen ontbrekende voorbereide MP3 gevonden. Er is niets gekopieerd.'
