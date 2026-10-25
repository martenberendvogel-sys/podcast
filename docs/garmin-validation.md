# Garmin-validatie op Windows 11

De cloudomgeving kan de fysieke Forerunner 265 niet zien. Praktische proeven gebeuren op een Windows-pc met het horloge via USB aangesloten.

## Al bevestigd door de gebruiker

De gebruiker downloadt MP3's al handmatig en heeft die zowel via Garmin Express als via Windows Verkenner op de Garmin gezet. De handmatige basisroute hoeft dus niet opnieuw te worden bewezen. De Garmin verschijnt onder `Deze pc\Forerunner 265\Internal Storage\Podcasts`, niet als gewone schijfletter. Daar staan bestaande podcastsubmappen; die blijven onaangeroerd.

De eigen .NET-app kan op de beheerde pc niet worden gestart door een beveiligingsregel van de beheerder; test de app later op een persoonlijke pc waarop eigen ontwikkelcode is toegestaan.

## App-proef op de persoonlijke pc

1. Open de repository op Windows en installeer de .NET 8-SDK.
2. Gebruik een volledige Podcast Addict-back-up. Bewaar die privé buiten de repository.
3. Bereid eerst één aflevering voor in een nieuwe lokale map, bijvoorbeeld `C:\PodcastSync\staging`:

```powershell
dotnet restore tests/PodcastSync.BackupInspector.Tests --locked-mode
dotnet run --project src/PodcastSync.BackupInspector -- prepare 'C:\pad\naar\PodcastAddict.backup' 'C:\PodcastSync\staging' --limit 1
```

De map bevat één MP3 met een bestandsnaam op basis van de aflevering-ID en een `podcast-sync-manifest.json`. Dat manifest koppelt de ID aan de titel en de positie in de playlist. Bij opnieuw draaien wordt de MP3 op grootte en SHA-256 gecontroleerd en hergebruikt. Het manifest bevat geen audiolinks, maar wel podcasttitels; bewaar de map privé.

## Horloge alleen-lezen inventariseren

Sluit de Forerunner via USB aan. Draai in PowerShell vanuit de repository:

```powershell
./scripts/Inspect-Garmin.ps1
```

Het script inventariseert Garmin-apparaten in Windows, Verkenner en eventuele volumes. Het controleert ook of `Internal Storage\Podcasts\Recente podcasts` via de Windows-apparaatinterface bereikbaar is en telt daar bestanden. De eerste bestandsnamen, typen en groottes worden getoond voor diagnose; deel die desgewenst zonder privé-informatie. Het kopieert of verwijdert niets.

Met `./scripts/Inspect-Garmin.ps1 -StagingDirectory 'C:\video\AI\podcast-staging'` vergelijkt het script bovendien de geselecteerde playlist met lokaal voorbereide bestanden en de Garmin-map. `SyncPlan` toont hoeveel nog moeten worden gedownload of gekopieerd en welke eerder voorbereide sleutels niet meer geselecteerd zijn. Dit is uitsluitend een voorstel: het verwijdert niets, en een oude back-up mag niet als basis voor opruimen dienen.

Op de persoonlijke pc vond dit overzicht alle drie werkende MP3's terug: 14 geselecteerde afleveringen in de back-up van 24 oktober, 3 voorbereid en aanwezig op de Garmin, 11 nog niet gedownload. Er waren geen onbekende of verouderde bestanden in de eigen Garmin-map. Voor de volgende echte sync is een nieuwe back-up nodig.

De nieuwe volledige back-up van 25 oktober bevat 10 actuele afleveringen. Vier verdwenen uit de playlist, maar geen van die vier stond in `Recente podcasts`; alle drie bestaande Garmin-bestanden bleven gewenst. `prepare` heeft alle 10 actuele MP3's lokaal klaargezet. De overige zeven zijn daarna één voor één gekopieerd. Het laatste `SyncPlan` meldde 10 voorbereid en 10 aanwezig op de Garmin, met 0 ontbrekende kopieën, 0 verwijderkandidaten en 0 onbekende bestanden. Dit bevestigt de bestandsselectie, niet de bytevolledigheid van elk MTP-bestand.

