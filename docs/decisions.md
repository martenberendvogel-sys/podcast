# Besluitenlog

| Datum | Besluit | Reden | Status |
| --- | --- | --- | --- |
| 2026-10-10 | C# en .NET 8 gebruiken voor de kern. | Past bij de beoogde Windows 11-toepassing en is lokaal testbaar. | Actief |
| 2026-10-10 | Beginnen met analyse van echte back-ups. | Het schema en de beschikbare luisterposities zijn nog onbekend. | Actief |
| 2026-10-10 | Garmin-overdracht pas automatiseren na een hardwareproef. | USB-zichtbaarheid bewijst geen ondersteunde mediaoverdracht. | Actief |
| 2026-10-10 | Eenrichtingssynchronisatie is versie 1. | Tweerichtingsposities zijn technisch onbewezen. | Actief |
| 2026-10-10 | Playlist/wachtrij is de primaire synchronisatiebron; luisterpositie is optioneel. | De Garmin moet vooral dezelfde geselecteerde afleveringen bevatten, ook wanneer de telefoon streamt. | Actief |
| 2026-10-10 | Lees de handmatige playlist via `ordered_list.type=1` en `filter=-1` uit de volledige SQLite-back-up. | De telling kwam overeen met de app; een tweede back-up liet precies één toevoeging zien. | Bevestigd voor deze appversie |
| 2026-10-10 | Gebruik voorlopig handmatige volledige back-ups. | Automatische back-ups verschijnen nog niet betrouwbaar in de cloudopslag van de gebruiker. | Actief; automatisering geparkeerd |
| 2026-10-10 | Onderzoek lokale wifi-overdracht van de back-upmap naar Windows. | Automatische volledige back-ups staan inmiddels aan en de map ligt in gedeelde Android-opslag; cloud-overdracht is onbetrouwbaar. | Te testen op telefoon en Windows-pc |
| 2026-10-10 | Bereid MP3-bestanden eerst in een lokale pc-map voor. | De playlist heeft bruikbare MP3-links; de app mag niet ongecontroleerd naar Garmin schrijven. | Staging getest met gesimuleerde bron; automatische overdracht volgt later |
| 2026-10-12 | De handmatige MP3-overdracht via Verkenner en Garmin Express geldt als bevestigd; onderzoek nu automatisering van deze routes. | De gebruiker heeft beide routes al gebruikt. | Exacte USB-doelmap en automatische schrijfmethode nog te testen |
| 2026-10-12 | Splits het lange audioboek los van de podcast-sync in korte MP3-stukken. | Bij wisselen van MP3 raakt het horloge de positie in het 12-uursboek kwijt. | Hulpmiddel gemaakt; proef op persoonlijk systeem volgt |
| 2026-10-24 | Beperk automatische Garmin-overdracht tot een nieuwe map `Podcasts\Recente podcasts`; raak bestaande podcastsubmappen niet aan. | Het horloge is via Verkenner bereikbaar en de gebruiker wil alle actuele playlistafleveringen in één eigen map. | Twee app-gedownloade MP3's spelen af, waaronder één automatisch via Windows Shell gekopieerd; bulk en verwijdering nog onbewezen |
| 2026-10-25 | Gebruik de actuele Podcast Addict-playlist als bron van waarheid: kopieer ontbrekende afleveringen en ruim alleen eerder beheerde afleveringen op die niet meer in die playlist staan. | Een volledige wis-en-herkopieeractie zou onnodig tijd kosten en posities van blijvende afleveringen verstoren; een oude back-up zou verwijderde afleveringen weer toevoegen. | Besloten; veilige MTP-verwijdering nog niet gebouwd of getest |
| 2026-10-27 | `Internal Storage\Podcasts\Recente podcasts` is de definitieve, uitsluitend door Podcast Sync beheerde Garmin-map. Andere Garmin-mappen blijven buiten scope. | De echte incrementele sync, uniforme groep en volgorde van tien afleveringen zijn op het horloge bevestigd; de gebruiker heeft de map expliciet als definitief aangewezen. | Actief; onbekende bestanden en submappen blokkeren automatische wijzigingen |
| 2026-10-28 | De nieuwste volledige Podcast Addict-back-up via USB/MTP ophalen en daarna met één opdracht de Garmin synchroniseren. | De S24 FE geeft de back-up via WPD vrij; de app verifieert de pc-kopie met een tweede SHA-256-lezing. | Op telefoon en Garmin bevestigd: 7 gewenste afleveringen, 2 oude verwijderd; Android-ontgrendeling blijft handmatig |
| 2026-10-29 | Back-ups voorlopig op aanvraag in Podcast Addict zelf starten; daarna kan het Windows-venster de nieuwste back-up ophalen. | De handmatige knop schrijft aantoonbaar een lokaal `.backup`-bestand. Er is nog geen verifieerbare, ondersteunde externe back-upactie/API gevonden; MTP kan alleen bestaande bestanden ophalen. | Handmatige stap blijft; een officiële externe trigger mag later opnieuw worden onderzocht |

## Werkwijze voor vervolgchats

Lees eerst `README.md`, `docs/requirements.md`, `docs/feasibility.md` en dit bestand. Gebruik de bestaande checkout; maak geen apart worktree tenzij de gebruiker daar expliciet om vraagt. Werk één onzekerheid tegelijk af en leg nieuwe technische bevindingen hier vast.

