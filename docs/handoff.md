# Stand van het project – 24 oktober 2026

## Wat werkt

- `main` bevat een .NET 8 CLI voor `playlist`, `compare` en `prepare`.
- Een volledige Podcast Addict-back-up is een ZIP met een SQLite-database. Voor de gebruikte appversie leest `ordered_list.type=1` en `filter=-1` de handmatige playlist.
- Twee echte, privé gehouden back-ups bevestigden de selectie: eerst 16 afleveringen, daarna 17; precies één toevoeging op positie 7, geen verwijderingen.
- Alle geselecteerde afleveringen hebben HTTPS-MP3-links. De `prepare`-opdracht bewaart MP3's in een lokale stagingmap, schrijft een manifest, controleert SHA-256 en hergebruikt bestaande bestanden. De 20 geautomatiseerde controles slagen met een gesimuleerde audiobron.
- `scripts/Inspect-Garmin.ps1` is een alleen-lezen inventarisatie voor Windows; dit script is nog niet op de aangesloten Garmin getest.
- De gebruiker heeft losse MP3's al handmatig via Garmin Express én Windows Verkenner op de Garmin gezet; die basisroute is bevestigd.
- Een aparte `scripts/Split-Audiobook.ps1` deelde het echte M4B-boek op in 39 genummerde MP3-stukken. De Garmin speelt die stukken volgens de gebruiker automatisch in volgorde af. Dit staat los van de podcastplaylist.
- GitHub Actions bouwt en test de .NET-app op een Windows-runner en bewaart een Windows-artifact; de uitslag van deze nieuwe workflow moet nog worden gecontroleerd.
- Op de persoonlijke pc ontbrak een NuGet-bron. `NuGet.Config` met nuget.org loste `NU1100` op. Daarna blokkeerde SQLite-pooling kort de tijdelijke `backup.db` op Windows; pooling is voor die leesverbinding uitgeschakeld.
- Met de volledige back-up van 24 oktober meldde `prepare --limit 1`: 14 geselecteerd, 1 gedownload, 13 nog niet voorbereid. De MP3 staat lokaal in een privé-stagingmap; afspelen van dit bestand en overdracht naar Garmin moeten nog worden gecontroleerd.

## Wat nog niet is bewezen

- Of de door `prepare` gedownloade MP3 volledig afspeelt op de pc en Garmin.
- Of een door `prepare` gemaakte MP3 op dezelfde manier overgedragen wordt, en hoe de bestaande Verkenner-/Express-route veilig geautomatiseerd kan worden.
- Rechtstreeks kopiëren via USB door onze app en automatisch synchroniseren met Garmin.
- Herkennen van een verwijderde playlistaflevering in een derde echte back-up.

## Eerstvolgende stap

Controleer het ene voorbereide MP3-bestand op de persoonlijke pc en inventariseer het aangesloten Garmin-horloge alleen-lezen. De gebruiker heeft handmatige MP3-overdracht al bewezen; onderzoek nu welke route veilig automatisch kan worden aangestuurd. Zie [garmin-validation.md](garmin-validation.md).

Automatische zichtbare overdracht van de telefoonback-up naar een gewone Google Drive-map is nog niet ingericht; de gebruiker wil dat voor later. Gebruik voorlopig een handmatig gemaakte volledige back-up. Luisterpositie blijft optioneel voor versie 1.

Back-ups, audiobestanden en stagingmanifesten kunnen privégegevens bevatten; ze horen niet in Git. Alleen broncode, tests en projectdocumentatie worden gecommit.