De Garmin presenteert deze 10 bestanden ondanks de ene map als twee afzonderlijke podcastgroepen (6 `All in the Game`, 4 `America podcasts`). De gebruiker kan nog geen doorlopende reeks van alle 10 starten. Onderzoek een ondersteunde Garmin-afspeellijst of welke audiotags de groepering sturen voordat er bestanden op het horloge worden vervangen.

`ffprobe` op één bestand uit elke groep toont dezelfde artiest (`BNR Nieuwsradio`) en hetzelfde genre (`Podcast`), maar verschillende albums (`All in the Game` en `Amerika Podcast`). Dit maakt het albumveld een sterke hypothese voor de groepering; het is nog niet op de Garmin bewezen. `scripts/Make-GarminAlbumTest.ps1` maakt daarom twee aparte lokale kopieën met hetzelfde album `Podcast Sync Test` en tracknummers 1 en 2, zonder audio opnieuw te coderen of de stagingbestanden te wijzigen. Proefopdracht:

```powershell
./scripts/Make-GarminAlbumTest.ps1 -StagingDirectory 'C:\video\AI\podcast-staging' -OutputDirectory 'C:\video\AI\Podcast Sync Test'
```

Kopieer uitsluitend die nieuwe testmap via Verkenner naar `Forerunner 265\Internal Storage\Podcasts`, dus niet naar de beheerde map `Recente podcasts`. Kijk of de twee afleveringen op de Garmin als één groep verschijnen en of aflevering 2 automatisch start na 1. Laat de tien bestaande bestanden met rust. De testmap kan later apart worden opgeruimd nadat de uitkomst vaststaat.

De gebruiker ziet beide testafleveringen uit verschillende podcasts onder één Garmin-groep `Podcast Sync Test`, en de knop 'volgende' gaat van aflevering 1 naar 2. Dit bevestigt dat de gezamenlijke albumnaam voor deze twee podcasts een doorlopende groep oplevert; automatisch doorspelen na het einde is nog niet apart getest. `scripts/Prepare-GarminAlbum.ps1` kan op de pc alle geselecteerde MP3's als aparte kopieën met album `Recente podcasts` en playlistvolgnummer maken. Het valideert eerst de lokale bronbestanden, bewaart hun titel en schrijft niets naar de Garmin. De uitvoermap bevat een privémanifest met titels en checksums. Maak die lokale set vóór vervanging op het horloge; ontwerp daarna een veilige migratie van de huidige tien bestanden.

De eerste volledige tien-bestandenproef staat inmiddels op de Garmin. Eén gedeeld album werkt, maar de volgorde is verkeerd: het horloge toont 10 bovenaan en 1 onderaan; na 1 gaat 'volgende' naar 10, daarna 9. De oude lokale uitvoermap `Garmin klaar 20261025` heeft de verkeerde tracktags en mag niet opnieuw als definitieve set worden gebruikt. Een nieuwe, geïsoleerde proef met drie tracks test omgekeerde ID3-tracknummers (3, 2, 1) bij gewenste afspeelvolgorde (1, 2, 3). Het testscript schrijft alleen naar een nieuwe pc-map:

```powershell
./scripts/Make-GarminAlbumTest.ps1 -StagingDirectory 'C:\video\AI\podcast-staging' -OutputDirectory 'C:\video\AI\Podcast Sync Volgorde Test' -ReverseTrack
```

