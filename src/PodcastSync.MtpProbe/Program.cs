using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using MediaDevices;

var command = args.Length == 0 ? "probe" : args[0];
if (!((args.Length == 1 && command == "self-test") ||
      (args.Length <= 1 && command == "probe") ||
      (args.Length == 2 && (command == "copy-test" || command == "delete-test")) ||
      (args.Length == 3 && (command == "sync-plan" || command == "sync-apply"))))
{
    Console.Error.WriteLine("Gebruik: ... -- probe | self-test | copy-test <test.mp3> | delete-test <test.mp3> | sync-plan <back-up> <Garmin-klare-map> | sync-apply <back-up> <Garmin-klare-map>");
    return 2;
}

if (command == "self-test") return GarminSyncSelfTest.Run();

if (!OperatingSystem.IsWindows())
{
    Console.Error.WriteLine("Deze WPD/MTP-proef draait alleen op Windows.");
    return 2;
}

try
{
    var devices = MediaDeviceManager.Instance.GetDevices()?
        .Where(device => string.Equals(device.FriendlyName, "Forerunner 265", StringComparison.Ordinal))
        .ToArray() ?? [];
    if (devices.Length != 1)
        throw new InvalidOperationException("Precies één aangesloten Forerunner 265 is vereist.");

    using var garmin = devices[0];
    garmin.Connect(enableCache: false);
    try
    {
        var rootFolders = garmin.GetDirectories(@"\");
        var storage = FindFolder(rootFolders, "Internal Storage");
        var podcasts = storage is null ? null : FindFolder(garmin.GetDirectories(storage), "Podcasts");
        var recent = podcasts is null ? null : FindFolder(garmin.GetDirectories(podcasts), "Recente podcasts");
        if (command == "probe")
        {
            DeviceFile[] files = recent is null ? [] : garmin.GetFiles(recent)
                .Select(path => ReadDeviceFile(garmin, path))
                .OrderBy(file => file.Name, StringComparer.Ordinal)
                .ToArray();
            Print(new
            {
                Device = garmin.FriendlyName,
                RootFolders = rootFolders.Select(Path.GetFileName).ToArray(),
                StorageFound = storage is not null,
                PodcastsFound = podcasts is not null,
                RecentPodcastsFound = recent is not null,
                RecentPodcastsFileCount = files.Length,
                Files = files,
                Warning = "Alleen lezen. Geen bestand naar de Garmin gekopieerd of verwijderd."
            });
        }
        else if (command is "sync-plan" or "sync-apply")
        {
            if (recent is null) throw new InvalidOperationException("De exacte Garmin-testmap ontbreekt.");
            if (command == "sync-apply") GarminSync.RequireFreshBackup(args[1]);
            var manifest = await GarminSync.ValidateReadyAsync(args[1], args[2]);
            var plan = GarminSync.Inspect(garmin, recent, manifest);
            if (command == "sync-plan")
                Print(new { Action = "sync-plan", SelectedCount = manifest.SelectedKeys.Length, Plan = plan,
                    Warning = "Alleen lezen. sync-apply vereist een back-up van maximaal 48 uur oud en voert pas na volledige lokale en Garmin-controle wijzigingen uit." });
            else
            {
                if (plan.Unknown.Length != 0)
                    throw new InvalidOperationException("Onbekende bestanden in de Garmin-testmap; geen wijzigingen uitgevoerd.");
                Print(new { Action = "sync-apply", Before = plan });
                var after = GarminSync.Apply(garmin, recent, manifest, args[2], plan);
                Print(new { Action = "sync-apply", After = after });
            }
        }
        else
        {
            if (recent is null) throw new InvalidOperationException("De exacte Garmin-testmap ontbreekt.");
            var source = ResolveTestSource(args[1]);
            var destination = Path.Combine(recent, Path.GetFileName(source));

            if (command == "copy-test")
            {
                if (garmin.FileExists(destination))
                    throw new IOException("Het testbestand staat al op de Garmin; er wordt niets overschreven.");
                garmin.UploadFile(source, destination);
                var remoteBytes = VerifyRemote(garmin, destination, source);
                Print(new { Action = "copy-test", File = Path.GetFileName(source), Bytes = remoteBytes,
                    ReadBackSha256Matches = true, Note = "Directe WPD-upload is teruggelezen en gecontroleerd." });
            }
            else
            {
                if (!garmin.FileExists(destination))
                    throw new FileNotFoundException("Het exacte testbestand staat niet op de Garmin.");
                VerifyRemote(garmin, destination, source);
                garmin.DeleteFile(destination);
                if (garmin.FileExists(destination))
                    throw new IOException("Het testbestand is na de verwijderopdracht nog aanwezig.");
                Print(new { Action = "delete-test", File = Path.GetFileName(source),
                    Removed = true, Note = "Alleen het vooraf teruggelezen wegwerpbestand is verwijderd." });
            }
        }
    }
    finally
    {
        garmin.Disconnect();
    }

    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"WPD-proef mislukt: {exception.Message}");
    return 1;
}

