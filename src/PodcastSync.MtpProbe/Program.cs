using System.Text.Json;
using MediaDevices;

if (args.Length != 0 && !(args.Length == 1 && args[0] == "probe"))
{
    Console.Error.WriteLine("Gebruik: dotnet run --project src/PodcastSync.MtpProbe -- probe");
    return 2;
}

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
        DeviceFile[] files = recent is null ? [] : garmin.GetFiles(recent)
            .Select(path => ReadDeviceFile(garmin, path))
            .OrderBy(file => file.Name, StringComparer.Ordinal)
            .ToArray();

        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Device = garmin.FriendlyName,
            RootFolders = rootFolders.Select(Path.GetFileName).ToArray(),
            StorageFound = storage is not null,
            PodcastsFound = podcasts is not null,
            RecentPodcastsFound = recent is not null,
            RecentPodcastsFileCount = files.Length,
            Files = files,
            Warning = "Alleen lezen. Geen bestand naar de Garmin gekopieerd of verwijderd."
        }, new JsonSerializerOptions { WriteIndented = true }));
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

internal sealed record DeviceFile(string Name, ulong? Bytes, string? MetadataError);
