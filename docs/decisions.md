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

## Werkwijze voor vervolgchats

Lees eerst `README.md`, `docs/requirements.md`, `docs/feasibility.md` en dit bestand. Gebruik de bestaande checkout; maak geen apart worktree tenzij de gebruiker daar expliciet om vraagt. Werk één onzekerheid tegelijk af en leg nieuwe technische bevindingen hier vast.

