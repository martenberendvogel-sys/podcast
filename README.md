# Podcast Sync

Onderzoeksprototype voor een betrouwbare synchronisatieketen van Podcast Addict via Windows naar een Garmin Forerunner 265.

Lees bij hervatten eerst [de actuele projectstand](docs/handoff.md).

De eerste stap leest nu de afspeellijst uit een echte Podcast Addict-back-up. Handmatige Garmin-overdracht werkt al; de veilige automatisering daarvan vereist nog een gerichte hardwareproef. Luisterposities zijn optioneel voor versie 1.

Een aparte, alleen-lezen Windows-proef gebruikt nu de directe WPD/MTP-interface in plaats van Verkenner. Sluit de Garmin aan en voer uit:

```powershell
dotnet run --project .\src\PodcastSync.MtpProbe -- probe
```

De proef toont of `Internal Storage\Podcasts\Recente podcasts` via WPD bereikbaar is en welke bestanden/groottes het apparaat rechtstreeks meldt. Hij kopieert of verwijdert niets. Deze route is nog niet op het horloge getest; zie [de projectstand](docs/handoff.md).

## Back-upinspecteur draaien

Deze repository gebruikt .NET 8. Installeer een SDK of gebruik in de cloudomgeving `/workspace/.dotnet/dotnet`. `NuGet.Config` voegt de publieke pakketbron nuget.org voor dit project toe, ook als op de pc nog geen globale NuGet-bronnen zijn ingesteld.

```bash
dotnet restore tests/PodcastSync.BackupInspector.Tests --locked-mode
dotnet run --project src/PodcastSync.BackupInspector -- playlist /pad/naar/PodcastAddict.backup
dotnet run --project src/PodcastSync.BackupInspector -- compare /pad/naar/oude.backup /pad/naar/nieuwe.backup
dotnet run --project src/PodcastSync.BackupInspector -- prepare /pad/naar/nieuwe.backup /pad/naar/lokale-stagingmap --limit 1
```

`playlist` toont de afleveringen in volgorde, met een stabiele sleutel en audiolink. `compare` toont welke afleveringen zijn toegevoegd, verwijderd of gebleven tussen twee back-ups. Er wordt nog niets naar de Garmin geschreven. De databasekoppeling is met twee echte back-ups gevalideerd: één nieuwe aflevering werd correct herkend.

`prepare` haalt geselecteerde MP3-bestanden op vanaf de audiolinks en bewaart ze in een beheerde lokale map met een manifest. De opdracht verwijdert geen audio en schrijft niet naar het horloge. Begin met `--limit 1`; zonder limiet worden alle geselecteerde afleveringen voorbereid. Met de back-up van 25 oktober zijn alle 10 actuele afleveringen op de persoonlijke Windows-pc voorbereid en via gecontroleerde één-voor-één kopieën in de Garmin-map geplaatst. Volledige automatische batchoverdracht en opruimen zijn nog niet gebouwd. Zie [de Garmin-proef](docs/garmin-validation.md).

De oudere opdracht `inspect` blijft beschikbaar voor losse JSON-, XML- en ZIP-bestanden. Voor `.backup` gebruikt die dezelfde Podcast Addict-parser als `playlist`.

Zie [de instructies voor een testback-up](docs/backup-capture.md) voordat je een bestand aanlevert.

Een los MP3- of M4B-audioboek kun je met [de audioboeksplitser](docs/audiobook-split.md) in korte, genummerde MP3-stukken voorbereiden. Dat is een aparte side quest; het boek gaat niet in de podcastplaylist of in Git.

GitHub Actions voert de controles ook op Windows uit en publiceert bij een geslaagde run een Windows-build als artifact. Daarvoor zijn geen privé-back-ups nodig.

## Huidige verificatie

```bash
dotnet run --project tests/PodcastSync.BackupInspector.Tests
```

Dit test JSON-, XML- en ZIP-invoer, vergelijkt twee synthetische Podcast Addict-back-ups en controleert download, hergebruik en behoud van bestaande audio met een gesimuleerde bron.

## Volgende praktische onderzoeken

1. Test op een later moment of een verwijderde playlistaflevering correct wordt herkend.
2. Test op Windows of één MP3 uit de playlist kan worden opgehaald.
3. Inventariseer alleen-lezen hoe de Garmin via USB door Windows wordt gepresenteerd.
4. Onderzoek welke van de al werkende handmatige Garmin-routes (Verkenner of Garmin Express) veilig te automatiseren is.

