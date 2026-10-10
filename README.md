# Podcast Sync

Onderzoeksprototype voor een betrouwbare synchronisatieketen van Podcast Addict via Windows naar een Garmin Forerunner 265.

De eerste stap leest nu de afspeellijst uit een echte Podcast Addict-back-up. Garmin-overdracht wordt pas na praktische hardwaretests ontworpen. Luisterposities zijn optioneel voor versie 1.

## Back-upinspecteur draaien

Deze repository gebruikt .NET 8. Installeer een SDK of gebruik in de cloudomgeving `/workspace/.dotnet/dotnet`.

```bash
dotnet restore tests/PodcastSync.BackupInspector.Tests --locked-mode
dotnet run --project src/PodcastSync.BackupInspector -- playlist /pad/naar/PodcastAddict.backup
dotnet run --project src/PodcastSync.BackupInspector -- compare /pad/naar/oude.backup /pad/naar/nieuwe.backup
dotnet run --project src/PodcastSync.BackupInspector -- prepare /pad/naar/nieuwe.backup /pad/naar/lokale-stagingmap --limit 1
```

`playlist` toont de afleveringen in volgorde, met een stabiele sleutel en audiolink. `compare` toont welke afleveringen zijn toegevoegd, verwijderd of gebleven tussen twee back-ups. Er wordt nog niets naar de Garmin geschreven. De databasekoppeling is met twee echte back-ups gevalideerd: één nieuwe aflevering werd correct herkend.

`prepare` haalt geselecteerde MP3-bestanden op vanaf de audiolinks en bewaart ze in een beheerde lokale map met een manifest. De opdracht verwijdert geen audio en schrijft niet naar het horloge. Begin met `--limit 1`; zonder limiet worden alle geselecteerde afleveringen voorbereid. In de cloudomgeving blokkeert de netwerkproxy de huidige podcastbronnen, dus de echte download moet nog op de Windows-pc worden getest. Zie [de Garmin-proef](docs/garmin-validation.md).

De oudere opdracht `inspect` blijft beschikbaar voor losse JSON-, XML- en ZIP-bestanden. Voor `.backup` gebruikt die dezelfde Podcast Addict-parser als `playlist`.

Zie [de instructies voor een testback-up](docs/backup-capture.md) voordat je een bestand aanlevert.

## Huidige verificatie

```bash
dotnet run --project tests/PodcastSync.BackupInspector.Tests
```

Dit test JSON-, XML- en ZIP-invoer, vergelijkt twee synthetische Podcast Addict-back-ups en controleert download, hergebruik en behoud van bestaande audio met een gesimuleerde bron.

## Volgende praktische onderzoeken

1. Test op een later moment of een verwijderde playlistaflevering correct wordt herkend.
2. Test op Windows of één MP3 uit de playlist kan worden opgehaald.
3. Inventariseer alleen-lezen hoe de Garmin via USB door Windows wordt gepresenteerd.
4. Importeer één voorbereide MP3 via Garmin Express en controleer afspelen op het horloge.

