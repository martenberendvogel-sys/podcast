# Technische haalbaarheid – eerste bevindingen

## Bewezen in dit prototype

- Een lokale, platformonafhankelijke inspecteur kan JSON, XML en ZIP met JSON/XML doorzoeken.
- Kandidaatvelden voor podcasttitel, afleveringstitel, audioreferentie en luisterpositie worden als controleerbaar JSON-rapport getoond.
- Het rapport bevat per invoerbestand de aangetroffen veldnamen, zodat het werkelijke back-upschema zonder giswerk kan worden vastgelegd.
- De echte `.backup` is een ZIP met `podcastAddict.db` (SQLite). `ordered_list` met `type=1` en `filter=-1` bevatte 16 geordende afleveringen; de gebruiker bevestigde dat de app 16 playlistafleveringen toonde.
- Een tweede volledige back-up bevat 17 afleveringen. `compare` vond precies één toevoeging op positie 7, geen verwijderingen en 16 behouden afleveringen. Dit bevestigt de playlistkoppeling voor deze appversie.
- De 16 afleveringen in de eerste back-up en de 17 in de tweede hebben elk een HTTPS-audiolink, een podcastfeed en een unieke afgeleide sleutel. Ze zijn volgens de back-ups niet lokaal gedownload.
- Een OPML-export bevat abonnementen, maar geen playlistafleveringen.
- De lokale stagingopdracht kan MP3-bestanden downloaden, een controlesom bewaren en dezelfde audio bij een tweede run verifiëren en hergebruiken; dit is met een gesimuleerde HTTPS-bron getest.

## Nog te bewijzen met echte hardware en data

| Vraag | Benodigde proef | Besluit na de proef |
| --- | --- | --- |
| Verwijdering uit de playlist | Verwijder later een testaflevering en vergelijk twee volledige back-ups. | Controleer dat `compare` alleen die aflevering als verwijderd meldt. |
| Lokale wifi-overdracht | Test of een Android-synchronisatie-app de back-upmap onder gedeelde opslag (`Android/media`) kan lezen en nieuwe `.backup`-bestanden via het thuisnetwerk naar een Windows-map kan sturen. | Kies een beveiligde, eenrichtings-overdracht en controleer dat Windows alleen complete bestanden verwerkt. |
| Automatische volledige back-up | In de tweede back-up staat deze instelling aan en is een lokale back-upmap ingesteld. Controleer nog of een geplande back-up zonder handmatige actie verschijnt en de nieuwste playlist bevat. | Gebruik automatische back-ups als invoer wanneer timing en volledigheid zijn bewezen. |
| Audio voor streamingafleveringen | Voer `prepare ... --limit 1` uit op de Windows-pc. De cloudproxy weigert de huidige podcastbronnen met 403, dus een echte download is hier nog niet geverifieerd. | Bevestig dat de MP3 volledig en afspeelbaar is. |
| Garmin-opslag | Sluit de Forerunner op Windows aan, voer `scripts/Inspect-Garmin.ps1` uit en gebruik Garmin Express voor één MP3-proef. | Bepaal of het bestand op het horloge verschijnt en afspeelt. |

## Voorlopige architectuur

Voorlopig levert de gebruiker een handmatig gemaakte volledige back-up met de gewenste selectie aan de Windows .NET-app. Automatische volledige back-ups staan nu aan op de telefoon, maar automatische overdracht naar OneDrive werkt nog niet betrouwbaar. Overdracht via wifi onderzoeken we later; dezelfde wifi alleen geeft Windows geen toegang tot Android-bestanden. De Windows-app haalt waar mogelijk de geselecteerde audio op bij de oorspronkelijke podcastbron en houdt een lokale synchronisatiestatus bij. Pas nadat de Garmin-overdrachtsroute praktisch is bewezen, wordt USB-detectie en kopiëren toegevoegd. Luisterposities en tweerichtingssynchronisatie vallen buiten versie 1.

## Veiligheidsgrens

Het prototype leest alleen door de gebruiker aangeleverde back-ups. Het schrijft niet naar Android of Garmin. Een tijdelijk SQLite-bestand wordt in een privémap gemaakt en na het lezen verwijderd. De volledige back-up kan persoonlijke gegevens en aanmeldinformatie bevatten; voeg die niet toe aan Git.

