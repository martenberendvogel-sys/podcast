# Podcast Sync

Onderzoeksprototype voor een betrouwbare synchronisatieketen van Podcast Addict via Windows naar een Garmin Forerunner 265.

Lees bij hervatten eerst [de actuele projectstand](docs/handoff.md).

De Windows-app leest een handmatig gemaakte Podcast Addict-back-up, downloadt de actuele playlist, zet uniforme Garmin-tags en synchroniseert de definitieve beheerde map `Podcasts\Recente podcasts` op een Forerunner 265. Een echte gewijzigde playlist en groepering/volgorde van tien afleveringen zijn op het horloge bevestigd. Andere Garmin-mappen blijven buiten scope. Luisterposities en automatische aanlevering van de telefoonback-up zijn nog niet inbegrepen.

## Eén opdracht voor voorbereiding en synchronisatie

Maak eerst een **nieuwe volledige** Podcast Addict-back-up en kopieer die naar de pc. Sluit de Forerunner 265 via USB aan. Dit is één PowerShell-opdracht, ook als scripts in je huidige sessie geblokkeerd zijn:

```powershell
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File "$env:USERPROFILE\podcast\scripts\Sync-Podcasts.ps1" -BackupPath 'C:\video\AI\PodcastAddict_YYYYMMDD_HHMMSS.backup' -Apply
```

Vervang de voorbeeldnaam door die van de nieuwe back-up. Het script gebruikt standaard `podcast-staging` naast de back-up en maakt een aparte, controleerbare Garmin-klare map voor deze back-up. Eerst worden alle lokale bestanden voorbereid en het alleen-lezen Garmin-plan getoond; met `-Apply` wordt daarna uitsluitend `Internal Storage\Podcasts\Recente podcasts` bijgewerkt. Zonder `-Apply` stopt het na het plan. Het onderliggende programma weigert een back-up ouder dan 48 uur, onbekende bestanden of submappen in de doelmap en onvolledige lokale bestanden. Bij een onderbroken overdracht blijft de lokale set staan; dezelfde opdracht kan opnieuw worden uitgevoerd, nadat het plan is gecontroleerd. De privé-back-up en MP3's worden niet in Git opgeslagen.

Een aparte, alleen-lezen Windows-proef gebruikt nu de directe WPD/MTP-interface in plaats van Verkenner. Sluit de Garmin aan en voer uit:

```powershell
dotnet run --project .\src\PodcastSync.MtpProbe -- probe
```

De proef toont of `Internal Storage\Podcasts\Recente podcasts` via WPD bereikbaar is en welke bestanden/groottes het apparaat rechtstreeks meldt. Hij kopieert of verwijdert niets; zie [de projectstand](docs/handoff.md).

De alleen-lezen proef is inmiddels op de aangesloten Forerunner geslaagd: alle tien MP3's en hun echte WPD-groottes zijn zichtbaar. Voor een geïsoleerde rondreisproef zijn `copy-test` en `delete-test` beschikbaar. Ze accepteren uitsluitend herkenbare proefbestanden uit de exacte lokale testmappen en raken geen hashvormige playlistbestanden aan. `copy-test` leest de upload terug en vergelijkt SHA-256; `delete-test` controleert dezelfde inhoud vóór verwijdering.

Ook de directe WPD-rondreisproef is geslaagd, inclusief volledige terugleescontrole en verwijderen zonder Windows-pop-up. De afzonderlijke opdrachten `sync-plan` en `sync-apply` blijven beschikbaar voor diagnose. `sync-plan` leest alleen; `sync-apply` vereist een back-up met Podcast Addict-datum in de bestandsnaam van maximaal 48 uur oud en wijzigt uitsluitend hashvormige MP3's in `Podcasts\Recente podcasts`. Een voorbeeld van het plan:

```powershell
dotnet run --project .\src\PodcastSync.MtpProbe -- sync-plan 'C:\video\AI\PodcastAddict_20261025_113232.backup' 'C:\video\AI\Garmin klaar omgekeerd'
```

Het programma valideert back-upvolgorde, lokaal manifest en SHA-256, en leest bestaande Garmin-MP3's volledig terug. `sync-apply` is met een veranderde echte playlist en daarna met tien hertagde afleveringen op de Garmin getest. Onbekende bestanden of submappen in de beheerde map blokkeren automatische wijzigingen. Bij een mislukte vervanging stopt de opdracht; het lokale MP3-bestand blijft beschikbaar om een volgende run te herstellen.

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

