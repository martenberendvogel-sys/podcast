using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using MediaDevices;
using PodcastSync.BackupInspector;

internal sealed record ReadyFile(int Rank, int TrackNumber, string Key, string Podcast, string Title, long Bytes, string Sha256);
internal sealed record ReadyManifest(int Version, string Album, string OrderMode, string[] SelectedKeys, ReadyFile[] Files);
internal sealed record DeviceState(string Name, ulong? Bytes, string? Sha256);
internal sealed record SyncPlan(string[] Keep, string[] Add, string[] Replace, string[] Remove, string[] Unknown);

internal static class GarminSync
{
    private static readonly Regex ManagedFileName = new("^[0-9a-f]{64}\\.mp3$", RegexOptions.CultureInvariant);
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };

    internal static void RequireFreshBackup(string backupPath)
    {
        var match = Regex.Match(Path.GetFileName(backupPath), @"^PodcastAddict_(\d{8})_(\d{6})\.backup$",
            RegexOptions.CultureInvariant);
        if (!match.Success || !DateTime.TryParseExact(match.Groups[1].Value + match.Groups[2].Value,
            "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp) ||
            timestamp < DateTime.Now.AddHours(-48) || timestamp > DateTime.Now.AddHours(1))
            throw new InvalidOperationException("Maak eerst een nieuwe volledige Podcast Addict-back-up (maximaal 48 uur oud); geen Garmin-wijzigingen uitgevoerd.");
    }

    internal static async Task<ReadyManifest> ValidateReadyAsync(string backupPath, string readyDirectory)
    {
        var backup = Path.GetFullPath(backupPath);
        var directory = Path.GetFullPath(readyDirectory);
        if (!Directory.Exists(directory)) throw new DirectoryNotFoundException("De Garmin-klare map ontbreekt.");
        var manifestPath = Path.Combine(directory, "podcast-sync-garmin-ready.json");
        var manifest = JsonSerializer.Deserialize<ReadyManifest>(await File.ReadAllTextAsync(manifestPath), JsonOptions)
            ?? throw new InvalidDataException("Het Garmin-klare manifest is leeg.");
        if (manifest.Version != 1 || manifest.Album != "Recente podcasts" ||
            manifest.OrderMode != "GarminPodcastDescendingTrack" || manifest.SelectedKeys is null || manifest.Files is null ||
            manifest.SelectedKeys.Length == 0 || manifest.Files.Length != manifest.SelectedKeys.Length)
            throw new InvalidDataException("Onbekende of onvolledige Garmin-klare set.");
        if (manifest.SelectedKeys.Distinct(StringComparer.Ordinal).Count() != manifest.SelectedKeys.Length)
            throw new InvalidDataException("De Garmin-klare set bevat dubbele geselecteerde sleutels.");

        var playlist = await PodcastAddictPlaylist.ReadAsync(backup);
        var backupKeys = playlist.Episodes.Select(episode => episode.Key).ToArray();
        if (!backupKeys.SequenceEqual(manifest.SelectedKeys, StringComparer.Ordinal))
            throw new InvalidDataException("De Garmin-klare set komt niet overeen met de gekozen Podcast Addict-back-up.");

        var byKey = manifest.Files.ToDictionary(file => file.Key, StringComparer.Ordinal);
        for (var index = 0; index < manifest.SelectedKeys.Length; index++)
        {
            var key = manifest.SelectedKeys[index];
            if (!Regex.IsMatch(key, "^[0-9a-f]{64}$", RegexOptions.CultureInvariant) ||
                !byKey.TryGetValue(key, out var file) || file.Rank != index + 1 ||
                file.TrackNumber != manifest.SelectedKeys.Length - index || file.Bytes < 1024 ||
                !Regex.IsMatch(file.Sha256, "^[0-9a-f]{64}$", RegexOptions.CultureInvariant))
                throw new InvalidDataException("Ongeldige sleutel, volgorde of controlesom in de Garmin-klare set.");
            var path = Path.Combine(directory, key + ".mp3");
            if (!File.Exists(path) || new FileInfo(path).Length != file.Bytes ||
                !string.Equals(await HashFileAsync(path), file.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Garmin-klare MP3 ontbreekt of wijkt af: {key}");
        }
        return manifest;
    }

    internal static SyncPlan Inspect(MediaDevice device, string recent, ReadyManifest manifest)
    {
        var selected = manifest.SelectedKeys.ToHashSet(StringComparer.Ordinal);
        if (device.GetDirectories(recent).Any())
            throw new InvalidOperationException("De Garmin-testmap bevat submappen; automatische sync is geblokkeerd.");
        var states = new List<DeviceState>();
        foreach (var path in device.GetFiles(recent))
        {
            var name = Path.GetFileName(path);
            if (!ManagedFileName.IsMatch(name)) { states.Add(new DeviceState(name, null, null)); continue; }
            var key = name[..^4];
            if (!selected.Contains(key)) { states.Add(new DeviceState(name, null, null)); continue; }
            var remote = device.GetFileInfo(path);
            using var stream = remote.OpenRead();
            states.Add(new DeviceState(name, remote.Length, Convert.ToHexString(SHA256.HashData(stream))));
        }
        return Compare(manifest, states);
    }

    internal static SyncPlan Compare(ReadyManifest manifest, IReadOnlyList<DeviceState> states)
    {
        var selected = manifest.SelectedKeys.ToHashSet(StringComparer.Ordinal);
        var ready = manifest.Files.ToDictionary(file => file.Key, StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var keep = new HashSet<string>(StringComparer.Ordinal);
        var replace = new HashSet<string>(StringComparer.Ordinal);
        var remove = new List<string>();
        var unknown = new List<string>();
        foreach (var state in states)
        {
            if (!ManagedFileName.IsMatch(state.Name)) { unknown.Add(state.Name); continue; }
            var key = state.Name[..^4];
            if (!seen.Add(key)) throw new InvalidOperationException("Dubbele MP3-sleutel op de Garmin.");
            if (!selected.Contains(key)) { remove.Add(key); continue; }
            var expected = ready[key];
            if (state.Bytes == checked((ulong)expected.Bytes) &&
                string.Equals(state.Sha256, expected.Sha256, StringComparison.OrdinalIgnoreCase)) keep.Add(key);
            else replace.Add(key);
        }
        return new SyncPlan(manifest.SelectedKeys.Where(keep.Contains).ToArray(),
            manifest.SelectedKeys.Where(key => !seen.Contains(key)).ToArray(),
            manifest.SelectedKeys.Where(replace.Contains).ToArray(),
            remove.Order(StringComparer.Ordinal).ToArray(), unknown.Order(StringComparer.Ordinal).ToArray());
    }

    internal static SyncPlan Apply(MediaDevice device, string recent, ReadyManifest manifest, string readyDirectory, SyncPlan plan)
    {
        if (plan.Unknown.Length != 0) throw new InvalidOperationException("Onbekende bestanden in de Garmin-testmap; niets gewijzigd.");
        if (plan.Add.Length == 0 && plan.Replace.Length == 0 && plan.Remove.Length == 0) return plan;
        var directory = Path.GetFullPath(readyDirectory);
        foreach (var key in plan.Add)
            UploadAndVerify(device, recent, directory, key);
        foreach (var key in plan.Replace)
        {
            var path = Path.Combine(recent, key + ".mp3");
            device.DeleteFile(path);
            if (device.FileExists(path)) throw new IOException("Vervangen gestopt: oud bestand staat nog op de Garmin.");
            UploadAndVerify(device, recent, directory, key);
        }
        // Only remove no-longer-selected files after every desired file has been verified.
        var beforeRemoval = Inspect(device, recent, manifest);
        if (beforeRemoval.Unknown.Length != 0 || beforeRemoval.Add.Length != 0 || beforeRemoval.Replace.Length != 0)
            throw new IOException("Niet alle gewenste bestanden zijn bevestigd; verwijderen van oude afleveringen overgeslagen.");
        foreach (var key in beforeRemoval.Remove)
        {
            var path = Path.Combine(recent, key + ".mp3");
            device.DeleteFile(path);
            if (device.FileExists(path)) throw new IOException("Opruimen gestopt: bestand staat nog op de Garmin.");
        }
        return Inspect(device, recent, manifest);
    }

    private static void UploadAndVerify(MediaDevice device, string recent, string directory, string key)
    {
        var name = key + ".mp3";
        var source = Path.Combine(directory, name);
        var destination = Path.Combine(recent, name);
        if (device.FileExists(destination)) throw new IOException("Kopie gestopt: doelbestand bestaat al.");
        device.UploadFile(source, destination);
        using var sourceStream = File.OpenRead(source);
        var expectedHash = SHA256.HashData(sourceStream);
        var remote = device.GetFileInfo(destination);
        if (remote.Length != checked((ulong)new FileInfo(source).Length))
            throw new IOException("Kopie teruggelezen met verkeerde grootte; geen verdere wijzigingen.");
        using var remoteStream = remote.OpenRead();
        if (!CryptographicOperations.FixedTimeEquals(expectedHash, SHA256.HashData(remoteStream)))
            throw new IOException("Kopie teruggelezen met verkeerde SHA-256; geen verdere wijzigingen.");
    }

    private static async Task<string> HashFileAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        return Convert.ToHexString(await SHA256.HashDataAsync(stream));
    }
}
