# One disposable file in Podcasts\Recente podcasts. Never touches playlist hash files.
param(
    [Parameter(Mandatory = $true)]
    [string] $TestFilePath,

    [ValidateSet('Plan', 'Copy', 'Delete')]
    [string] $Action = 'Plan'
)

$ErrorActionPreference = 'Stop'

function Find-FolderItem {
    param($Parent, [string] $Name)
    if ($null -eq $Parent) { return $null }
    $matches = @($Parent.Items() | Where-Object { $_.IsFolder -and $_.Name -eq $Name })
    if ($matches.Count -ne 1) { return $null }
    return $matches[0]
}

$source = (Resolve-Path -LiteralPath $TestFilePath -ErrorAction Stop).Path
if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw 'Het testbestand ontbreekt.' }
if ([IO.Path]::GetFileName([IO.Path]::GetDirectoryName($source)) -cne 'Podcast Sync Volgorde Test') {
    throw 'Het testbestand moet uit de lokale map Podcast Sync Volgorde Test komen.'
}
$leaf = [IO.Path]::GetFileName($source)
if ($leaf -cnotmatch '^0[1-3] - [0-9a-f]{8}\.mp3$') {
    throw 'Gebruik uitsluitend een van de drie bestanden uit de map Podcast Sync Volgorde Test.'
}
$baseName = [IO.Path]::GetFileNameWithoutExtension($leaf)
$audio = Get-Item -LiteralPath $source
if ($audio.Length -lt 1024) { throw 'Het testbestand is onverwacht klein.' }

$shell = New-Object -ComObject Shell.Application
$thisPc = $shell.Namespace(17)
$devices = @($thisPc.Items() | Where-Object { $_.IsFolder -and $_.Name -eq 'Forerunner 265' })
if ($devices.Count -ne 1) { throw 'Precies één aangesloten Forerunner 265 is vereist.' }
$storage = Find-FolderItem $devices[0].GetFolder 'Internal Storage'
$podcasts = if ($null -ne $storage) { Find-FolderItem $storage.GetFolder 'Podcasts' } else { $null }
$recent = if ($null -ne $podcasts) { Find-FolderItem $podcasts.GetFolder 'Recente podcasts' } else { $null }
if ($null -eq $recent) { throw 'De Garmin-testmap Podcasts\Recente podcasts ontbreekt.' }
$destination = $recent.GetFolder
$matches = @($destination.Items() | Where-Object {
    -not $_.IsFolder -and ($_.Name -eq $baseName -or $_.Name -eq $leaf)
})
if ($matches.Count -gt 1) { throw 'Het testbestand staat meer dan één keer op de Garmin; stop voor handmatige controle.' }

if ($Action -eq 'Plan') {
    [pscustomobject]@{
        Target = 'Forerunner 265\Internal Storage\Podcasts\Recente podcasts'
        TestFile = $leaf
        LocalBytes = $audio.Length
        ExistsOnGarmin = $matches.Count -eq 1
        NextAction = if ($matches.Count -eq 0) { 'Copy' } else { 'Delete na controle op het horloge' }
        Warning = 'Alleen lezen. Copy en Delete zijn aparte expliciete acties; MTP meldt geen betrouwbare bytevolledigheid.'
    } | ConvertTo-Json -Depth 3
    return
}

if ($Action -eq 'Copy') {
    if ($matches.Count -ne 0) { throw 'Het testbestand staat al op de Garmin; er wordt niets overschreven.' }
    $destination.CopyHere($source, 0)
    Write-Host "Kopieeropdracht gestart voor $leaf. Wacht tot Windows klaar is; controleer daarna de testmap en het horloge."
    return
}

if ($matches.Count -ne 1) { throw 'Het exacte testbestand staat niet op de Garmin; er wordt niets verwijderd.' }
# Only the uniquely identified, non-hash test MP3 can reach this call.
$matches[0].InvokeVerb('delete')
Write-Host "Verwijderopdracht gevraagd voor $leaf. Bevestig een eventuele Windows-dialoog en controleer daarna de testmap."
