# Technische haalbaarheid – eerste bevindingen

## Bewezen in dit prototype

- Een lokale, platformonafhankelijke inspecteur kan JSON, XML en ZIP met JSON/XML doorzoeken.
- Kandidaatvelden voor podcasttitel, afleveringstitel, audioreferentie en luisterpositie worden als controleerbaar JSON-rapport getoond.
- Het rapport bevat per invoerbestand de aangetroffen veldnamen, zodat het werkelijke back-upschema zonder giswerk kan worden vastgelegd.
- De echte `.backup` is een ZIP met `podcastAddict.db` (SQLite). `ordered_list` met `type=1` en `filter=-1` bevatte 16 geordende afleveringen; de gebruiker bevestigde dat de app 16 playlistafleveringen toonde.
- Een tweede volledige back-up bevat 17 afleveringen. `compare` vond precies één toevoeging op positie 7, geen verwijderingen en 16 behouden afleveringen. Dit bevestigt de playlistkoppeling voor deze appversie.
- Alle 16 geselecteerde afleveringen hebben een HTTPS-audiolink, een podcastfeed en een unieke afgeleide sleutel. Ze zijn volgens de back-up niet lokaal gedownload.
- Een OPML-export bevat abonnementen, maar geen playlistafleveringen.

## Nog te bewijzen met echte hardware en data

| Vraag | Benodigde proef | Besluit na de proef |
| --- | --- | --- |
| Verwijdering uit de playlist | Verwijder later een testaflevering en vergelijk twee volledige back-ups. | Controleer dat `compare` alleen die aflevering als verwijderd meldt. |
| Lokale wifi-overdracht | Test of een Android-synchronisatie-app de back-upmap onder gedeelde opslag (`Android/media`) kan lezen en nieuwe `.backup`-bestanden via het thuisnetwerk naar een Windows-map kan sturen. | Kies een beveiligde, eenrichtings-overdracht en controleer dat Windows alleen complete bestanden verwerkt. |
| Automatische volledige back-up | In de tweede back-up staat deze instelling aan en is een lokale back-upmap ingesteld. Controleer nog of een geplande back-up zonder handmatige actie verschijnt en de nieuwste playlist bevat. | Gebruik automatische back-ups als invoer wanneer timing en volledigheid zijn bewezen. |
| Audio voor streamingafleveringen | Neem één publiek beschikbare aflevering uit de testplaylist en bepaal of de pc de oorspronkelijke audiobron kan ophalen. | Kies directe download vanaf Windows of markeer de bron als niet ondersteund. |
| Garmin-opslag | Sluit de Forerunner op Windows aan en inventariseer alleen leesbaar welke opslag en mappen zichtbaar zijn. | Bepaal of een ondersteunde overdrachtsroute bestaat. |

## Voorlopige architectuur

Voorlopig levert de gebruiker een handmatig gemaakte volledige back-up met de gewenste selectie aan de Windows .NET-app. Automatische volledige back-ups staan nu aan op de telefoon, maar automatische overdracht naar OneDrive werkt nog niet betrouwbaar. Een volgende proef is de lokale back-upmap via wifi naar Windows te synchroniseren. Dezelfde wifi alleen geeft Windows geen toegang tot Android-bestanden; daar is een dienst of synchronisatie-app op de telefoon voor nodig. De Windows-app haalt waar mogelijk de geselecteerde audio op bij de oorspronkelijke podcastbron en houdt een lokale synchronisatiestatus bij. Pas nadat de Garmin-overdrachtsroute praktisch is bewezen, wordt USB-detectie en kopiëren toegevoegd. Luisterposities en tweerichtingssynchronisatie vallen buiten versie 1.

## Veiligheidsgrens

Het prototype leest alleen door de gebruiker aangeleverde back-ups. Het schrijft niet naar Android of Garmin. Een tijdelijk SQLite-bestand wordt in een privémap gemaakt en na het lezen verwijderd. De volledige back-up kan persoonlijke gegevens en aanmeldinformatie bevatten; voeg die niet toe aan Git.

