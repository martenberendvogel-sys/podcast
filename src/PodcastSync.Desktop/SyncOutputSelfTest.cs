namespace PodcastSync.Desktop;

internal static class SyncOutputSelfTest
{
    internal static void Run()
    {
        GarminCleanupSelfTest.Run();
        const string output = """
            Before output
            {"Action":"phone-copy-latest","Sha256Matches":true,"LocalPath":"C:\\video\\AI\\PodcastAddict_20261028_164753.backup"}
            {"Action":"sync-plan","SelectedCount":2,"Plan":{"Keep":["a"],"Add":["b"],"Replace":[],"Remove":[],"Unknown":[]}}
            After output
            """;
        var plan = SyncOutput.ParsePlan("[Voortgang] Controleren: ge\u00efrriteerd {titel met \"quotes\"\n" + output);
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

        var command = System.Text.Encoding.Unicode.GetString(Convert.FromBase64String(
            PowerShellCommand.Encode(@"C:\Marco's app\sync.ps1", ["-PhoneName", "Marco's telefoon; $(test)", "-BackupDirectory", @"C:\Priv\u00e9", "-Apply"])));
        if (!command.Contains("'Marco''s telefoon; $(test)'", StringComparison.Ordinal)
            || !command.Contains("'C:\\Marco''s app\\sync.ps1'", StringComparison.Ordinal))
            throw new Exception("PowerShell-argumenten zijn niet veilig geciteerd.");

        if (OperatingSystem.IsWindows()) CheckWindowsEncoding();

        Console.WriteLine("Desktop plan checks passed.");
    }

    private static void CheckWindowsEncoding()
    {
        var directory = Path.Combine(Path.GetTempPath(), "podcast-encoding-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var script = Path.Combine(directory, "encoding.ps1");
        try
        {
            File.WriteAllText(script, "param($PhoneName, $BackupDirectory)\nWrite-Progress -Activity 'Preparing modules' -Status 'Test'\nWrite-Output $PhoneName\nWrite-Output $BackupDirectory");
            const string phone = "Marco's telefoon; $(geen-opdracht) ge\u00efrriteerd";
            const string folder = "C:\\Priv\u00e9\\podcasts";
            var start = new System.Diagnostics.ProcessStartInfo("powershell.exe")
            {
                UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true,
                StandardOutputEncoding = new System.Text.UTF8Encoding(false),
                StandardErrorEncoding = new System.Text.UTF8Encoding(false)
            };
            foreach (var argument in new[] { "-NoProfile", "-OutputFormat", "Text", "-ExecutionPolicy", "RemoteSigned", "-EncodedCommand",
                PowerShellCommand.Encode(script, ["-PhoneName", phone, "-BackupDirectory", folder]) })
                start.ArgumentList.Add(argument);
            using var process = System.Diagnostics.Process.Start(start)!;
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0 || error.Length != 0 || output.TrimEnd() != phone + Environment.NewLine + folder)
                throw new Exception("Windows PowerShell UTF-8-rondreis mislukt: " + error);

            File.WriteAllText(script, "param($PhoneName, $BackupDirectory)\nthrow $PhoneName");
            using var failing = System.Diagnostics.Process.Start(start)!;
            var failureOutput = failing.StandardOutput.ReadToEnd();
            var failureError = failing.StandardError.ReadToEnd();
            failing.WaitForExit();
            if (failing.ExitCode == 0 || !failureError.Contains(phone, StringComparison.Ordinal)
                || failureError.Contains("CLIXML", StringComparison.Ordinal) || failureOutput.Length != 0)
                throw new Exception("Windows PowerShell-fout komt niet als leesbare tekst terug: " + failureError);
        }
        finally
        {
            File.Delete(script);
            Directory.Delete(directory);
        }
    }
}
