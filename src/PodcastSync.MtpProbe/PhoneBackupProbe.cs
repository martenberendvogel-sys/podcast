using System.Text.RegularExpressions;
using System.Security.Cryptography;
using MediaDevices;

internal sealed record PhoneBackupFile(string Name, ulong Bytes);
internal sealed record PhoneBackupInventory(string Device, string[] RootFolders, string? BackupDirectory,
    int BackupCount, PhoneBackupFile[] RecentBackups, string? LatestBackup, string Warning);
internal sealed record PhoneBackupCopy(string Action, string Device, string Name, ulong Bytes,
    string LocalPath, bool Reused, bool Sha256Matches);

internal static class PhoneBackupProbe
{
    private static readonly Regex BackupName = new(@"^PodcastAddict_\d{8}_\d{6}\.backup$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    internal static PhoneBackupInventory Inspect(string friendlyName)
    {
        using var phone = FindPhone(friendlyName);
        phone.Connect(enableCache: false);
        try
        {
            var roots = phone.GetDirectories(@"\");
            var folder = FindBackupFolder(phone, roots);
            var files = folder is null ? [] : GetBackups(phone, folder)
                .Select(path => new PhoneBackupFile(Path.GetFileName(path), phone.GetFileInfo(path).Length))
                .ToArray();
            return new PhoneBackupInventory(phone.FriendlyName ?? friendlyName,
                roots.Select(path => Path.GetFileName(path.TrimEnd('\\'))).ToArray(),
                folder, files.Length, files.Take(5).ToArray(), files.FirstOrDefault()?.Name,
                "Alleen lezen: bestandsnamen en groottes. Geen back-up geopend of gekopieerd; telefoon en Garmin zijn niet gewijzigd.");
        }
        finally
        {
            phone.Disconnect();
        }
    }

    internal static PhoneBackupCopy CopyLatest(string friendlyName, string outputDirectory)
    {
        var directory = Path.GetFullPath(outputDirectory);
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException("De lokale doelmap bestaat niet; er is niets gekopieerd.");

        using var phone = FindPhone(friendlyName);
        phone.Connect(enableCache: false);
        try
        {
            var folder = FindBackupFolder(phone, phone.GetDirectories(@"\"))
                ?? throw new DirectoryNotFoundException("De exacte Podcast Addict-back-upmap is niet gevonden op de telefoon.");
            var source = GetBackups(phone, folder).FirstOrDefault()
                ?? throw new FileNotFoundException("Geen Podcast Addict-back-up met verwachte naam op de telefoon gevonden.");
            var name = Path.GetFileName(source);
            var target = Path.Combine(directory, name);
            var remote = phone.GetFileInfo(source);
            if (remote.Length < 1024)
                throw new InvalidDataException("De telefoonback-up is onverwacht klein; niets gekopieerd.");

            if (File.Exists(target))
            {
                using var remoteStream = remote.OpenRead();
                using var localStream = File.OpenRead(target);
                if (checked((ulong)new FileInfo(target).Length) != remote.Length ||
                    !CryptographicOperations.FixedTimeEquals(
                        SHA256.HashData(localStream), SHA256.HashData(remoteStream)))
                    throw new IOException("Een lokaal bestand met dezelfde naam wijkt af; niets overschreven.");
                return new PhoneBackupCopy("phone-copy-latest", phone.FriendlyName ?? friendlyName,
                    name, remote.Length, target, true, true);
            }

            var temporary = Path.Combine(directory, $".{name}.{Guid.NewGuid():N}.partial");
            try
            {
                using (var input = remote.OpenRead())
                using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    input.CopyTo(output);
                    output.Flush(flushToDisk: true);
                }
                if (checked((ulong)new FileInfo(temporary).Length) != remote.Length)
                    throw new IOException("De lokale back-up heeft niet dezelfde grootte als de telefoonback-up.");
                // Close both streams before File.Move: Windows cannot rename an open file.
                using (var localStream = File.OpenRead(temporary))
                using (var remoteStream = remote.OpenRead())
                {
                    if (!CryptographicOperations.FixedTimeEquals(
                        SHA256.HashData(localStream), SHA256.HashData(remoteStream)))
                        throw new IOException("De lokale back-up wijkt af van de opnieuw gelezen telefoonback-up.");
                }
                File.Move(temporary, target);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
            return new PhoneBackupCopy("phone-copy-latest", phone.FriendlyName ?? friendlyName,
                name, remote.Length, target, false, true);
        }
        finally
        {
            phone.Disconnect();
        }
    }

    private static MediaDevice FindPhone(string friendlyName)
    {
        if (string.IsNullOrWhiteSpace(friendlyName))
            throw new ArgumentException("Geef de exacte telefoonnaam uit Windows Verkenner op.");
        var devices = MediaDeviceManager.Instance.GetDevices()?
            .Where(device => string.Equals(device.FriendlyName, friendlyName, StringComparison.OrdinalIgnoreCase))
            .ToArray() ?? [];
        if (devices.Length != 1)
            throw new InvalidOperationException($"Verwacht precies één ontgrendelde MTP-telefoon met naam '{friendlyName}', gevonden: {devices.Length}. Controleer USB-bestandsoverdracht.");
        return devices[0];
    }

    private static string? FindBackupFolder(MediaDevice phone, IEnumerable<string> roots)
    {
        var backupFolders = new List<string>();
        foreach (var root in roots)
        {
            var android = FindChild(phone, root, "Android");
            var media = android is null ? null : FindChild(phone, android, "media");
            var app = media is null ? null : FindChild(phone, media, "com.bambuna.podcastaddict");
            var backup = app is null ? null : FindChild(phone, app, "backup");
            if (backup is not null) backupFolders.Add(backup);
        }
        if (backupFolders.Count > 1)
            throw new InvalidOperationException("Meer dan één Podcast Addict-back-upmap gevonden; er is niets gelezen of gekopieerd.");
        return backupFolders.SingleOrDefault();
    }

    private static string[] GetBackups(MediaDevice phone, string folder) => phone.GetFiles(folder)
        .Where(path => BackupName.IsMatch(Path.GetFileName(path)))
        .OrderByDescending(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private static string? FindChild(MediaDevice phone, string parent, string name)
    {
        var matches = phone.GetDirectories(parent)
            .Where(path => string.Equals(Path.GetFileName(path.TrimEnd('\\')), name, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (matches.Length > 1)
            throw new InvalidOperationException($"Meerdere mappen met naam '{name}' gevonden onder '{parent}'.");
        return matches.SingleOrDefault();
    }
}
