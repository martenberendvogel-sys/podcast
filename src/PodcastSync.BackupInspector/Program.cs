using PodcastSync.BackupInspector;

if (!((args.Length == 2 && (args[0] == "inspect" || args[0] == "playlist")) ||
      (args.Length == 3 && args[0] == "compare")))
{
    Console.Error.WriteLine("Gebruik: podcast-sync inspect|playlist <back-upbestand> | compare <oude.backup> <nieuwe.backup>");
    return 2;
}

try
{
    if (args[0] == "compare")
    {
        var previous = await PodcastAddictPlaylist.ReadAsync(args[1]);
        var current = await PodcastAddictPlaylist.ReadAsync(args[2]);
        Console.WriteLine(ReportWriter.ToJson(PodcastAddictPlaylist.Compare(previous, current)));
    }
    else if (args[0] == "playlist" || Path.GetExtension(args[1]).Equals(".backup", StringComparison.OrdinalIgnoreCase))
        Console.WriteLine(ReportWriter.ToJson(await PodcastAddictPlaylist.ReadAsync(args[1])));
    else
        Console.WriteLine(ReportWriter.ToJson(await BackupInspector.InspectAsync(args[1])));
    return 0;
}
catch (Exception exception) when (exception is IOException or InvalidDataException or ArgumentException or Microsoft.Data.Sqlite.SqliteException)
{
    Console.Error.WriteLine($"Inspectie mislukt: {exception.Message}");
    return 1;
}

