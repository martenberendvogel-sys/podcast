# Stand van het project – 12 oktober 2026

## Wat werkt

- `main` bevat een .NET 8 CLI voor `playlist`, `compare` en `prepare`.
- Een volledige Podcast Addict-back-up is een ZIP met een SQLite-database. Voor de gebruikte appversie leest `ordered_list.type=1` en `filter=-1` de handmatige playlist.
- Twee echte, privé gehouden back-ups bevestigden de selectie: eerst 16 afleveringen, daarna 17; precies één toevoeging op positie 7, geen verwijderingen.
- Alle geselecteerde afleveringen hebben HTTPS-MP3-links. De `prepare`-opdracht bewaart MP3's in een lokale stagingmap, schrijft een manifest, controleert SHA-256 en hergebruikt bestaande bestanden. De 20 geautomatiseerde controles slagen met een gesimuleerde audiobron.
- `scripts/Inspect-Garmin.ps1` is een alleen-lezen inventarisatie voor Windows; dit script is nog niet op de aangesloten Garmin getest.
- De gebruiker heeft losse MP3's al handmatig via Garmin Express én Windows Verkenner op de Garmin gezet; die basisroute is bevestigd.
- Een aparte `scripts/Split-Audiobook.ps1` deelt een lang MP3- of M4B-boek op in genummerde MP3-stukken. Het is geen onderdeel van de podcastplaylist; testen op het echte boek en horloge moet nog gebeuren.
- GitHub Actions bouwt en test de .NET-app op een Windows-runner en bewaart een Windows-artifact; de uitslag van deze nieuwe workflow moet nog worden gecontroleerd.
- Op de persoonlijke pc was geen NuGet-bron ingesteld, waardoor `Microsoft.Data.Sqlite` niet kon worden hersteld (`NU1100`). De repository bevat nu `NuGet.Config` met nuget.org; herstel en `prepare --limit 1` moeten op die pc opnieuw worden geprobeerd.

## Wat nog niet is bewezen

- Een echte MP3-download vanuit de Windows-pc. De cloudproxy weigert de huidige podcastbronnen met 403; dit zegt niets definitiefs over toegang vanaf de pc.
- Of een door `prepare` gemaakte MP3 op dezelfde manier overgedragen wordt, en hoe de bestaande Verkenner-/Express-route veilig geautomatiseerd kan worden.
- Rechtstreeks kopiëren via USB door onze app en automatisch synchroniseren met Garmin.
- Herkennen van een verwijderde playlistaflevering in een derde echte back-up.

## Eerstvolgende stap

Test `prepare ... --limit 1` en de alleen-lezen Garmin-inventarisatie later op een persoonlijke pc. Een beveiligingsregel blokkeert zelf gebouwde `.exe`-bestanden op de beheerde Windows-pc. De gebruiker heeft handmatige MP3-overdracht al bewezen; onderzoek nu welke route veilig automatisch kan worden aangestuurd. Zie [garmin-validation.md](garmin-validation.md).

Automatische overdracht van de telefoonback-up via OneDrive of lokale wifi is bewust geparkeerd. Gebruik voorlopig een handmatig gemaakte volledige back-up. Luisterpositie blijft optioneel voor versie 1.

Back-ups, audiobestanden en stagingmanifesten kunnen privégegevens bevatten; ze horen niet in Git. Alleen broncode, tests en projectdocumentatie worden gecommit.
