using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace PodcastSync.BackupInspector;

public sealed record EpisodeCandidate(
    string Source,
    string? Podcast,
    string? Title,
    long? PositionMilliseconds,
    string? AudioReference);

public sealed record InspectionReport(
    string InputFile,
    IReadOnlyList<string> InspectedEntries,
    IReadOnlyDictionary<string, IReadOnlyList<string>> DiscoveredFields,
    IReadOnlyList<EpisodeCandidate> Episodes,
    IReadOnlyList<string> Notes);

public static class BackupInspector
{
    private static readonly string[] PositionKeys = ["position", "playbackposition", "currentposition", "progress", "resume"];
    private static readonly string[] TitleKeys = ["title", "episodetitle", "name"];
    private static readonly string[] PodcastKeys = ["podcast", "feedtitle", "author"];
    private static readonly string[] AudioKeys = ["file", "path", "download", "mediaurl", "audio"];

    public static async Task<InspectionReport> InspectAsync(string inputFile)
    {
        if (!File.Exists(inputFile)) throw new FileNotFoundException("Bestand bestaat niet.", inputFile);

        var entries = new List<(string Name, string Content)>();
        if (Path.GetExtension(inputFile).Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            using var archive = ZipFile.OpenRead(inputFile);
            foreach (var entry in archive.Entries.Where(e => e.Length > 0 && IsStructured(e.Name)))
            using (var reader = new StreamReader(entry.Open(), Encoding.UTF8, true))
                entries.Add((entry.FullName, await reader.ReadToEndAsync()));
        }
        else
        {
            entries.Add((Path.GetFileName(inputFile), await File.ReadAllTextAsync(inputFile)));
        }

        var episodes = new List<EpisodeCandidate>();
        var discoveredFields = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        var notes = new List<string>();
        foreach (var (name, content) in entries)
        {
            try
            {
                var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (LooksLikeJson(content)) ExtractJson(name, content, episodes, fields);
                else ExtractXml(name, content, episodes, fields);
                discoveredFields[name] = fields.Order(StringComparer.OrdinalIgnoreCase).ToList();
            }
            catch (Exception exception) when (exception is JsonException or System.Xml.XmlException)
            {
                notes.Add($"{name}: geen leesbare JSON of XML ({exception.Message}).");
            }
        }

        if (episodes.Count == 0)
            notes.Add("Geen afleveringkandidaten gevonden. Bewaar dit rapport en voeg een echte back-up toe om de veldkoppeling te verfijnen.");

        return new InspectionReport(inputFile, entries.Select(e => e.Name).ToList(), discoveredFields, episodes, notes);
    }

    private static bool IsStructured(string name) => name.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase);
    private static bool LooksLikeJson(string text) => text.TrimStart().StartsWith('{') || text.TrimStart().StartsWith('[');

    private static void ExtractJson(string source, string json, List<EpisodeCandidate> output, ISet<string> discoveredFields)
    {
        using var document = JsonDocument.Parse(json);
        WalkJson(source, document.RootElement, output, discoveredFields);
    }

    private static void WalkJson(string source, JsonElement element, List<EpisodeCandidate> output, ISet<string> discoveredFields)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var fields = element.EnumerateObject().ToDictionary(p => p.Name, p => p.Value, StringComparer.OrdinalIgnoreCase);
            discoveredFields.UnionWith(fields.Keys);
            AddCandidate(source, fields.ToDictionary(p => p.Key, p => p.Value.ToString(), StringComparer.OrdinalIgnoreCase), output);
            foreach (var value in fields.Values) WalkJson(source, value, output, discoveredFields);
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray()) WalkJson(source, item, output, discoveredFields);
    }

    private static void ExtractXml(string source, string xml, List<EpisodeCandidate> output, ISet<string> discoveredFields)
    {
        var document = XDocument.Parse(xml);
        foreach (var element in document.Descendants())
        {
            var fields = element.Attributes().ToDictionary(a => a.Name.LocalName, a => a.Value, StringComparer.OrdinalIgnoreCase);
            foreach (var child in element.Elements()) fields[child.Name.LocalName] = child.Value;
            discoveredFields.UnionWith(fields.Keys);
            AddCandidate(source, fields, output);
        }
    }

    private static void AddCandidate(string source, IReadOnlyDictionary<string, string> fields, List<EpisodeCandidate> output)
    {
        string? Get(string[] keys) => fields.FirstOrDefault(pair => keys.Any(key => pair.Key.Contains(key, StringComparison.OrdinalIgnoreCase))).Value;
        var position = Get(PositionKeys);
        var title = Get(TitleKeys);
        var audio = Get(AudioKeys);
        if (position is null && title is null && audio is null) return;

        output.Add(new EpisodeCandidate(source, Get(PodcastKeys), title, ParsePosition(position), audio));
    }

    private static long? ParsePosition(string? value)
    {
        if (long.TryParse(value, out var milliseconds)) return milliseconds;
        return TimeSpan.TryParse(value, out var time) ? (long)time.TotalMilliseconds : null;
    }
}

public static class ReportWriter
{
    public static string ToJson<T>(T report) => JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
}

