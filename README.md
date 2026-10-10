# Podcast Sync

Onderzoeksprototype voor een betrouwbare synchronisatieketen van Podcast Addict via Windows naar een Garmin Forerunner 265.

De eerste stap is bewust beperkt: een back-up inspecteren en de mogelijk bruikbare afleveringgegevens en luisterposities zichtbaar maken. Garmin-overdracht en het herstellen van afspeelposities zijn nog niet bewezen en worden pas na praktische hardwaretests ontworpen.

## Back-upinspecteur draaien

Deze repository gebruikt .NET 8. Installeer een SDK of gebruik in de cloudomgeving `/workspace/.dotnet/dotnet`.

```bash
dotnet run --project src/PodcastSync.BackupInspector -- inspect /pad/naar/podcast-addict-back-up
```

De inspecteur accepteert JSON, XML en ZIP. Hij schrijft een JSON-rapport met gevonden velden en kandidaat-afleveringen. De heuristiek is expres traceerbaar: een echte Podcast Addict-back-up is nodig om de velden definitief te koppelen.

## Huidige verificatie

```bash
dotnet run --project tests/PodcastSync.BackupInspector.Tests
```

Dit test JSON-, XML- en ZIP-invoer met luisterposities.

## Volgende praktische onderzoeken

1. Exporteer een echte Podcast Addict-back-up en voer die door de inspecteur.
2. Leg vast hoe audiobestanden en de back-up automatisch van Android naar Windows komen.
3. Test, zonder bestanden te schrijven, hoe de Garmin via USB door Windows wordt gepresenteerd.
4. Test één handmatige audio-overdracht voordat automatische Garmin-synchronisatie wordt gebouwd.

