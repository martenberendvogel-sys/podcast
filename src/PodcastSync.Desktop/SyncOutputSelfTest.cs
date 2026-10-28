namespace PodcastSync.Desktop;

internal static class SyncOutputSelfTest
{
    internal static void Run()
    {
        const string output = """
            Before output
            {"Action":"phone-copy-latest","Sha256Matches":true,"LocalPath":"C:\\video\\AI\\PodcastAddict_20261028_164753.backup"}
            {"Action":"sync-plan","SelectedCount":2,"Plan":{"Keep":["a"],"Add":["b"],"Replace":[],"Remove":[],"Unknown":[]}}
            After output
            """;
        var plan = SyncOutput.ParsePlan(output);
        if (plan.Selected != 2 || plan.Keep != 1 || plan.Add != 1 || plan.Unknown != 0
            || plan.BackupPath != @"C:\video\AI\PodcastAddict_20261028_164753.backup")
        {
            throw new Exception("Planparser gaf onjuiste gegevens.");
        }

        var now = new DateTime(2026, 10, 28, 17, 0, 0);
        if (!SyncOutput.IsFreshBackup(plan.BackupPath, now)
            || SyncOutput.IsFreshBackup(plan.BackupPath, now.AddDays(4)))
        {
            throw new Exception("Back-updatumcontrole gaf onjuist resultaat.");
        }

        var failed = false;
        try { SyncOutput.ParsePlan("{geen JSON}\n{\"Action\":\"sync-plan\",\"SelectedCount\":0,\"Plan\":{\"Keep\":[],\"Add\":[],\"Replace\":[],\"Remove\":[],\"Unknown\":[]}}"); }
        catch (InvalidOperationException) { failed = true; }
        if (!failed) { throw new Exception("Plan zonder gecontroleerde telefoonkopie werd geaccepteerd."); }

        Console.WriteLine("Desktop plan checks passed.");
    }
}
