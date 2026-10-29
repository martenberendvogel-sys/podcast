using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;

namespace PodcastSync.Desktop;

internal sealed record CleanupCandidate(string Path, long Bytes, string Fingerprint);
internal sealed record CleanupPlan(string? Keep, CleanupCandidate[] Candidates, string[] Skipped);

internal static class GarminCleanup
{
    private const string ManifestName = "podcast-sync-garmin-ready.json";
    private const string Prefix = "Garmin klaar Podcast Sync ";

    internal static CleanupPlan Plan(string root)
    {
        root = System.IO.Path.GetFullPath(root);
        RequireLocalDirectory(root);
        var folders = Directory.GetDirectories(root).Where(path => IsSetName(System.IO.Path.GetFileName(path)))
            .OrderByDescending(System.IO.Path.GetFileName, StringComparer.Ordinal).ToArray();
        var candidates = new List<CleanupCandidate>();
        var skipped = new List<string>();
        if (folders.Length > 0)
        {
            try { Validate(folders[0]); }
            catch (Exception ex) when (IsValidationError(ex))
            {
                return new CleanupPlan(folders[0], [], ["Nieuwste set is niet volledig gecontroleerd; niets opruimen: " + ex.Message]);
            }
        }
        foreach (var path in folders.Skip(1))
        {
            try { candidates.Add(Validate(path)); }
            catch (Exception ex) when (IsValidationError(ex))
            {
                skipped.Add(System.IO.Path.GetFileName(path) + ": " + ex.Message);
            }
        }
        return new CleanupPlan(folders.FirstOrDefault(), candidates.ToArray(), skipped.ToArray());
    }

    internal static CleanupCandidate Validate(string path)
    {
        var name = System.IO.Path.GetFileName(path);
        if (!IsSetName(name)) throw new InvalidDataException("Geen herkende Garmin-set.");
        var parent = System.IO.Path.GetDirectoryName(path)!;
        RequireLocalDirectory(parent);
        RequireLocalDirectory(path);
        var stamp = name[Prefix.Length..];
        if (!File.Exists(System.IO.Path.Combine(parent, $"PodcastAddict_{stamp[..8]}_{stamp[8..]}.backup")))
            throw new InvalidDataException("Bijbehorende back-up ontbreekt.");
        var entries = Directory.GetFileSystemEntries(path);
        if (entries.Any(entry => (File.GetAttributes(entry) & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0))
            throw new InvalidDataException("Bevat submappen of bestandskoppelingen.");
        var manifestPath = System.IO.Path.Combine(path, ManifestName);
        var manifestBytes = File.ReadAllBytes(manifestPath);
        // Windows PowerShell writes a UTF-8 BOM; JsonDocument expects JSON without it.
        using var json = JsonDocument.Parse(manifestBytes.AsMemory(manifestBytes.AsSpan().StartsWith(new byte[] { 239, 187, 191 }) ? 3 : 0));
        var data = json.RootElement;
        if (data.GetProperty("Version").GetInt32() != 1 || data.GetProperty("Album").GetString() != "Recente podcasts"
            || data.GetProperty("OrderMode").GetString() != "GarminPodcastDescendingTrack")
            throw new InvalidDataException("Onbekend manifest.");
        var keys = data.GetProperty("SelectedKeys").EnumerateArray().Select(item => item.GetString()!).ToArray();
        var files = data.GetProperty("Files").EnumerateArray().ToArray();
        if (keys.Length == 0 || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length || files.Length != keys.Length
            || entries.Length != keys.Length + 1)
            throw new InvalidDataException("Onvolledige set of extra bestanden.");
        long bytes = manifestBytes.Length;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in files)
        {
            var key = file.GetProperty("Key").GetString()!;
            if (!Regex.IsMatch(key, "^[0-9a-f]{64}$") || !keys.Contains(key, StringComparer.Ordinal) || !seen.Add(key))
                throw new InvalidDataException("Ongeldige bestandssleutel.");
            var audio = System.IO.Path.Combine(path, key + ".mp3");
            using var stream = File.OpenRead(audio);
            if (stream.Length != file.GetProperty("Bytes").GetInt64() || !string.Equals(
                Convert.ToHexString(SHA256.HashData(stream)), file.GetProperty("Sha256").GetString(), StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("MP3 gewijzigd of onvolledig; map blijft staan.");
            bytes = checked(bytes + stream.Length);
        }
        return new CleanupCandidate(path, bytes, Convert.ToHexString(SHA256.HashData(manifestBytes)));
    }

    internal static void Recycle(CleanupCandidate candidate)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        var root = System.IO.Path.GetDirectoryName(candidate.Path)!;
        var latest = Directory.GetDirectories(root).Where(path => IsSetName(System.IO.Path.GetFileName(path)))
            .OrderByDescending(System.IO.Path.GetFileName, StringComparer.Ordinal).FirstOrDefault();
        if (latest is null) throw new IOException("Geen nieuwste set gevonden; opruimen gestopt.");
        Validate(latest);
        if (string.Equals(candidate.Path, latest, StringComparison.OrdinalIgnoreCase) || Validate(candidate.Path) != candidate)
            throw new IOException("Opruimplan is gewijzigd. Maak opnieuw een overzicht.");
        FileSystem.DeleteDirectory(candidate.Path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin, UICancelOption.ThrowException);
        if (Directory.Exists(candidate.Path)) throw new IOException("De map staat nog op de pc; opruimen gestopt.");
    }

    private static bool IsSetName(string name) => name.StartsWith(Prefix, StringComparison.Ordinal)
        && DateTime.TryParseExact(name[Prefix.Length..], "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    private static bool IsValidationError(Exception ex) => ex is IOException or InvalidDataException
        or UnauthorizedAccessException or JsonException or InvalidOperationException or KeyNotFoundException
        or FormatException or OverflowException or ArgumentException;

    private static void RequireLocalDirectory(string path)
    {
        for (var directory = new DirectoryInfo(path); directory is not null; directory = directory.Parent)
            if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Mapkoppelingen worden niet opgeruimd.");
        var drive = new DriveInfo(System.IO.Path.GetPathRoot(path)!);
        if (drive.DriveType != DriveType.Fixed) throw new IOException("Kies een lokale vaste schijf met Prullenbak.");
    }
}
