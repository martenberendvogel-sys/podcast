using System.Globalization;
using System.Text.Json;

namespace PodcastSync.Desktop;

internal sealed record SyncPlan(string BackupPath, int Selected, int Keep, int Add, int Replace, int Remove, int Unknown);

internal static class SyncOutput
{
    internal static SyncPlan ParsePlan(string output)
    {
        // Progress titles may contain braces or quotes; they are not JSON records.
        output = string.Join('\n', output.Split('\n').Where(line => !line.StartsWith("[Voortgang] ", StringComparison.Ordinal)));
        string? backupPath = null;
        JsonElement? plan = null;
        foreach (var json in JsonObjects(output))
        {
            JsonDocument document;
            try { document = JsonDocument.Parse(json); }
            catch (JsonException) { continue; }
            using (document)
            {
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object) { continue; }
                if (root.TryGetProperty("Action", out var action))
                {
                    if (action.GetString() == "phone-copy-latest"
                        && root.GetProperty("Sha256Matches").GetBoolean())
                    {
                        backupPath = root.GetProperty("LocalPath").GetString();
                    }
                    else if (action.GetString() == "sync-plan")
                    {
                        plan = root.Clone();
                    }
                }
            }
        }

        if (string.IsNullOrWhiteSpace(backupPath) || plan is null)
        {
            throw new InvalidOperationException("Geen bevestigde telefoonback-up of compleet Garmin-plan in de uitvoer gevonden.");
        }

        var data = plan.Value;
        var actions = data.GetProperty("Plan");
        return new SyncPlan(
            backupPath,
            data.GetProperty("SelectedCount").GetInt32(),
            actions.GetProperty("Keep").GetArrayLength(),
            actions.GetProperty("Add").GetArrayLength(),
            actions.GetProperty("Replace").GetArrayLength(),
            actions.GetProperty("Remove").GetArrayLength(),
            actions.GetProperty("Unknown").GetArrayLength());
    }

    internal static bool IsFreshBackup(string path, DateTime now)
    {
        var name = Path.GetFileName(path.Replace('\\', '/'));
        if (!name.StartsWith("PodcastAddict_", StringComparison.Ordinal)
            || !name.EndsWith(".backup", StringComparison.Ordinal))
        {
            return false;
        }

        var stamp = name["PodcastAddict_".Length..^".backup".Length];
        return DateTime.TryParseExact(stamp, "yyyyMMdd_HHmmss", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var date) && date >= now.AddHours(-48) && date <= now.AddHours(1);
    }

    private static IEnumerable<string> JsonObjects(string output)
    {
        var start = -1;
        var depth = 0;
        var quoted = false;
        var escaped = false;
        for (var i = 0; i < output.Length; i++)
        {
            var ch = output[i];
            if (start < 0)
            {
                if (ch == '{')
                {
                    start = i;
                    depth = 1;
                }
                continue;
            }

            if (escaped) { escaped = false; continue; }
            if (quoted && ch == '\\') { escaped = true; continue; }
            if (ch == '"') { quoted = !quoted; continue; }
            if (quoted) { continue; }
            if (ch == '{') { depth++; }
            if (ch == '}' && --depth == 0)
            {
                yield return output[start..(i + 1)];
                start = -1;
            }
        }
    }
}
