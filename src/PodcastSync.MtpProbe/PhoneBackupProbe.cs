using System.Text.RegularExpressions;
using MediaDevices;

internal sealed record PhoneBackupFile(string Name, ulong Bytes);
internal sealed record PhoneBackupInventory(string Device, string[] RootFolders, string? BackupDirectory,
    int BackupCount, PhoneBackupFile[] RecentBackups, string? LatestBackup, string Warning);

internal static class PhoneBackupProbe
{
    private static readonly Regex BackupName = new(@"^PodcastAddict_\d{8}_\d{6}\.backup$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    internal static PhoneBackupInventory Inspect(string friendlyName)
    {
        if (string.IsNullOrWhiteSpace(friendlyName))
            throw new ArgumentException("Geef de exacte telefoonnaam uit Windows Verkenner op.");
        var devices = MediaDeviceManager.Instance.GetDevices()?
            .Where(device => string.Equals(device.FriendlyName, friendlyName, StringComparison.OrdinalIgnoreCase))
            .ToArray() ?? [];
        if (devices.Length != 1)
            throw new InvalidOperationException($"Verwacht precies één ontgrendelde MTP-telefoon met naam '{friendlyName}', gevonden: {devices.Length}. Controleer USB-bestandsoverdracht.");

        using var phone = devices[0];
        phone.Connect(enableCache: false);
        try
        {
            var roots = phone.GetDirectories(@"\");
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

            var folder = backupFolders.SingleOrDefault();
            var files = folder is null ? [] : phone.GetFiles(folder)
                .Where(path => BackupName.IsMatch(Path.GetFileName(path)))
                .Select(path => new PhoneBackupFile(Path.GetFileName(path), phone.GetFileInfo(path).Length))
                .OrderByDescending(file => file.Name, StringComparer.OrdinalIgnoreCase)
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
