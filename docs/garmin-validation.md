# Eerste Garmin-proef op Windows 11

De cloudomgeving kan de fysieke Forerunner 265 niet zien. Deze proef gebeurt op de Windows-pc met het horloge via USB aangesloten.

## Voorbereiding

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

Het script inventariseert Garmin-apparaten in Windows, Verkenner en eventuele volumes. Het kopieert of verwijdert niets. Een zichtbaar apparaat bewijst nog niet dat de muziekspeler losse MP3-bestanden accepteert.

## Eerste afspeelproef

Gebruik voor de eerste overdracht de Garmin Express-route die op deze pc al werkt. Selecteer de voorbereide MP3 uit de stagingmap, synchroniseer via Garmin Express, koppel het horloge veilig los en controleer op het horloge of de aflevering verschijnt en afspeelt.

Noteer zonder privé-URL's te delen: detectie in Windows, of Garmin Express de MP3 accepteert, of deze op het horloge verschijnt, en of afspelen werkt. Automatiseer het schrijven naar Garmin-opslag pas nadat deze route is bevestigd.
