using PodcastSync.BackupInspector;

Console.OutputEncoding = new System.Text.UTF8Encoding(false);

if (!((args.Length == 2 && (args[0] == "inspect" || args[0] == "playlist")) ||
      (args.Length == 3 && (args[0] == "compare" || args[0] == "prepare")) ||
      (args.Length == 5 && args[0] == "prepare" && args[3] == "--limit")))
{
    Console.Error.WriteLine("Gebruik: podcast-sync inspect|playlist <back-up> | compare <oud.backup> <nieuw.backup> | prepare <back-up> <lege uitvoermap> [--limit aantal]");
    return 2;
}

try
{
    if (args[0] == "prepare")
    {
        int? limit = null;
        if (args.Length == 5)
        {
            if (!int.TryParse(args[4], out var parsedLimit) || parsedLimit < 1)
                throw new ArgumentException("--limit vereist een positief geheel getal.");
            limit = parsedLimit;
        }
        var playlist = await PodcastAddictPlaylist.ReadAsync(args[1]);
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            Timeout = TimeSpan.FromMinutes(30)
        };
        Console.WriteLine(ReportWriter.ToJson(await PlaylistStager.PrepareAsync(playlist, args[2], limit, client,
            progress: message => Console.WriteLine("[Voortgang] " + message.Replace('\r', ' ').Replace('\n', ' ')))));
    }
    else if (args[0] == "compare")
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
catch (Exception exception) when (exception is IOException or InvalidDataException or ArgumentException or InvalidOperationException or
                                 UnauthorizedAccessException or System.Text.Json.JsonException or Microsoft.Data.Sqlite.SqliteException)
{
    Console.Error.WriteLine($"Bewerking mislukt: {exception.Message}");
    return 1;
}

