using System.IO.Compression;
using PodcastSync.BackupInspector;

var root = Path.Combine(Path.GetTempPath(), $"podcast-sync-tests-{Guid.NewGuid():N}");
Directory.CreateDirectory(root);
try
{
    var json = Path.Combine(root, "backup.json");
    await File.WriteAllTextAsync(json, """{"episodes":[{"podcast":"Example","title":"Run 1","playbackPosition":1380000,"file":"/audio/run1.mp3"}]}""");
    var jsonReport = await BackupInspector.InspectAsync(json);
    Assert(jsonReport.Episodes.Single().PositionMilliseconds == 1380000, "JSON-luisterpositie ontbreekt.");

    var xml = Path.Combine(root, "backup.xml");
    await File.WriteAllTextAsync(xml, "<backup><episode podcast=\"Example\" title=\"Run 2\" position=\"00:23:00\" path=\"/audio/run2.mp3\" /></backup>");
    var xmlReport = await BackupInspector.InspectAsync(xml);
    Assert(xmlReport.Episodes.Single().PositionMilliseconds == 1_380_000, "XML-tijdnotatie ontbreekt.");

    var zip = Path.Combine(root, "backup.zip");
    using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
    {
        var entry = archive.CreateEntry("data/episodes.json");
        await using var writer = new StreamWriter(entry.Open());
        await writer.WriteAsync("[{\"title\":\"Run 3\",\"position\":\"60000\"}]");
    }
    var zipReport = await BackupInspector.InspectAsync(zip);
    Assert(zipReport.Episodes.Single().PositionMilliseconds == 60000, "ZIP-inhoud ontbreekt.");

    Console.WriteLine("3 parsercontroles geslaagd.");
    return 0;
}
finally
{
    Directory.Delete(root, true);
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

