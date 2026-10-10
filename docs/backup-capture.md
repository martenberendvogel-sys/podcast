# Podcast Addict-playlistwijziging controleren

Twee echte back-ups zijn vergeleken: de tweede bevat één nieuwe aflevering op positie 7 en de eerdere 16 zijn behouden. De volgende proef kan een verwijdering uit de playlist testen.

## Gewenste inhoud

Voor een volgende vergelijkingsproef:

1. verwijder één testaflevering uit de playlist;
2. maak daarna opnieuw een **volledige back-up** in Podcast Addict;
3. vergelijk die met de vorige back-up.

Een OPML-export is voor deze proef niet nodig. Noteer welke aflevering is verwijderd, zodat we de vergelijking kunnen toetsen.

## Veilig aanleveren

- Een volledige Podcast Addict-back-up kan aanmeldgegevens voor podcastfeeds bevatten. Behandel het bestand als privé; plaats het niet in GitHub.
- De inspecteur schrijft nooit terug naar de back-up.
- Lever het bestand hier aan als upload. De `.backup`-parser leest de SQLite-database uit het ZIP-archief.

## Uitvoeren op Windows

Na installatie van .NET 8, vanuit de repositorymap:

```powershell
dotnet run --project src/PodcastSync.BackupInspector -- playlist 'C:\pad\naar\PodcastAddict.backup'
dotnet run --project src/PodcastSync.BackupInspector -- compare 'C:\pad\naar\oud.backup' 'C:\pad\naar\nieuw.backup'
```

De uitvoer bevat titels en URL's; behandel het rapport daarom ook als privé.
