using System.Security.Cryptography;
using System.Text.Json;

namespace PodcastSync.Desktop;

internal static class GarminCleanupSelfTest
{
    internal static void Run()
    {
        var root = Path.Combine(Path.GetTempPath(), "podcast-cleanup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var old = MakeSet(root, "20261028164753");
            var latest = MakeSet(root, "20261029191706");
            var extra = MakeSet(root, "20261029174758");
            File.WriteAllText(Path.Combine(extra, "persoonlijk.txt"), "bewaren");
            Directory.CreateDirectory(Path.Combine(root, "podcast-staging"));
            File.WriteAllText(Path.Combine(root, "starten.txt"), "bewaren");
            var plan = GarminCleanup.Plan(root);
            if (plan.Keep != latest || plan.Candidates.Length != 1 || plan.Candidates[0].Path != old || plan.Skipped.Length != 1)
                throw new Exception("Opruimplan bewaart niet de nieuwste set of accepteert gebruikersbestanden.");
            var latestExtra = Path.Combine(latest, "onbekend.txt");
            File.WriteAllText(latestExtra, "nieuwste set niet betrouwbaar");
            if (GarminCleanup.Plan(root).Candidates.Length != 0)
                throw new Exception("Opruimen moet blokkeren als de nieuwste set niet volledig is gecontroleerd.");
            File.Delete(latestExtra);
            var audio = Directory.GetFiles(old, "*.mp3").Single();
            File.WriteAllBytes(audio, new byte[] { 9, 9, 9 });
            if (GarminCleanup.Plan(root).Candidates.Length != 0)
                throw new Exception("Gewijzigde audio mag niet worden opgeruimd.");
            if (!File.Exists(Path.Combine(root, "starten.txt")) || !Directory.Exists(Path.Combine(root, "podcast-staging"))
                || Directory.GetFiles(root, "*.backup").Length != 3)
                throw new Exception("Plannen heeft gebruikersdata gewijzigd.");
            Directory.CreateDirectory(Path.Combine(latest, "submap"));
            var newest = MakeSet(root, "20261030120000");
            var secondPlan = GarminCleanup.Plan(root);
            if (secondPlan.Keep != newest || secondPlan.Candidates.Length != 0 || secondPlan.Skipped.Length != 3)
                throw new Exception("Een set met submap mag niet in het opruimvoorstel komen.");
        }
        finally { Directory.Delete(root, true); }
        Console.WriteLine("Cleanup boundary checks passed (no recycling performed).");
    }

    private static string MakeSet(string root, string stamp)
    {
        var folder = Path.Combine(root, "Garmin klaar Podcast Sync " + stamp);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(root, $"PodcastAddict_{stamp[..8]}_{stamp[8..]}.backup"), "synthetisch");
        var key = new string('a', 64);
        byte[] audio = [1, 2, 3];
        File.WriteAllBytes(Path.Combine(folder, key + ".mp3"), audio);
        File.WriteAllText(Path.Combine(folder, "podcast-sync-garmin-ready.json"), JsonSerializer.Serialize(new
        {
            Version = 1, Album = "Recente podcasts", OrderMode = "GarminPodcastDescendingTrack", SelectedKeys = new[] { key },
            Files = new[] { new { Key = key, Bytes = audio.Length, Sha256 = Convert.ToHexString(SHA256.HashData(audio)) } }
        }), new System.Text.UTF8Encoding(true));
        return folder;
    }
}
