using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PodcastSync.BackupInspector;

public sealed record StagedAudio(string Key, string Sha256, long Bytes);

public sealed record SelectedEpisode(int Rank, string Key, string Podcast, string Title);

public sealed record StagingManifest(int Version, IReadOnlyList<string> SelectedKeys, IReadOnlyList<StagedAudio> Files)
{
    public IReadOnlyList<SelectedEpisode> SelectedEpisodes { get; init; } = [];
}

public sealed record StagingResult(
    string Directory,
    int Selected,
    int Downloaded,
    int Reused,
    int Pending,
    int PreviouslyStagedButNoLongerSelected,
    int RecoveredPartialDownloads);

public static class PlaylistStager
{
    private const string ManifestName = "podcast-sync-manifest.json";
    private const string LockName = ".podcast-sync-staging.lock";
    private const long MaximumAudioBytes = 512L * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly Regex KeyPattern = new("^[0-9a-f]{64}$", RegexOptions.CultureInvariant);

    public static async Task<StagingResult> PrepareAsync(
        PlaylistReport playlist,
        string outputDirectory,
        int? limit,
        HttpClient client,
        CancellationToken cancellationToken = default)
    {
        if (limit is < 1) throw new ArgumentOutOfRangeException(nameof(limit), "De limiet moet positief zijn.");
        if (string.IsNullOrWhiteSpace(outputDirectory)) throw new ArgumentException("Geef een uitvoermap op.", nameof(outputDirectory));

        var directory = Path.GetFullPath(outputDirectory);
        var drive = new DriveInfo(Path.GetPathRoot(directory)!);
        if (drive.DriveType is DriveType.Removable or DriveType.CDRom or DriveType.Network)
            throw new InvalidOperationException("Kies een lokale map op de pc; voorbereiding op een USB-apparaat of netwerkschijf is niet toegestaan.");
        Directory.CreateDirectory(directory);
        var lockPath = Path.Combine(directory, LockName);
        FileStream lockStream;
        try
        {
            lockStream = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException exception)
        {
            throw new InvalidOperationException("Deze stagingmap wordt al door een andere voorbereiding gebruikt; start geen tweede sync.", exception);
        }
        await using var stagingLock = lockStream;
        var manifestPath = Path.Combine(directory, ManifestName);
        var manifestTemporaryPath = manifestPath + ".partial";
        if (!File.Exists(manifestPath))
        {
            var pathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            var hasUnmanagedEntries = Directory.EnumerateFileSystemEntries(directory).Any(path =>
                !string.Equals(path, lockPath, pathComparison) &&
                !string.Equals(path, manifestTemporaryPath, pathComparison));
            if (hasUnmanagedEntries)
                throw new InvalidOperationException("De uitvoermap bevat al bestanden zonder Podcast Sync-manifest. Kies een lege map.");
            if (File.Exists(manifestTemporaryPath)) File.Delete(manifestTemporaryPath);
        }

        var manifest = File.Exists(manifestPath)
            ? JsonSerializer.Deserialize<StagingManifest>(await File.ReadAllTextAsync(manifestPath, cancellationToken))
              ?? throw new InvalidDataException("Het manifest is leeg.")
            : new StagingManifest(1, [], []);
        if (manifest.Version != 1) throw new InvalidDataException("Onbekende manifestversie.");

        var files = manifest.Files.ToDictionary(file => file.Key, StringComparer.Ordinal);
        foreach (var file in files.Values)
            if (!KeyPattern.IsMatch(file.Key) || !KeyPattern.IsMatch(file.Sha256))
                throw new InvalidDataException("Het manifest bevat een ongeldige bestandsleutel of controlesom.");

        var selectedKeys = playlist.Episodes.Select(episode => episode.Key).Distinct(StringComparer.Ordinal).ToList();
        var removedCount = manifest.SelectedKeys.Count(key => !selectedKeys.Contains(key, StringComparer.Ordinal));
        manifest = manifest with
        {
            SelectedKeys = selectedKeys,
            SelectedEpisodes = playlist.Episodes.Select(episode =>
                new SelectedEpisode(episode.Rank, episode.Key, episode.Podcast, episode.Title)).ToList()
        };
        await SaveManifestAsync(manifestPath, manifest, cancellationToken);

        var downloaded = 0;
        var reused = 0;
        var recoveredPartialDownloads = 0;
        foreach (var episode in playlist.Episodes.Take(limit ?? int.MaxValue))
        {
            if (!KeyPattern.IsMatch(episode.Key)) throw new InvalidDataException("Ongeldige afleveringssleutel.");
            var audioPath = Path.Combine(directory, episode.Key + ".mp3");
            if (files.TryGetValue(episode.Key, out var known))
            {
                if (!File.Exists(audioPath) || new FileInfo(audioPath).Length != known.Bytes ||
                    !string.Equals(await HashFileAsync(audioPath, cancellationToken), known.Sha256, StringComparison.Ordinal))
                    throw new InvalidDataException($"De eerder opgeslagen audio op playlistpositie {episode.Rank} is gewijzigd of ontbreekt.");
                reused++;
                continue;
            }

            if (File.Exists(audioPath))
                throw new InvalidDataException($"Er bestaat al een onbeheerd bestand voor playlistpositie {episode.Rank}.");

            var uri = ValidateAudioUrl(episode.AudioUrl, requireMp3Path: true);
            var temporaryPath = Path.Combine(directory, episode.Key + ".partial");
            if (File.Exists(temporaryPath))
            {
                // The exclusive staging lock rules out a concurrent new-version download.
                File.Delete(temporaryPath);
                recoveredPartialDownloads++;
            }

            try
            {
                var staged = await DownloadAsync(client, uri, temporaryPath, episode.Key, cancellationToken);
                File.Move(temporaryPath, audioPath);
                files.Add(episode.Key, staged);
                manifest = manifest with { Files = files.Values.OrderBy(file => file.Key, StringComparer.Ordinal).ToList() };
                await SaveManifestAsync(manifestPath, manifest, cancellationToken);
                downloaded++;
            }
            catch (HttpRequestException exception)
            {
                throw new IOException($"Download mislukt op playlistpositie {episode.Rank}: {exception.StatusCode?.ToString() ?? "netwerkfout"}.", exception);
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
        }

        return new StagingResult(directory, selectedKeys.Count, downloaded, reused,
            selectedKeys.Count(key => !files.ContainsKey(key)), removedCount, recoveredPartialDownloads);
    }

    private static async Task<StagedAudio> DownloadAsync(
        HttpClient client,
        Uri uri,
        string temporaryPath,
        string key,
        CancellationToken cancellationToken)
    {
        for (var redirects = 0; redirects <= 5; redirects++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.StatusCode is HttpStatusCode.MovedPermanently or HttpStatusCode.Redirect or
                HttpStatusCode.RedirectMethod or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect)
            {
                if (response.Headers.Location is null || redirects == 5)
                    throw new InvalidDataException("De audiolink heeft te veel of een ongeldige doorverwijzing.");
                uri = ValidateAudioUrl(new Uri(uri, response.Headers.Location).ToString(), requireMp3Path: false);
                continue;
            }

            response.EnsureSuccessStatusCode();
            if (response.StatusCode != HttpStatusCode.OK)
                throw new InvalidDataException("De audiolink gaf geen volledig bestand terug.");
            if (response.Content.Headers.ContentLength is > MaximumAudioBytes)
                throw new InvalidDataException("Het audiobestand is groter dan 512 MB.");
            await using var output = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write);
            await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[64 * 1024];
            var header = new byte[3];
            var headerBytes = 0;
            long bytes = 0;
            while (true)
            {
                var count = await input.ReadAsync(buffer, cancellationToken);
                if (count == 0) break;
                bytes += count;
                if (bytes > MaximumAudioBytes) throw new InvalidDataException("Het audiobestand is groter dan 512 MB.");
                var headerCount = Math.Min(header.Length - headerBytes, count);
                if (headerCount > 0)
                {
                    Array.Copy(buffer, 0, header, headerBytes, headerCount);
                    headerBytes += headerCount;
                }
                hash.AppendData(buffer, 0, count);
                await output.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
            }
            if (bytes < 1024) throw new InvalidDataException("Het audiobestand is te klein om een podcastaflevering te zijn.");
            if (response.Content.Headers.ContentLength is long expectedBytes && expectedBytes != bytes)
                throw new InvalidDataException("De download is onvolledig.");
            if (headerBytes < 2 || !(headerBytes >= 3 && header[0] == 'I' && header[1] == 'D' && header[2] == '3') &&
                !(header[0] == 0xFF && (header[1] & 0xE0) == 0xE0))
                throw new InvalidDataException("Het ontvangen bestand lijkt geen MP3-audio.");
            return new StagedAudio(key, Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant(), bytes);
        }

        throw new InvalidDataException("De audiolink heeft te veel doorverwijzingen.");
    }

    private static Uri ValidateAudioUrl(string? value, bool requireMp3Path)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            uri.IsLoopback || uri.UserInfo.Length > 0 ||
            (requireMp3Path && !uri.AbsolutePath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("Deze aflevering heeft geen ondersteunde HTTPS-MP3-link.");
        return uri;
    }

    private static async Task<string> HashFileAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        return Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken)).ToLowerInvariant();
    }

    private static async Task SaveManifestAsync(string path, StagingManifest manifest, CancellationToken cancellationToken)
    {
        var temporaryPath = path + ".partial";
        await File.WriteAllTextAsync(temporaryPath, JsonSerializer.Serialize(manifest, JsonOptions), cancellationToken);
        File.Move(temporaryPath, path, overwrite: true);
    }
}
