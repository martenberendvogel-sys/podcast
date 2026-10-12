# Een lang audioboek opdelen voor Garmin

De Forerunner onthoudt bij het wisselen van lokale MP3's de luisterpositie van dit boek niet betrouwbaar. Daarom is een stuk van 20 minuten handiger dan één bestand van 12 uur: bij terugkeer na een podcast zoek je maximaal binnen het huidige stuk. Dit is een losse handeling buiten de podcast-sync.

Gebruik op een persoonlijke Windows-pc met `ffmpeg` en `ffprobe` op het `PATH`:

```powershell
ffmpeg -version
ffprobe -version
./scripts/Split-Audiobook.ps1 -InputPath 'C:\pad\naar\Sword of Destiny (Unabridged).m4b' -Minutes 20
```

Het script accepteert `.m4b` en `.mp3` en maakt naast het origineel een nieuwe map met genummerde MP3's, bijvoorbeeld `Sword of Destiny (Unabridged) - 001.mp3`. Het past ook de MP3-titel en tracknummers aan, zodat de stukken op het horloge te herkennen zijn. Het originele bestand blijft intact. Een bestaande uitvoermap wordt geweigerd: niets wordt overschreven. Met `-OutputDirectory 'D:\GarminBoek'` kun je een andere nieuwe map kiezen.

Bij M4B wordt de audio omgezet naar MP3 op 96 kbit/s. Reken bij een boek van 12 uur op ongeveer 0,5 GB uitvoer en enige verwerkingstijd. MP3-invoer wordt zonder hercoderen opgesplitst. Beveiligde/DRM-vergrendelde M4B-bestanden kan het script niet verwerken.

FFmpeg moet vooraf zijn geïnstalleerd; als `ffmpeg -version` niet werkt, installeer het dan op je persoonlijke pc via een bron die je vertrouwt. Op de beheerde werk-pc kan dezelfde beveiligingsregel die onze .NET-app blokkeert ook FFmpeg blokkeren; omzeil die regel niet.

Probeer eerst twee of drie stukken op de Garmin via de overdrachtsroute die je al gebruikt. Controleer de volgorde en of afspelen automatisch naar het volgende stuk gaat. Bij 12 uur en stukken van 20 minuten worden het ongeveer 36 bestanden. Aan de grenzen van stukken kan een kleine tijdsafwijking ontstaan. Voeg het audioboek en de stukken niet toe aan Git.
