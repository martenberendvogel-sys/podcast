# Podcast Addict-playlistwijziging controleren

De eerste echte back-up is gelezen. De 16 gevonden afleveringen komen overeen met de telling in de app. Voor de volgende proef is een tweede back-up nodig.

## Gewenste inhoud

Verander de playlist op de telefoon op een manier die je gemakkelijk kunt herkennen:

1. voeg één publieke streamingaflevering toe;
2. verwijder desgewenst één bestaande aflevering;
3. maak daarna opnieuw een **volledige back-up** in Podcast Addict.

Upload alleen die tweede `.backup`. Een OPML-export is voor deze proef niet nodig. Noteer welke aflevering is toegevoegd of verwijderd, zodat we de vergelijking kunnen toetsen.

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

