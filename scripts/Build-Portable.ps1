[CmdletBinding()]
param(
    [string] $OutputDirectory = (Join-Path $PSScriptRoot '..\artifacts\podcast-sync-portable-win-x64')
)

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) {
    throw "De uitvoermap bestaat al: $output. Kies een nieuwe, lege bestemming."
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw '.NET 8 SDK ontbreekt of dotnet staat niet op PATH.'
}

$projects = @(
    @{ Name = 'BackupInspector'; Path = 'src\PodcastSync.BackupInspector\PodcastSync.BackupInspector.csproj'; Exe = 'PodcastSync.BackupInspector.exe' },
    @{ Name = 'MtpProbe'; Path = 'src\PodcastSync.MtpProbe\PodcastSync.MtpProbe.csproj'; Exe = 'PodcastSync.MtpProbe.exe' },
    @{ Name = 'Desktop'; Path = 'src\PodcastSync.Desktop\PodcastSync.Desktop.csproj'; Exe = 'PodcastSync.Desktop.exe' }
)

New-Item -ItemType Directory -Path $output -ErrorAction Stop | Out-Null
foreach ($project in $projects) {
    $projectPath = Join-Path $root $project.Path
    $target = if ($project.Name -eq 'Desktop') {
        Join-Path $output 'app'
    } else {
        Join-Path (Join-Path $output 'bin') $project.Name
    }
    Write-Host "Publiceer $($project.Name) voor Windows x64"
    & dotnet publish $projectPath --configuration Release --runtime win-x64 --self-contained true --output $target '-p:PublishTrimmed=false'
    if ($LASTEXITCODE -ne 0) { throw "Publiceren van $($project.Name) mislukt." }
    if (-not (Test-Path -LiteralPath (Join-Path $target $project.Exe) -PathType Leaf)) {
        throw "Het gepubliceerde programma ontbreekt: $($project.Exe)"
    }
}

$scripts = Join-Path $output 'scripts'
New-Item -ItemType Directory -Path $scripts -ErrorAction Stop | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Sync-Podcasts.ps1') -Destination $scripts -ErrorAction Stop
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Prepare-GarminAlbum.ps1') -Destination $scripts -ErrorAction Stop
Copy-Item -LiteralPath (Join-Path $root 'docs\portable.md') -Destination (Join-Path $output 'LEES-MIJ.md') -ErrorAction Stop

Write-Host "Draagbaar pakket klaar: $output"
Write-Host 'Start app\PodcastSync.Desktop.exe. FFmpeg moet nog apart op PATH staan.'
