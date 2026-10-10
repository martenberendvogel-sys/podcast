# Stand van het project – 10 oktober 2026

## Wat werkt

- `main` bevat een .NET 8 CLI voor `playlist`, `compare` en `prepare`.
- Een volledige Podcast Addict-back-up is een ZIP met een SQLite-database. Voor de gebruikte appversie leest `ordered_list.type=1` en `filter=-1` de handmatige playlist.
- Twee echte, privé gehouden back-ups bevestigden de selectie: eerst 16 afleveringen, daarna 17; precies één toevoeging op positie 7, geen verwijderingen.
- Alle geselecteerde afleveringen hebben HTTPS-MP3-links. De `prepare`-opdracht bewaart MP3's in een lokale stagingmap, schrijft een manifest, controleert SHA-256 en hergebruikt bestaande bestanden. De 20 geautomatiseerde controles slagen met een gesimuleerde audiobron.
- `scripts/Inspect-Garmin.ps1` is een alleen-lezen inventarisatie voor Windows; de Garmin is nog niet fysiek getest.

## Wat nog niet is bewezen

- Een echte MP3-download vanuit de Windows-pc. De cloudproxy weigert de huidige podcastbronnen met 403; dit zegt niets definitiefs over toegang vanaf de pc.
- Import via Garmin Express, verschijnen op de Forerunner 265 en afspelen op het horloge.
- Rechtstreeks kopiëren via USB en automatisch synchroniseren met Garmin.
- Herkennen van een verwijderde playlistaflevering in een derde echte back-up.

## Eerstvolgende stap

Voer op de Windows 11-pc de proef in [garmin-validation.md](garmin-validation.md) uit: bereid één MP3 voor met `prepare ... --limit 1`, inventariseer het aangesloten horloge alleen-lezen en importeer die MP3 via de al gebruikte Garmin Express-route. Noteer of downloaden, importeren en afspelen elk slagen. Pas daarna ontwerpen we automatische Garmin-overdracht.

Automatische overdracht van de telefoonback-up via OneDrive of lokale wifi is bewust geparkeerd. Gebruik voorlopig een handmatig gemaakte volledige back-up. Luisterpositie blijft optioneel voor versie 1.

Back-ups, audiobestanden en stagingmanifesten kunnen privégegevens bevatten; ze horen niet in Git. Alleen broncode, tests en projectdocumentatie worden gecommit.
