# Stand van het project – 24 oktober 2026

## Wat werkt

- `main` bevat een .NET 8 CLI voor `playlist`, `compare` en `prepare`.
- Een volledige Podcast Addict-back-up is een ZIP met een SQLite-database. Voor de gebruikte appversie leest `ordered_list.type=1` en `filter=-1` de handmatige playlist.
- Twee echte, privé gehouden back-ups bevestigden de selectie: eerst 16 afleveringen, daarna 17; precies één toevoeging op positie 7, geen verwijderingen.
- Alle geselecteerde afleveringen hebben HTTPS-MP3-links. De `prepare`-opdracht bewaart MP3's in een lokale stagingmap, schrijft een manifest, controleert SHA-256 en hergebruikt bestaande bestanden. De 20 geautomatiseerde controles slagen met een gesimuleerde audiobron.
- `scripts/Inspect-Garmin.ps1` is een alleen-lezen inventarisatie voor Windows en controleert nu ook de exacte map `Podcasts\Recente podcasts`; dit script moet nog op de aangesloten Garmin worden getest.
- De gebruiker heeft losse MP3's al handmatig via Garmin Express én Windows Verkenner op de Garmin gezet; die basisroute is bevestigd.
- Een aparte `scripts/Split-Audiobook.ps1` deelde het echte M4B-boek op in 39 genummerde MP3-stukken. De Garmin speelt die stukken volgens de gebruiker automatisch in volgorde af. Dit staat los van de podcastplaylist.
- GitHub Actions bouwt en test de .NET-app op een Windows-runner en bewaart een Windows-artifact; de uitslag van deze nieuwe workflow moet nog worden gecontroleerd.
- Op de persoonlijke pc ontbrak een NuGet-bron. `NuGet.Config` met nuget.org loste `NU1100` op. Daarna blokkeerde SQLite-pooling kort de tijdelijke `backup.db` op Windows; pooling is voor die leesverbinding uitgeschakeld.
- Met de volledige back-up van 24 oktober meldde `prepare --limit 3`: 14 geselecteerd, 3 lokaal voorbereid (1 nieuw gedownload, 2 hergebruikt), 11 nog niet voorbereid. De MP3's blijven in een privé-stagingmap.
- De Garmin verschijnt in Verkenner als `Deze pc\Forerunner 265\Internal Storage\Podcasts`, met bestaande podcastsubmappen. De eigen submap `Recente podcasts` is via Verkenner toegevoegd door een lege pc-map naar het horloge te kopiëren. De app-gedownloade MP3 staat daarin, toont op het horloge de juiste titel en speelt af.
- De read-only inventarisatie vond WPD/Portable Device en het volledige doelpad, maar de eerste versie telde nul `*.mp3` op naam terwijl Verkenner het bestand toont. Mogelijk verbergt de Shell de extensie; de inventarisatie rapporteert nu ruwe bestandsaantallen en naam/type/grootte voor diagnose.
- De tweede inventarisatie vond exact één bestand in `Recente podcasts`; Windows meldt de hashnaam zonder extensie, type `MP3 File` en grootte `0` ondanks werkende weergave. `scripts/Copy-OneToGarmin.ps1` (standaard alleen voorbeeld, `-Apply` voor één ontbrekende MP3) kopieerde daarna de tweede aflevering via de Windows-apparaatinterface. De gebruiker ziet twee bestanden in de map en beide spelen op de Garmin af.
- Een derde aflevering is lokaal voorbereid en met hetzelfde script naar de Garmin gekopieerd; alle drie zijn zichtbaar en de derde speelt af. De PowerShell-prompt kwam vrijwel direct terug, zodat `CopyHere` niet als voltooiingssignaal voor meerdere kopieën mag worden gebruikt.
- `Inspect-Garmin.ps1 -StagingDirectory <map>` maakt nu ook een alleen-lezen `SyncPlan`. Op de persoonlijke pc gaf de back-up van 24 oktober exact: 14 geselecteerd, 3 lokaal voorbereid, 3 op Garmin, 11 nog te downloaden, 0 te kopiëren, 0 verwijderkandidaten, 0 onbekende apparaatsleutels en 0 lokale problemen. De planner vond alle drie werkende MP3's op het horloge.

## Wat nog niet is bewezen

- Of de door `prepare` gedownloade MP3 helemaal tot het einde afspeelt; verschijnen en starten in de nieuwe Garmin-map zijn bevestigd.
- Een betrouwbaar voltooiingssignaal voor MTP-kopieën. `CopyHere` keert vrijwel direct terug en Garmin rapporteert grootte `0`, dus naam of PowerShell-prompt alleen is geen bytecontrole.
- Veilige bulkoverdracht van alle geselecteerde afleveringen en het verwijderen van oude bestanden uit uitsluitend de eigen Garmin-map.
- Een verse volledige Podcast Addict-back-up als bron voor opschonen. De huidige back-up van 24 oktober is geschikt voor een kopieertimingproef, niet voor de definitieve opruimactie als de telefoonplaylist inmiddels is veranderd.
- Herkennen van een verwijderde playlistaflevering in een derde echte back-up.

## Eerstvolgende stap

Maak een verse volledige Podcast Addict-back-up en vergelijk die met de back-up van 24 oktober; gebruik daarna dezelfde stagingmap om de nieuwe playlist te plannen. Onderzoek daarnaast een betrouwbaar voltooiingssignaal voor MTP-overdracht voordat meerdere afleveringen of verwijderingen worden geautomatiseerd. Zie [garmin-validation.md](garmin-validation.md).

De gewenste definitieve sync is incrementeel: de actuele Podcast Addict-playlist is leidend; ontbrekende afleveringen toevoegen, bestaande behouden en alleen niet meer geselecteerde, eerder beheerde bestanden uit `Podcasts\Recente podcasts` verwijderen. Een verse back-up en volledige lokale voorbereiding zijn vereist voordat er iets op de Garmin wordt opgeruimd. De verwijderstap is nog niet geïmplementeerd.

Automatische zichtbare overdracht van de telefoonback-up naar een gewone Google Drive-map is nog niet ingericht; de gebruiker wil dat voor later. Gebruik voorlopig een handmatig gemaakte volledige back-up. Luisterpositie blijft optioneel voor versie 1.

Back-ups, audiobestanden en stagingmanifesten kunnen privégegevens bevatten; ze horen niet in Git. Alleen broncode, tests en projectdocumentatie worden gecommit.
