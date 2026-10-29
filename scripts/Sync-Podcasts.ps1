[CmdletBinding(DefaultParameterSetName = 'BackupFile')]
param(
    [Parameter(Mandatory = $true, ParameterSetName = 'BackupFile')]
    [string] $BackupPath,

    [Parameter(Mandatory = $true, ParameterSetName = 'Phone')]
    [string] $PhoneName,

    [Parameter(Mandatory = $true, ParameterSetName = 'Phone')]
    [string] $BackupDirectory,

    [string] $StagingDirectory,

    [switch] $Apply
)

$ErrorActionPreference = 'Stop'
$OutputEncoding = [Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)

$lockDirectory = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'PodcastSync'
New-Item -ItemType Directory -Path $lockDirectory -Force | Out-Null
$lockPath = Join-Path $lockDirectory 'sync.lock'
try {
    $syncLock = [IO.FileStream]::new($lockPath, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
} catch [IO.IOException] {
    throw 'Er draait al een Podcast Sync-opdracht. Start geen tweede synchronisatie.'
}
try {

$packageRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$inspectorExe = Join-Path $packageRoot 'bin\BackupInspector\PodcastSync.BackupInspector.exe'
$mtpExe = Join-Path $packageRoot 'bin\MtpProbe\PodcastSync.MtpProbe.exe'
$inspector = Join-Path $packageRoot 'src\PodcastSync.BackupInspector\PodcastSync.BackupInspector.csproj'
$mtp = Join-Path $packageRoot 'src\PodcastSync.MtpProbe\PodcastSync.MtpProbe.csproj'
$prepareAlbum = Join-Path $PSScriptRoot 'Prepare-GarminAlbum.ps1'
$hasInspectorExe = Test-Path -LiteralPath $inspectorExe -PathType Leaf
$hasMtpExe = Test-Path -LiteralPath $mtpExe -PathType Leaf
if ($hasInspectorExe -ne $hasMtpExe) {
    throw 'Het draagbare pakket is onvolledig: een van de twee benodigde programma''s ontbreekt.'
}
$portable = $hasInspectorExe -and $hasMtpExe
if (-not $portable) {
    if (-not (Test-Path -LiteralPath $inspector -PathType Leaf) -or -not (Test-Path -LiteralPath $mtp -PathType Leaf)) {
        throw 'Geen volledig draagbaar pakket of volledige ontwikkelrepo gevonden.'
    }
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw '.NET 8 SDK ontbreekt of dotnet staat niet op PATH.'
    }
}

if ($PSCmdlet.ParameterSetName -eq 'Phone') {
    Write-Output 'Nieuwste Podcast Addict-back-up gecontroleerd van telefoon ophalen'
    if ($portable) {
        $copyOutput = & $mtpExe phone-copy-latest $PhoneName $BackupDirectory
    } else {
        & dotnet build $mtp
        if ($LASTEXITCODE -ne 0) { throw 'Bouwen van de telefoonkopieerfunctie mislukt; Garmin niet gewijzigd.' }
        $copyOutput = & dotnet run --no-build --project $mtp -- phone-copy-latest $PhoneName $BackupDirectory
    }
    if ($LASTEXITCODE -ne 0) { throw 'Telefoonback-up ophalen mislukt; Garmin niet gewijzigd.' }
    $copyJson = $copyOutput -join [Environment]::NewLine
    Write-Output $copyJson
    $copy = $copyJson | ConvertFrom-Json
    if (-not $copy.Sha256Matches -or [string]::IsNullOrWhiteSpace($copy.LocalPath)) {
        throw 'De gekopieerde telefoonback-up is niet bevestigd; Garmin niet gewijzigd.'
    }
    $BackupPath = $copy.LocalPath
}

$backup = (Resolve-Path -LiteralPath $BackupPath -ErrorAction Stop).Path
if (-not (Test-Path -LiteralPath $backup -PathType Leaf)) {
    throw "Back-upbestand ontbreekt: $backup"
}
$match = [regex]::Match([IO.Path]::GetFileName($backup), '^PodcastAddict_(\d{8})_(\d{6})\.backup$')
if (-not $match.Success) {
    throw 'Gebruik een volledige Podcast Addict-back-up met naam PodcastAddict_YYYYMMDD_HHMMSS.backup.'
}
$stamp = $match.Groups[1].Value + $match.Groups[2].Value
if ($Apply) {
    try {
        $backupTime = [datetime]::ParseExact($stamp, 'yyyyMMddHHmmss', [Globalization.CultureInfo]::InvariantCulture)
    } catch {
        throw 'De datum in de back-upnaam is ongeldig.'
    }
    if ($backupTime -lt [datetime]::Now.AddHours(-48) -or $backupTime -gt [datetime]::Now.AddHours(1)) {
        throw 'Maak eerst een nieuwe volledige Podcast Addict-back-up (maximaal 48 uur oud); niets op de Garmin gewijzigd.'
    }
}

$parent = [IO.Path]::GetDirectoryName($backup)
$staging = if ([string]::IsNullOrWhiteSpace($StagingDirectory)) {
    Join-Path $parent 'podcast-staging'
} else {
    [IO.Path]::GetFullPath($StagingDirectory)
}
$ready = Join-Path $parent "Garmin klaar Podcast Sync $stamp"
Write-Output "1/3 Playlist voorbereiden vanuit $backup"
if ($portable) {
    & $inspectorExe prepare $backup $staging
} else {
    & dotnet run --project $inspector -- prepare $backup $staging
}
if ($LASTEXITCODE -ne 0) { throw 'Playlist voorbereiden mislukt; Garmin niet gewijzigd.' }

if (Test-Path -LiteralPath $ready) {
    Write-Output "2/3 Bestaande lokale Garmin-set controleren: $ready"
} else {
    Write-Output "2/3 Nieuwe lokale Garmin-set maken: $ready"
    & $prepareAlbum -StagingDirectory $staging -OutputDirectory $ready
}

Write-Output '3/3 Garmin-plan controleren (alleen lezen)'
if ($portable) {
    & $mtpExe sync-plan $backup $ready
} else {
    & dotnet run --project $mtp -- sync-plan $backup $ready
}
if ($LASTEXITCODE -ne 0) { throw 'Garmin-plan mislukt; geen synchronisatie uitgevoerd.' }

if (-not $Apply) {
    Write-Output 'Alleen voorbereiding en plan uitgevoerd. Voeg -Apply toe om de Garmin te synchroniseren.'
    return
}

Write-Output 'Synchronisatie uitvoeren; laat de Garmin aangesloten.'
if ($portable) {
    & $mtpExe sync-apply $backup $ready
} else {
    & dotnet run --project $mtp -- sync-apply $backup $ready
}
if ($LASTEXITCODE -ne 0) {
    throw 'Synchronisatie gestopt. Bewaar de lokale set en bekijk eerst het plan voordat je opnieuw probeert.'
}
Write-Output 'Klaar. Controleer op het horloge de groep Recente podcasts en de afspeelvolgorde.'

} finally {
    $syncLock.Dispose()
}
