# Garmin-validatie op Windows 11

De cloudomgeving kan de fysieke Forerunner 265 niet zien. Praktische proeven gebeuren op een Windows-pc met het horloge via USB aangesloten.

## Al bevestigd door de gebruiker

De gebruiker downloadt MP3's al handmatig en heeft die zowel via Garmin Express als via Windows Verkenner op de Garmin gezet. De handmatige basisroute hoeft dus niet opnieuw te worden bewezen. De precieze automatische schrijflocatie en veilige verwijder-/bijwerkregels zijn nog onbekend.

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

Het script inventariseert Garmin-apparaten in Windows, Verkenner en eventuele volumes. Het kopieert of verwijdert niets. Handmatige MP3-overdracht werkt al; het script moet helpen de juiste route voor automatisering vast te stellen.

## Volgende overdrachtsproef

Gebruik één door `prepare` gemaakte MP3 voor een beperkte overdrachtsproef. Vergelijk of de al gebruikte Verkenner-route en Garmin Express-route ook met dit voorbereide bestand werken. Noteer de precieze Windows-locatie waar het horloge zichtbaar wordt en of de muziekspeler de bestandstitel en volgorde goed leest.

Noteer zonder privé-URL's te delen wat voor automatische overdracht nodig is. Laat de app pas naar Garmin-opslag schrijven als doelmap, bestandsgedrag en herstel bij onderbreking zijn vastgesteld.
