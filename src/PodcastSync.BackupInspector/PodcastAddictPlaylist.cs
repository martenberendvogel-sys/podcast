using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

namespace PodcastSync.BackupInspector;

public sealed record PlaylistEpisode(int Rank, string Key, string Podcast, string Title, string? AudioUrl, string? FeedUrl);

public sealed record PlaylistReport(string InputFile, string SelectionRule, IReadOnlyList<PlaylistEpisode> Episodes);

public sealed record PlaylistChanges(
    IReadOnlyList<PlaylistEpisode> Added,
    IReadOnlyList<PlaylistEpisode> Removed,
    IReadOnlyList<PlaylistEpisode> Retained);

public static class PodcastAddictPlaylist
{
    private const string SelectionRule = "ordered_list.type=1 AND ordered_list.filter=-1 (geverifieerd met twee back-ups uit dezelfde appversie)";

    public static async Task<PlaylistReport> ReadAsync(string backupPath)
    {
        if (!File.Exists(backupPath)) throw new FileNotFoundException("Back-upbestand bestaat niet.", backupPath);

        using var archive = ZipFile.OpenRead(backupPath);
        var databaseEntry = archive.GetEntry("podcastAddict.db")
            ?? throw new InvalidDataException("Deze back-up bevat geen podcastAddict.db.");
        if (databaseEntry.Length > 512L * 1024 * 1024)
            throw new InvalidDataException("De database in de back-up is groter dan 512 MB.");

        // SQLite needs a seekable file. Never extract an archive path or alter the source backup.
        var temporaryDirectory = Directory.CreateTempSubdirectory("podcast-addict-");
        var temporaryPath = Path.Combine(temporaryDirectory.FullName, "backup.db");
        try
        {
            await using (var input = databaseEntry.Open())
            await using (var output = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write))
                await input.CopyToAsync(output);

            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = temporaryPath,
                Mode = SqliteOpenMode.ReadOnly,
                Cache = SqliteCacheMode.Private
            }.ToString();
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT o.rank, e.guid, e.download_url, e.url, e.name, p.name, p.feed_url
                FROM ordered_list AS o
                JOIN episodes AS e ON e._id = o.id
                LEFT JOIN podcasts AS p ON p._id = e.podcast_id
                WHERE o.type = 1 AND o.filter = -1
                ORDER BY o.rank, o.id
                """;

            var episodes = new List<PlaylistEpisode>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var guid = GetString(reader, 1);
                var downloadUrl = GetString(reader, 2);
                var episodeUrl = GetString(reader, 3);
                var feedUrl = GetString(reader, 6);
                var keySource = !string.IsNullOrWhiteSpace(guid) ? guid : downloadUrl ?? episodeUrl;
                if (string.IsNullOrWhiteSpace(keySource))
                    throw new InvalidDataException("Een playlistaflevering mist zowel een GUID als een audio-URL.");

                var keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{feedUrl}\n{keySource}"));
                episodes.Add(new PlaylistEpisode(
                    reader.GetInt32(0),
                    Convert.ToHexString(keyBytes).ToLowerInvariant(),
                    GetString(reader, 5) ?? "",
                    GetString(reader, 4) ?? "",
                    downloadUrl ?? episodeUrl,
                    feedUrl));
            }

            return new PlaylistReport(backupPath, SelectionRule, episodes);
        }
        finally
        {
            temporaryDirectory.Delete(recursive: true);
        }
    }

    public static PlaylistChanges Compare(PlaylistReport previous, PlaylistReport current)
    {
        var oldKeys = previous.Episodes.Select(e => e.Key).ToHashSet(StringComparer.Ordinal);
        var newKeys = current.Episodes.Select(e => e.Key).ToHashSet(StringComparer.Ordinal);
        return new PlaylistChanges(
            current.Episodes.Where(e => !oldKeys.Contains(e.Key)).ToList(),
            previous.Episodes.Where(e => !newKeys.Contains(e.Key)).ToList(),
            current.Episodes.Where(e => oldKeys.Contains(e.Key)).ToList());
    }

    private static string? GetString(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
}
