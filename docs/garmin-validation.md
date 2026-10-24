# Garmin-validatie op Windows 11

De cloudomgeving kan de fysieke Forerunner 265 niet zien. Praktische proeven gebeuren op een Windows-pc met het horloge via USB aangesloten.

## Al bevestigd door de gebruiker

De gebruiker downloadt MP3's al handmatig en heeft die zowel via Garmin Express als via Windows Verkenner op de Garmin gezet. De handmatige basisroute hoeft dus niet opnieuw te worden bewezen. De Garmin verschijnt onder `Deze pc\Forerunner 265\Internal Storage\Podcasts`, niet als gewone schijfletter. Daar staan bestaande podcastsubmappen; die blijven onaangeroerd.

De eigen .NET-app kan op de beheerde pc niet worden gestart door een beveiligingsregel van de beheerder; test de app later op een persoonlijke pc waarop eigen ontwikkelcode is toegestaan.

## App-proef op de persoonlijke pc

1. Open de repository op Windows en installeer de .NET 8-SDK.
2. Gebruik een volledige Podcast Addict-back-up. Bewaar die privé buiten de repository.
3. Bereid eerst één aflevering voor in een nieuwe lokale map, bijvoorbeeld `C:\PodcastSync\staging`:

```powershell
dotnet restore tests/PodcastSync.BackupInspector.Tests --locked-mode
dotnet run --project src/PodcastSync.BackupInspector -- prepare 'C:\pad\naar\PodcastAddict.backup' 'C:\PodcastSync\staging' --limit 1
```

De map bevat één MP3 met een bestandsnaam op basis van de aflevering-ID en een `podcast-sync-manifest.json`. Dat manifest koppelt de ID aan de titel en de positie in de playlist. Bij opnieuw draaien wordt de MP3 op grootte en SHA-256 gecontroleerd en hergebruikt. Het manifest bevat geen audiolinks, maar wel podcasttitels; bewaar de map privé.

## Horloge alleen-lezen inventariseren

Sluit de Forerunner via USB aan. Draai in PowerShell vanuit de repository:

```powershell
./scripts/Inspect-Garmin.ps1
```

Het script inventariseert Garmin-apparaten in Windows, Verkenner en eventuele volumes. Het controleert ook of `Internal Storage\Podcasts\Recente podcasts` via de Windows-apparaatinterface bereikbaar is en telt daar bestanden. De eerste bestandsnamen, typen en groottes worden getoond voor diagnose; deel die desgewenst zonder privé-informatie. Het kopieert of verwijdert niets.

## Volgende overdrachtsproef

De eerste door `prepare` gedownloade MP3 speelt op de pc én in de nieuwe Garmin-map `Podcasts\Recente podcasts` af. De Garmin toont de juiste MP3-titel, ondanks de hashachtige bestandsnaam. De map is gemaakt door een lege pc-map via Verkenner naar de Garmin te kopiëren; direct een map aanmaken op het apparaat lukte niet.

De map `Recente podcasts` wordt de enige door de app beheerde Garmin-map; bestaande podcastsubmappen zijn buiten scope. Verkenner toont het apparaat als draagbaar apparaat/MTP-kandidaat, dus een normaal `.NET File.Copy` naar een schijfletter is waarschijnlijk niet mogelijk. Laat de app pas naar Garmin-opslag schrijven als het mappad, kopieergedrag en herstel bij onderbreking zijn vastgesteld. Verwijder voorlopig niets van het horloge.
