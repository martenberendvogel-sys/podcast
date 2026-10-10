# Technische haalbaarheid – eerste bevindingen

## Bewezen in dit prototype

- Een lokale, platformonafhankelijke inspecteur kan JSON, XML en ZIP met JSON/XML doorzoeken.
- Kandidaatvelden voor podcasttitel, afleveringstitel, audioreferentie en luisterpositie worden als controleerbaar JSON-rapport getoond.

## Nog te bewijzen met echte hardware en data

| Vraag | Benodigde proef | Besluit na de proef |
| --- | --- | --- |
| Podcast Addict-back-upformaat | Exporteer één back-up met een deels beluisterde, gedownloade aflevering en inspecteer die. | Leg het definitieve schema vast. |
| Automatische Android-overdracht | Vergelijk ondersteunde export/back-upmogelijkheden en een veilige LAN-overdracht naar Windows. | Kies een methode die geen toegang tot de privédata van de app vereist. |
| Garmin-opslag | Sluit de Forerunner op Windows aan en inventariseer alleen leesbaar welke opslag en mappen zichtbaar zijn. | Bepaal of een ondersteunde overdrachtsroute bestaat. |
| Afspeelpositie op Garmin | Zet één testaflevering via de bewezen route over en bepaal of de speler een startpositie biedt. | Kies volledige overdracht, audioknippen als omweg, of verwerp de functie voor v1. |

## Voorlopige architectuur

Android levert een geëxporteerde back-up plus geselecteerde audiobestanden via het thuisnetwerk aan een Windows .NET-app. Die app houdt een lokale synchronisatiestatus bij. Pas nadat de Garmin-overdrachtsroute praktisch is bewezen, wordt USB-detectie en kopiëren toegevoegd. Tweerichtingssynchronisatie valt buiten versie 1.

## Veiligheidsgrens

Het prototype leest alleen door de gebruiker aangeleverde back-ups. Het schrijft niet naar Android, Garmin of systeemlocaties.

