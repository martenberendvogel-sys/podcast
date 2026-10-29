# Podcast Sync – draagbare Windows-proef

Dit pakket bevat het venster en de twee benodigde .NET-programma's. Het is bedoeld voor Windows 10/11 x64 en heeft **geen .NET SDK of broncodecheckout** nodig. FFmpeg moet nog apart geïnstalleerd zijn en via `ffmpeg -version` bereikbaar zijn. De telefoon en Garmin moeten via USB toegankelijk zijn.

1. Download het artifact `podcast-sync-portable-win-x64` van de geslaagde GitHub Actions-run **Windows build**. Controleer dat het uit deze repository komt. Als Windows het gedownloade ZIP-bestand blokkeert, kies bij **Eigenschappen → Blokkering opheffen** voordat je het uitpakt. Pak daarna het volledige pakket uit in een eigen map. Verplaats niet alleen de `.exe`: de mappen `app`, `bin` en `scripts` horen bij elkaar.
2. Maak in Podcast Addict een verse volledige back-up. Ontgrendel de telefoon en schakel USB-bestandsoverdracht in; sluit ook de Forerunner aan.
3. Start `app\PodcastSync.Desktop.exe`. Vul de exacte telefoonnaam uit Verkenner en een bestaande lokale back-upmap in. Deze twee instellingen worden alleen op deze pc onder `%LOCALAPPDATA%\PodcastSync\desktop-settings.json` onthouden.
4. Klik eerst op **Plan controleren** en lees toevoegen/vervangen/verwijderen. Klik pas daarna op **Synchroniseer**. Laat de apparaten aangesloten totdat de eindcontrole klaar is.

Back-ups, MP3-cache en Garmin-klare bestanden blijven in de door jou gekozen lokale back-upmap; ze zitten niet in dit pakket. Het programma wijzigt alleen `Internal Storage\Podcasts\Recente podcasts` op de Garmin. De back-up in Podcast Addict maken en de telefoon ontgrendelen blijven handmatig. Bij een mislukte overdracht: bekijk het log, maak opnieuw een plan en probeer pas daarna opnieuw.

Met **Opruimen…** verschijnt eerst een overzicht van oude lokale `Garmin klaar Podcast Sync <datumtijd>`-mappen. De nieuwste set blijft staan. Alleen complete sets met een herkend manifest, bijbehorende back-up en ongewijzigde MP3's worden voorgesteld. Extra bestanden, submappen en koppelingen worden overgeslagen. Na je bevestiging gaan de voorgestelde mappen naar de Windows-Prullenbak, waar je ze kunt terugzetten. Back-ups, `podcast-staging` en losse bestanden blijven staan. Tijdens synchronisatie is opruimen geblokkeerd. Leeg de Prullenbak pas als je zeker weet dat je de oude sets niet meer nodig hebt; tot dan nemen ze nog schijfruimte in.

Dit is een **proefpakket**. De broncode en projectgeschiedenis staan in de podcast-repository; de bewezen `v0.1.0`-versie blijft apart beschikbaar. Synchronisatie en leesbare voortgang zijn op de persoonlijke Windows-pc getest. De nieuwe opruimfunctie moet nog op de persoonlijke pc worden bevestigd.
