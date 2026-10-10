using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using PodcastSync.BackupInspector;

var root = Path.Combine(Path.GetTempPath(), $"podcast-sync-tests-{Guid.NewGuid():N}");
Directory.CreateDirectory(root);
try
{
    var json = Path.Combine(root, "backup.json");
    await File.WriteAllTextAsync(json, """{"episodes":[{"podcast":"Example","title":"Run 1","playbackPosition":1380000,"file":"/audio/run1.mp3"}]}""");
    var jsonReport = await BackupInspector.InspectAsync(json);
    Assert(jsonReport.Episodes.Single().PositionMilliseconds == 1380000, "JSON-luisterpositie ontbreekt.");
    Assert(jsonReport.DiscoveredFields["backup.json"].Contains("playbackPosition"), "JSON-velden ontbreken in het rapport.");

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

    var previousBackup = Path.Combine(root, "previous.backup");
    var currentBackup = Path.Combine(root, "current.backup");
    await CreateBackup(previousBackup, [1, 2]);
    await CreateBackup(currentBackup, [2, 3]);
    var previousPlaylist = await PodcastAddictPlaylist.ReadAsync(previousBackup);
    var currentPlaylist = await PodcastAddictPlaylist.ReadAsync(currentBackup);
    Assert(previousPlaylist.Episodes.Count == 2, "Playlistselectie ontbreekt.");
    Assert(previousPlaylist.Episodes[0].Rank == 1, "Playlistvolgorde ontbreekt.");
    Assert(previousPlaylist.Episodes[0].AudioUrl == "https://example.test/1.mp3", "Streamlink ontbreekt.");
    var changes = PodcastAddictPlaylist.Compare(previousPlaylist, currentPlaylist);
    Assert(changes.Added.Count == 1 && changes.Added[0].Title == "Episode 3", "Toevoeging ontbreekt.");
    Assert(changes.Removed.Count == 1 && changes.Removed[0].Title == "Episode 1", "Verwijdering ontbreekt.");
    Assert(changes.Retained.Count == 1 && changes.Retained[0].Title == "Episode 2", "Bestaande aflevering ontbreekt.");

    var stageDirectory = Path.Combine(root, "staged");
    using var handler = new FakeAudioHandler();
    using var client = new HttpClient(handler);
    var firstStage = await PlaylistStager.PrepareAsync(previousPlaylist, stageDirectory, 1, client);
    Assert(firstStage.Selected == 2 && firstStage.Downloaded == 1 && firstStage.Pending == 1, "Beperkte eerste voorbereiding klopt niet.");
    var manifestText = await File.ReadAllTextAsync(Path.Combine(stageDirectory, "podcast-sync-manifest.json"));
    var manifest = JsonSerializer.Deserialize<StagingManifest>(manifestText)!;
    Assert(manifest.SelectedEpisodes.Count == 2 && manifest.SelectedEpisodes[0].Title == "Episode 1", "Playlistinformatie ontbreekt in het manifest.");
    Assert(!manifestText.Contains("https://", StringComparison.Ordinal), "Het manifest bevat een privé-audiolink.");
    var repeatedStage = await PlaylistStager.PrepareAsync(previousPlaylist, stageDirectory, 1, client);
    Assert(repeatedStage.Reused == 1 && handler.RequestCount == 1, "Een bestaand audiobestand werd opnieuw gedownload.");
    var changedStage = await PlaylistStager.PrepareAsync(currentPlaylist, stageDirectory, null, client);
    Assert(changedStage.PreviouslyStagedButNoLongerSelected == 1, "Verdwenen playlistaflevering ontbreekt in het plan.");
    Assert(changedStage.Downloaded == 2 && changedStage.Pending == 0, "Nieuwe afleveringen zijn niet voorbereid.");
    Assert(File.Exists(Path.Combine(stageDirectory, previousPlaylist.Episodes[0].Key + ".mp3")), "Eerdere audio is ten onrechte verwijderd.");

    var invalidDirectory = Path.Combine(root, "invalid-audio");
    using var invalidClient = new HttpClient(new FixedResponseHandler(() => new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent("<html>Geen MP3</html>")
    }));
    await AssertInvalidAudioAsync(previousPlaylist, invalidDirectory, invalidClient);
    Assert(!Directory.EnumerateFiles(invalidDirectory, "*.mp3").Any(), "Ongeldige audio is toch opgeslagen.");

    var redirectDirectory = Path.Combine(root, "unsafe-redirect");
    using var redirectClient = new HttpClient(new FixedResponseHandler(() => new HttpResponseMessage(HttpStatusCode.Redirect)
    {
        Headers = { Location = new Uri("http://example.test/audio.mp3") }
    }));
    await AssertInvalidAudioAsync(previousPlaylist, redirectDirectory, redirectClient);

    Console.WriteLine("20 parser-, playlist- en voorbereidingscontroles geslaagd.");
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

static async Task AssertInvalidAudioAsync(PlaylistReport playlist, string directory, HttpClient client)
{
    try
    {
        await PlaylistStager.PrepareAsync(playlist, directory, 1, client);
        throw new InvalidOperationException("Ongeldige audio werd geaccepteerd.");
    }
    catch (InvalidDataException)
    {
        // Expected: no audio should be promoted into the staging directory.
    }
}

static async Task CreateBackup(string backupPath, int[] playlistIds)
{
    var databasePath = Path.ChangeExtension(backupPath, ".db");
    using (var connection = new SqliteConnection($"Data Source={databasePath}"))
    {
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE podcasts (_id INTEGER PRIMARY KEY, name TEXT, feed_url TEXT);
            CREATE TABLE episodes (_id INTEGER PRIMARY KEY, podcast_id INTEGER, name TEXT, guid TEXT, download_url TEXT, url TEXT);
            CREATE TABLE ordered_list (rank INTEGER, id INTEGER, type INTEGER, filter INTEGER);
            INSERT INTO podcasts VALUES (10, 'Example Podcast', 'https://example.test/feed.xml');
            INSERT INTO episodes VALUES (1, 10, 'Episode 1', 'guid-1', 'https://example.test/1.mp3', NULL);
            INSERT INTO episodes VALUES (2, 10, 'Episode 2', 'guid-2', 'https://example.test/2.mp3', NULL);
            INSERT INTO episodes VALUES (3, 10, 'Episode 3', 'guid-3', 'https://example.test/3.mp3', NULL);
            INSERT INTO ordered_list VALUES (0, 3, 3, 17);
            """;
        await command.ExecuteNonQueryAsync();
        foreach (var (id, index) in playlistIds.Select((id, index) => (id, index)))
        {
            command.CommandText = "INSERT INTO ordered_list VALUES ($rank, $id, 1, -1)";
            command.Parameters.Clear();
            command.Parameters.AddWithValue("$rank", index + 1);
            command.Parameters.AddWithValue("$id", id);
            await command.ExecuteNonQueryAsync();
        }
    }

    using var archive = ZipFile.Open(backupPath, ZipArchiveMode.Create);
    var entry = archive.CreateEntry("podcastAddict.db");
    await using var entryStream = entry.Open();
    await using var databaseStream = File.OpenRead(databasePath);
    await databaseStream.CopyToAsync(entryStream);
}

sealed class FakeAudioHandler : HttpMessageHandler
{
    public int RequestCount { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestCount++;
        var audio = new byte[2048];
        audio[0] = 0x49;
        audio[1] = 0x44;
        audio[2] = 0x33;
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(audio)
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
        return Task.FromResult(response);
    }
}

sealed class FixedResponseHandler(Func<HttpResponseMessage> createResponse) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(createResponse());
}