static string? FindFolder(IEnumerable<string> paths, string name)
{
    var matches = paths.Where(path => string.Equals(Path.GetFileName(path.TrimEnd('\\')), name,
        StringComparison.Ordinal)).ToArray();
    return matches.Length == 1 ? matches[0] : null;
}

static DeviceFile ReadDeviceFile(MediaDevice device, string path)
{
    try { return new DeviceFile(Path.GetFileName(path), device.GetFileInfo(path).Length, null); }
    catch (Exception exception) { return new DeviceFile(Path.GetFileName(path), null, exception.Message); }
}

static string ResolveTestSource(string path)
{
    var source = Path.GetFullPath(path);
    if (!File.Exists(source) ||
        !(string.Equals(Path.GetFileName(Path.GetDirectoryName(source)), "Podcast Sync Volgorde Test", StringComparison.Ordinal) ||
          string.Equals(Path.GetFileName(Path.GetDirectoryName(source)), "Podcast Sync Artiest Test", StringComparison.Ordinal)) ||
        !Regex.IsMatch(Path.GetFileName(source), @"^0[1-3] - [0-9a-f]{8}\.mp3$", RegexOptions.CultureInvariant))
        throw new ArgumentException("Gebruik een bestaand MP3-bestand uit de lokale map Podcast Sync Volgorde Test of Podcast Sync Artiest Test.");
    if (new FileInfo(source).Length < 1024)
        throw new InvalidDataException("Het lokale testbestand is onverwacht klein.");
    return source;
}

static ulong VerifyRemote(MediaDevice device, string destination, string source)
{
    if (!device.FileExists(destination))
        throw new IOException("De WPD-overdracht is nog niet zichtbaar op het apparaat.");
    var remote = device.GetFileInfo(destination);
    var localBytes = checked((ulong)new FileInfo(source).Length);
    if (remote.Length != localBytes)
        throw new IOException($"WPD-grootte wijkt af: lokaal {localBytes}, Garmin {remote.Length}. Het bestand blijft staan voor onderzoek.");
    using var localStream = File.OpenRead(source);
    using var remoteStream = remote.OpenRead();
    var localHash = SHA256.HashData(localStream);
    var remoteHash = SHA256.HashData(remoteStream);
    if (!CryptographicOperations.FixedTimeEquals(localHash, remoteHash))
        throw new IOException("SHA-256 van de Garmin-kopie wijkt af. Het bestand blijft staan voor onderzoek.");
    return remote.Length;
}

static void Print<T>(T value) => Console.WriteLine(JsonSerializer.Serialize(value,
    new JsonSerializerOptions { WriteIndented = true }));

internal sealed record DeviceFile(string Name, ulong? Bytes, string? MetadataError);