Kopieer daarna uitsluitend die drie test-MP3's in een aparte testmap onder `Forerunner 265\Internal Storage\Podcasts`, naast `Recente podcasts`. Controleer of de Garmin de gewenste eerste, tweede en derde **titel** achtereenvolgens afspeelt, inclusief de grens tussen de twee bronpodcasts. De getoonde tracknummers mogen daarvoor van 3 naar 1 aftellen. Pas na bevestiging kan `Prepare-GarminAlbum.ps1` de tien huidige afleveringen opnieuw in een **nieuwe** uitvoermap maken; verwijder of vervang de huidige tien niet op basis van alleen deze hypothese.

De proef bevestigde dat Garmin afspeelt van hoog naar laag tracknummer: track 3 bevatte de gewenste eerste titel, gevolgd door 2 en 1. De tien nieuwe, omgekeerd getagde kopieën staan inmiddels in `Podcasts\Recente podcasts`: track 10 bevat de gewenste eerste titel en 'volgende' brengt track 9 met de gewenste tweede titel. Het cijfer op het horloge is dus niet de Podcast Addict-playlistpositie.

Voor de volgende automatiseringsstap moet eerst worden vastgesteld of Windows de ID3-tracktags van de MTP-bestanden kan uitlezen. Bij een nieuwe playlist verandert immers mogelijk de tag van een bestaand bestand zonder dat zijn hashachtige bestandsnaam verandert. De nieuwe, alleen-lezen proef gebruikt het privémanifest van de huidige lokale Garmin-klare set:

```powershell
./scripts/Inspect-GarminOrder.ps1 -GarminReadyDirectory 'C:\video\AI\Garmin klaar omgekeerd'
```

Het script vergelijkt geselecteerde sleutels met de bestanden in `Podcasts\Recente podcasts` en toont voor elk bestand het verwachte en, indien beschikbaar, door Windows gerapporteerde tracknummer en album. Geen schrijfacties. MTP-naam, metadata en de door Windows gerapporteerde grootte bewijzen niet dat de overdracht bytevolledig is.

Op de persoonlijke pc vond deze proef de tien geselecteerde bestanden, zonder ontbrekende of onverwachte namen; voor alle tien kwamen het MTP-tracknummer en album overeen met het lokale manifest. De volgende versie van hetzelfde script valideert ook lokale controlesommen en rapporteert een expliciet alleen-lezen plan: `KeepCount`, `MissingKeys`, `NeedsRetagOrReplaceKeys`, `UnverifiedMetadataKeys` en `RemovalCandidateKeys`. Een verwijderkandidaat wordt nooit automatisch verwijderd op basis van deze proef.

Ook de uitgebreide proef is geslaagd: 10 behouden, alle actielijsten leeg. De gebruiker beschouwt `Recente podcasts` momenteel als de project-testmap; deze is pas productie nadat de hele keten af is. `Test-GarminMtpRoundtrip.ps1` test daarom een kopie en later een verwijdering van precies één herkenbaar, niet-hashvormig MP3-bestand **in deze map**. De tien bestaande hashvormige playlistbestanden kunnen door dit script niet worden geselecteerd. Standaard toont het script alleen het plan; `-Action Copy` en `-Action Delete` zijn aparte acties. De bron is één van de drie lokale bestanden in `C:\video\AI\Podcast Sync Volgorde Test`.

De gebruiker heeft de rondreisproef gedaan: het testbestand verscheen na `-Action Copy` en verdween na `-Action Delete`; bij verwijderen liet Windows een bevestigingsvenster zien. De huidige Windows Shell-verb `delete` kan daarom niet zonder gebruiker als onbemande sync-stap gelden. `CopyHere` geeft evenmin een betrouwbaar eindesignaal. Onderzoek directe WPD/MTP-toegang of ontwerp een expliciet bevestigde semi-automatische sync; gebruik geen UI-automatisering om de Windows-dialoog stilzwijgend weg te klikken.

`src/PodcastSync.MtpProbe` test directe WPD/MTP-toegang via het .NET-pakket `MediaDevices` 2.0.0 (Windows-only). De eerste opdracht `dotnet run --project .\src\PodcastSync.MtpProbe -- probe` is alleen-lezen. Hij inventariseert de exacte doelmap en WPD-bestandsgroottes, zonder Garmin-writes.

