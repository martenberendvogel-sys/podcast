param(
    [Parameter(Mandatory = $true)]
    [string] $InputPath,

    [ValidateRange(1, 120)]
    [int] $Minutes = 20,

    [string] $OutputDirectory
)

$ErrorActionPreference = 'Stop'

$inputFile = (Resolve-Path -LiteralPath $InputPath -ErrorAction Stop).Path
if ([IO.Path]::GetExtension($inputFile) -ine '.mp3') {
    throw 'Dit hulpmiddel verwacht één MP3-bestand als invoer.'
}

$ffmpeg = (Get-Command ffmpeg -ErrorAction Stop).Source
$ffprobe = (Get-Command ffprobe -ErrorAction Stop).Source
$baseName = [IO.Path]::GetFileNameWithoutExtension($inputFile)

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path ([IO.Path]::GetDirectoryName($inputFile)) ($baseName + '-stukken')
}
$outputPath = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $outputPath) {
    throw "De uitvoermap bestaat al: $outputPath. Kies een nieuwe map; bestaande bestanden worden nooit overschreven."
}

$durationText = & $ffprobe -v error -show_entries format=duration -of 'default=noprint_wrappers=1:nokey=1' $inputFile
if ($LASTEXITCODE -ne 0 -or -not $durationText) {
    throw 'De duur van het MP3-bestand kon niet worden gelezen.'
}
$duration = [double]::Parse($durationText.Trim(), [Globalization.CultureInfo]::InvariantCulture)
$segmentSeconds = $Minutes * 60
$count = [int][Math]::Ceiling($duration / $segmentSeconds)
if ($count -lt 1) {
    throw 'Het MP3-bestand bevat geen audio.'
}

New-Item -ItemType Directory -Path $outputPath -ErrorAction Stop | Out-Null
Write-Host "Maak $count stukken van maximaal $Minutes minuten in $outputPath"

for ($index = 1; $index -le $count; $index++) {
    $number = $index.ToString('D3')
    $start = (($index - 1) * $segmentSeconds).ToString([Globalization.CultureInfo]::InvariantCulture)
    $length = $segmentSeconds.ToString([Globalization.CultureInfo]::InvariantCulture)
    $outputFile = Join-Path $outputPath ("{0} - {1}.mp3" -f $baseName, $number)
    $title = "{0} - {1}" -f $baseName, $number

    & $ffmpeg -hide_banner -loglevel error -nostdin -n -ss $start -i $inputFile -t $length -map '0:a:0' -map_metadata -1 -c:a copy -metadata "title=$title" -metadata "track=$index/$count" -id3v2_version 3 $outputFile
    if ($LASTEXITCODE -ne 0) {
        throw "Het splitsen stopte bij stuk $number. De originele MP3 is niet gewijzigd; controleer de uitvoermap."
    }
    Write-Host "Klaar: $number / $count"
}

Write-Host 'Klaar. Bewaar de originele MP3 als reserve en test eerst een paar stukken op de Garmin.'
