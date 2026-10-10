# Technische haalbaarheid – eerste bevindingen

## Bewezen in dit prototype

- Een lokale, platformonafhankelijke inspecteur kan JSON, XML en ZIP met JSON/XML doorzoeken.
- Kandidaatvelden voor podcasttitel, afleveringstitel, audioreferentie en luisterpositie worden als controleerbaar JSON-rapport getoond.
- Het rapport bevat per invoerbestand de aangetroffen veldnamen, zodat het werkelijke back-upschema zonder giswerk kan worden vastgelegd.
- De echte `.backup` is een ZIP met `podcastAddict.db` (SQLite). `ordered_list` met `type=1` en `filter=-1` bevat 16 geordende afleveringen; de gebruiker bevestigde dat de app 16 playlistafleveringen toont.
- Alle 16 geselecteerde afleveringen hebben een HTTPS-audiolink, een podcastfeed en een unieke afgeleide sleutel. Ze zijn volgens de back-up niet lokaal gedownload.
- Een OPML-export bevat abonnementen, maar geen playlistafleveringen.

## Nog te bewijzen met echte hardware en data

| Vraag | Benodigde proef | Besluit na de proef |
| --- | --- | --- |
| Playlistwijzigingen | Exporteer na toevoegen of verwijderen van een aflevering een tweede back-up. | Controleer dat `compare` de juiste toevoeging en verwijdering meldt. |
| Automatische Android-overdracht | Vergelijk ondersteunde export/back-upmogelijkheden en een veilige LAN-overdracht naar Windows. | Kies een methode die geen toegang tot de privédata van de app vereist. |
| Audio voor streamingafleveringen | Neem één publiek beschikbare aflevering uit de testplaylist en bepaal of de pc de oorspronkelijke audiobron kan ophalen. | Kies directe download vanaf Windows of markeer de bron als niet ondersteund. |
| Garmin-opslag | Sluit de Forerunner op Windows aan en inventariseer alleen leesbaar welke opslag en mappen zichtbaar zijn. | Bepaal of een ondersteunde overdrachtsroute bestaat. |

## Voorlopige architectuur

Android levert een geëxporteerde back-up met de gewenste selectie via het thuisnetwerk aan een Windows .NET-app. Die app haalt waar mogelijk de geselecteerde audio op bij de oorspronkelijke podcastbron en houdt een lokale synchronisatiestatus bij. Pas nadat de Garmin-overdrachtsroute praktisch is bewezen, wordt USB-detectie en kopiëren toegevoegd. Luisterposities en tweerichtingssynchronisatie vallen buiten versie 1.

## Veiligheidsgrens

Het prototype leest alleen door de gebruiker aangeleverde back-ups. Het schrijft niet naar Android of Garmin. Een tijdelijk SQLite-bestand wordt in een privémap gemaakt en na het lezen verwijderd. De volledige back-up kan persoonlijke gegevens en aanmeldinformatie bevatten; voeg die niet toe aan Git.

