# Product requirements – Podcast Addict naar Garmin

## Doel

De gebruiker luistert op Android met Podcast Addict en moet tijdens een hardloopronde zonder telefoon op een Garmin Forerunner 265 kunnen verderluisteren. De overdracht naar het horloge verloopt via een Windows 11-pc en USB, met zo weinig mogelijk handmatige handelingen.

## Afbakening versie 1

- Richting: Android naar Garmin.
- Centrale toepassing: een Windows 11-programma in C#/.NET.
- Invoer: een export/back-up van Podcast Addict en geselecteerde gedownloade audiobestanden.
- Overdracht telefoon naar pc: via het thuisnetwerk; de methode wordt pas gekozen na een praktische proef.
- Garmin: detecteren bij USB-aansluiting, vervolgens alleen synchroniseren als een ondersteunde overdrachtsroute is bewezen.
- Voorkom dubbele overdrachten en houd lokaal bij wat is overgezet.
- Toon uiteindelijk een eenvoudige synchronisatiestatus in een GUI.

## Gewenst gedrag

1. Podcast Addict bevat downloads, luistergeschiedenis en eventuele luisterposities.
2. Back-upgegevens en relevante audio worden automatisch beschikbaar op de Windows-pc.
3. De pc selecteert de afleveringen en bereidt ze voor.
4. Bij het aansluiten van de Garmin wordt veilig gesynchroniseerd.
5. De gebruiker koppelt de Garmin los en luistert offline.

## Niet aannemen zonder proef

- Dat Podcast Addict een bruikbare, automatisch te maken back-up levert met aflevering- en positiegegevens.
- Dat Android toegang tot Podcast Addict-bestanden toestaat of een geschikte export biedt.
- Dat de Forerunner 265 rechtstreeks gekopieerde audiobestanden als podcasts/muziek herkent.
- Dat Garmin Express omzeild kan worden.
- Dat een afspeelpositie op de Garmin ingesteld of uitgelezen kan worden.

## Fasen en acceptatiecriteria

### 1. Back-upanalyse

Een echte Podcast Addict-back-up wordt door de inspecteur gelezen; minstens podcast, aflevering en luisterpositie worden aan concrete bronvelden gekoppeld.

### 2. Android naar Windows

Een gekozen, herhaalbare methode levert back-up en een testaudio zonder handmatig kopiëren aan op de pc.

### 3. Garmin-USB-onderzoek

De Windows-tool herkent de aangesloten Garmin en inventariseert alleen-lezen de toegankelijke opslag. Eén testbestand wordt uitsluitend via een bewezen veilige route overgedragen en daarna op het horloge gecontroleerd.

### 4. Automatische eenrichtingssynchronisatie

Bij aansluiten synchroniseert de tool alleen ontbrekende of gewijzigde afleveringen, laat voltooiing zien en maakt geen dubbele of incomplete overdrachten.

## Buiten versie 1

- Tweerichtingssynchronisatie van luisterposities.
- Verwijderen van volledig beluisterde afleveringen.
- Opslaglimieten en slimme selectie voor hardlopen.