De proef op de Forerunner is geslaagd: de directe WPD-route zag alle tien MP3's met echte bestandsgroottes. De nieuwe acties `copy-test <lokale-test.mp3>` en `delete-test <lokale-test.mp3>` blijven strikt binnen `Podcasts\Recente podcasts` en accepteren uitsluitend de herkenbare MP3 uit de lokale map `Podcast Sync Volgorde Test`; de tien hashbestanden zijn uitgesloten. `copy-test` gebruikt WPD-upload, leest het bestand terug en vergelijkt grootte plus SHA-256. `delete-test` verifieert diezelfde inhoud voordat het één exact bestand verwijdert. Deze schrijfacties zijn nog niet op het horloge getest. Als de upload of readback mislukt, verwijder dan niet blindelings: inspecteer eerst de Garmin-map.

## Volgende overdrachtsproef

De eerste door `prepare` gedownloade MP3 speelt op de pc én in de nieuwe Garmin-map `Podcasts\Recente podcasts` af. De Garmin toont de juiste MP3-titel, ondanks de hashachtige bestandsnaam. De map is gemaakt door een lege pc-map via Verkenner naar de Garmin te kopiëren; direct een map aanmaken op het apparaat lukte niet.

De map `Recente podcasts` wordt de enige door de app beheerde Garmin-map; bestaande podcastsubmappen zijn buiten scope. Verkenner toont het apparaat als draagbaar apparaat/MTP-kandidaat, dus een normaal `.NET File.Copy` naar een schijfletter is waarschijnlijk niet mogelijk. Laat de app pas naar Garmin-opslag schrijven als het mappad, kopieergedrag en herstel bij onderbreking zijn vastgesteld. Verwijder voorlopig niets van het horloge.

## Voorzichtige automatische kopieerproef

De Garmin meldt in Windows zijn ene bestaande MP3 zonder `.mp3` in de naam en met `Size=0`, hoewel hij afspeelt. Groottecontrole op het apparaat is daardoor onbetrouwbaar. `scripts/Copy-OneToGarmin.ps1` controleert de lokale SHA-256 en kiest maximaal één voorbereide MP3 die nog niet onder zijn sleutel in `Recente podcasts` staat. Standaard toont het script alleen het plan; met `-Apply` vraagt het Windows een kopie via de draagbare-apparaatinterface te starten. Het maakt geen mappen aan, overschrijft niets en verwijdert niets. De kopieopdracht kan asynchroon zijn: wacht op Windows en controleer vervolgens op het horloge of de aflevering echt afspeelt.

Bereid voor deze proef op de persoonlijke pc eerst een tweede aflevering voor met dezelfde back-up en stagingmap, nu met `--limit 2`. De eerste MP3 wordt op SHA-256 gecontroleerd en hergebruikt. Draai daarna het kopieerscript zonder `-Apply` om te zien welke nieuwe MP3 hij zou kopiëren. Gebruik `-Apply` pas na controle van dat plan.

De proef is op de persoonlijke pc geslaagd: de tweede aflevering werd door `Copy-OneToGarmin.ps1 -Apply` naar de bestaande map gestuurd, beide bestanden zijn zichtbaar en spelen op het horloge. Nog niet bewezen: of `CopyHere` op voltooiing wacht, betrouwbaar de volledige playlist kan overzetten en een onderbroken overdracht kan herkennen.

Een derde aflevering werd eveneens via `-Apply` gekopieerd en speelt af. De PowerShell-prompt kwam vrijwel direct terug; `CopyHere` mag daarom niet als voltooiingssignaal voor een batch worden gebruikt. Op het horloge waren daarna drie bestanden zichtbaar. De volgende stap is een veilig, incrementeel plan maken en een betrouwbare voltooiingscontrole ontwerpen voordat meerdere kopieën of verwijderingen automatisch gebeuren.
