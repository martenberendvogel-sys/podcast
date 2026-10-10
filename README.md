# Podcast Sync

Onderzoeksprototype voor een betrouwbare synchronisatieketen van Podcast Addict via Windows naar een Garmin Forerunner 265.

De eerste stap leest nu de afspeellijst uit een echte Podcast Addict-back-up. Garmin-overdracht wordt pas na praktische hardwaretests ontworpen. Luisterposities zijn optioneel voor versie 1.

## Back-upinspecteur draaien

Deze repository gebruikt .NET 8. Installeer een SDK of gebruik in de cloudomgeving `/workspace/.dotnet/dotnet`.

```bash
dotnet restore tests/PodcastSync.BackupInspector.Tests --locked-mode
dotnet run --project src/PodcastSync.BackupInspector -- playlist /pad/naar/PodcastAddict.backup
dotnet run --project src/PodcastSync.BackupInspector -- compare /pad/naar/oude.backup /pad/naar/nieuwe.backup
```

`playlist` toont de afleveringen in volgorde, met een stabiele sleutel en audiolink. `compare` toont welke afleveringen zijn toegevoegd, verwijderd of gebleven tussen twee back-ups. Er wordt nog niets naar de Garmin geschreven. De gevonden databasekoppeling is op één echte back-up en de telling in de app gevalideerd; een tweede back-up met een veranderde playlist moet de wijzigingsdetectie nog bevestigen.

De oudere opdracht `inspect` blijft beschikbaar voor losse JSON-, XML- en ZIP-bestanden. Voor `.backup` gebruikt die dezelfde Podcast Addict-parser als `playlist`.

Zie [de instructies voor een testback-up](docs/backup-capture.md) voordat je een bestand aanlevert.

## Huidige verificatie

```bash
dotnet run --project tests/PodcastSync.BackupInspector.Tests
```

Dit test JSON-, XML- en ZIP-invoer en vergelijkt twee synthetische Podcast Addict-back-ups.

## Volgende praktische onderzoeken

1. Exporteer een tweede back-up nadat de playlist is gewijzigd en controleer de `compare`-uitvoer.
2. Leg vast hoe de back-up automatisch van Android naar Windows komt en test een publieke audiolink.
3. Test, zonder bestanden te schrijven, hoe de Garmin via USB door Windows wordt gepresenteerd.
4. Test één handmatige audio-overdracht voordat automatische Garmin-synchronisatie wordt gebouwd.

