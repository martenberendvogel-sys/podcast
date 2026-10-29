using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace PodcastSync.Desktop;

internal sealed class SyncForm : Form
{
    private readonly TextBox _phone = new() { Dock = DockStyle.Fill };
    private readonly TextBox _directory = new() { Dock = DockStyle.Fill };
    private readonly TextBox _log = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Both,
        WordWrap = false,
        Font = new System.Drawing.Font("Consolas", 9)
    };
    private readonly Button _planButton = new() { Text = "1. Plan controleren", AutoSize = true };
    private readonly Button _syncButton = new() { Text = "2. Synchroniseer", AutoSize = true, Enabled = false };
    private readonly Button _browseButton = new() { Text = "Bladeren…", AutoSize = true };
    private readonly Button _cleanupButton = new() { Text = "Opruimen…", AutoSize = true };
    private readonly Label _status = new() { Dock = DockStyle.Fill, AutoEllipsis = true, Text = "Maak eerst een nieuwe back-up in Podcast Addict; ontgrendel telefoon en sluit beide apparaten aan." };
    private readonly string _script;
    private readonly string _settingsPath;
    private SyncPlan? _approvedPlan;
    private bool _running;

    internal SyncForm()
    {
        Text = "Podcast Sync – proefversie";
        Width = 960;
        Height = 660;
        MinimumSize = new System.Drawing.Size(720, 480);
        StartPosition = FormStartPosition.CenterScreen;

        _script = FindScript();
        _settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PodcastSync", "desktop-settings.json");
        LoadSettings();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 3,
            RowCount = 5
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label { Text = "Telefoonnaam", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        layout.Controls.Add(_phone, 1, 0);
        layout.SetColumnSpan(_phone, 2);
        layout.Controls.Add(new Label { Text = "Back-upmap op pc", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        layout.Controls.Add(_directory, 1, 1);
        layout.Controls.Add(_browseButton, 2, 1);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        buttons.Controls.Add(_planButton);
        buttons.Controls.Add(_syncButton);
        buttons.Controls.Add(_cleanupButton);
        layout.Controls.Add(buttons, 0, 2);
        layout.SetColumnSpan(buttons, 3);
        layout.Controls.Add(_status, 0, 3);
        layout.SetColumnSpan(_status, 3);
        layout.Controls.Add(_log, 0, 4);
        layout.SetColumnSpan(_log, 3);
        Controls.Add(layout);

        _browseButton.Click += (_, _) => Browse();
        _phone.TextChanged += (_, _) => InvalidatePlan();
        _directory.TextChanged += (_, _) => InvalidatePlan();
        _planButton.Click += async (_, _) => await RunPlanAsync();
        _syncButton.Click += async (_, _) => await RunSyncAsync();
        _cleanupButton.Click += async (_, _) => await RunCleanupAsync();
        FormClosing += (_, e) =>
        {
            if (!_running) { return; }
            e.Cancel = true;
            MessageBox.Show(this, "Wacht tot de huidige opdracht klaar is. Trek de apparaten niet los tijdens synchronisatie.",
                "Podcast Sync", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
    }

    private async Task RunPlanAsync()
    {
        InvalidatePlan();
        if (string.IsNullOrWhiteSpace(_phone.Text) || !Directory.Exists(_directory.Text.Trim()))
        {
            MessageBox.Show(this, "Vul de exacte telefoonnaam uit Verkenner en een bestaande lokale back-upmap in.",
                "Podcast Sync", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _log.Clear();
        _status.Text = "Plan voorbereiden. Telefoon ontgrendeld en USB-bestandsoverdracht ingeschakeld?";
        try
        {
            SaveSettings();
            var (exitCode, output) = await RunScriptAsync("-PhoneName", _phone.Text.Trim(),
                "-BackupDirectory", _directory.Text.Trim());
            if (exitCode != 0) { throw new InvalidOperationException($"Voorbereiden stopte met code {exitCode}. Garmin is niet door dit plan gewijzigd."); }
            var plan = SyncOutput.ParsePlan(output);
            _status.Text = $"Plan: {plan.Selected} geselecteerd · {plan.Keep} behouden · {plan.Add} toevoegen · {plan.Replace} vervangen · {plan.Remove} verwijderen · {plan.Unknown} onbekend.";
            if (plan.Selected == 0 || plan.Selected != plan.Keep + plan.Add + plan.Replace)
            {
                Append("Synchroniseren is geblokkeerd: de playlist is leeg of het plan telt niet op. Bekijk de uitvoer.");
            }
            else if (plan.Unknown > 0)
            {
                Append("Synchroniseren is geblokkeerd: onbekende Garmin-bestanden of mappen. Bekijk de uitvoer.");
            }
            else if (!SyncOutput.IsFreshBackup(plan.BackupPath, DateTime.Now))
            {
                Append("Synchroniseren is geblokkeerd: maak een nieuwe volledige back-up (maximaal 48 uur oud).");
            }
            else
            {
                _approvedPlan = plan;
                _syncButton.Enabled = true;
                Append("Lees het plan hierboven. Klik alleen op Synchroniseer als toevoegen, vervangen en verwijderen kloppen.");
            }
        }
        catch (Exception ex)
        {
            _status.Text = "Plan mislukt; niets op de Garmin gewijzigd.";
            Append(ex.Message);
        }
    }

    private async Task RunCleanupAsync()
    {
        if (_running) return;
        InvalidatePlan();
        _running = true;
        _planButton.Enabled = _syncButton.Enabled = _cleanupButton.Enabled = _browseButton.Enabled = false;
        _phone.ReadOnly = _directory.ReadOnly = true;
        _status.Text = "Opruimoverzicht maken; oude Garmin-kopieen controleren.";
        _log.Clear();
        try
        {
            var root = Path.GetFullPath(_directory.Text.Trim());
            var lockDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PodcastSync");
            Directory.CreateDirectory(lockDirectory);
            using var syncLock = new FileStream(Path.Combine(lockDirectory, "sync.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            using var garminLock = new FileStream(Path.Combine(lockDirectory, "garmin.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            var plan = await Task.Run(() => GarminCleanup.Plan(root));
            Append("Bewaren: " + (plan.Keep is null ? "geen Garmin-klare set gevonden" : Path.GetFileName(plan.Keep)));
            Append("Back-ups, podcast-staging en losse bestanden blijven staan.");
            foreach (var skipped in plan.Skipped) Append("Overgeslagen: " + skipped);
            foreach (var candidate in plan.Candidates)
                Append($"Naar Prullenbak: {Path.GetFileName(candidate.Path)} ({candidate.Bytes / 1048576.0:F1} MB)");
            if (plan.Candidates.Length == 0)
            {
                _status.Text = "Geen gecontroleerde oude sets om op te ruimen.";
                return;
            }
            var total = plan.Candidates.Sum(item => item.Bytes) / 1048576.0;
            _status.Text = $"Opruimvoorstel: {plan.Candidates.Length} oude sets, {total:F1} MB.";
            if (MessageBox.Show(this,
                $"Deze mappen naar de Prullenbak verplaatsen?\n\n{string.Join(Environment.NewLine, plan.Candidates.Select(item => Path.GetFileName(item.Path)))}\n\nTotaal: {total:F1} MB. De nieuwste set, back-ups en stagingcache blijven staan.",
                "Opruimen bevestigen", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                _status.Text = "Opruimen geannuleerd; niets gewijzigd.";
                return;
            }
            foreach (var candidate in plan.Candidates)
            {
                _status.Text = "Naar Prullenbak: " + Path.GetFileName(candidate.Path);
                await Task.Run(() => GarminCleanup.Recycle(candidate));
                Append("Opgeruimd naar Prullenbak: " + Path.GetFileName(candidate.Path));
            }
            _status.Text = $"Klaar: {plan.Candidates.Length} oude sets naar de Prullenbak. Daar zijn ze terug te zetten.";
            Append(_status.Text);
        }
        catch (Exception ex)
        {
            _status.Text = "Opruimen gestopt; zie het log.";
            Append(ex.Message);
            Append("Reeds naar de Prullenbak verplaatste mappen zijn daar terug te zetten.");
        }
        finally
        {
            _running = false;
            _planButton.Enabled = _cleanupButton.Enabled = _browseButton.Enabled = true;
            _phone.ReadOnly = _directory.ReadOnly = false;
        }
    }

    private async Task RunSyncAsync()
    {
        var plan = _approvedPlan;
        if (plan is null) { return; }
        if (!SyncOutput.IsFreshBackup(plan.BackupPath, DateTime.Now))
        {
            InvalidatePlan();
            Append("Back-up is inmiddels te oud. Maak een nieuwe back-up en controleer het plan opnieuw.");
            return;
        }

        var answer = MessageBox.Show(this,
            $"Dit past het getoonde plan toe op Garmin ‘Recente podcasts’:\n\n{plan.Add} toevoegen, {plan.Replace} vervangen, {plan.Remove} verwijderen.\n\nBack-up: {Path.GetFileName(plan.BackupPath)}",
            "Synchroniseren bevestigen", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes) { return; }

        InvalidatePlan();
        _status.Text = "Synchroniseren; laat de Garmin aangesloten.";
        try
        {
            // Een vastgepinde kopie voorkomt dat een nieuwere telefoonback-up onopgemerkt wordt toegepast.
            var (exitCode, _) = await RunScriptAsync("-BackupPath", plan.BackupPath, "-Apply");
            if (exitCode != 0) { throw new InvalidOperationException($"Synchronisatie stopte met code {exitCode}. Bekijk het log en maak eerst opnieuw een plan."); }
            _status.Text = "Klaar. Controleer ‘Recente podcasts’ op het horloge.";
        }
        catch (Exception ex)
        {
            _status.Text = "Synchronisatie niet volledig afgerond; controleer het log en maak een nieuw plan.";
            Append(ex.Message);
        }
    }

    private async Task<(int ExitCode, string Output)> RunScriptAsync(params string[] arguments)
    {
        _running = true;
        _planButton.Enabled = false;
        _syncButton.Enabled = false;
        _browseButton.Enabled = false;
        _cleanupButton.Enabled = false;
        _phone.ReadOnly = true;
        _directory.ReadOnly = true;
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo("powershell.exe")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = new UTF8Encoding(false),
                StandardErrorEncoding = new UTF8Encoding(false)
            };
            foreach (var arg in new[] { "-NoProfile", "-OutputFormat", "Text", "-ExecutionPolicy", "RemoteSigned", "-EncodedCommand", PowerShellCommand.Encode(_script, arguments) })
            {
                process.StartInfo.ArgumentList.Add(arg);
            }
            if (!process.Start()) { throw new InvalidOperationException("PowerShell kon niet worden gestart."); }
            var output = new StringBuilder();
            var stdout = ReadLinesAsync(process.StandardOutput, output);
            var stderr = ReadLinesAsync(process.StandardError, null);
            await process.WaitForExitAsync();
            await Task.WhenAll(stdout, stderr);
            return (process.ExitCode, output.ToString());
        }
        finally
        {
            _running = false;
            _planButton.Enabled = true;
            _browseButton.Enabled = true;
            _cleanupButton.Enabled = true;
            _phone.ReadOnly = false;
            _directory.ReadOnly = false;
        }
    }

    private async Task ReadLinesAsync(StreamReader reader, StringBuilder? output)
    {
        while (await reader.ReadLineAsync() is { } line)
        {
            output?.AppendLine(line);
            Append(line);
        }
    }

    private void Append(string line)
    {
        if (IsDisposed) { return; }
        if (InvokeRequired)
        {
            try { BeginInvoke(new Action(() => Append(line))); }
            catch (InvalidOperationException) { /* Het venster is intussen gesloten. */ }
            return;
        }
        _log.AppendText(line + Environment.NewLine);
        if (line.StartsWith("[Voortgang] ", StringComparison.Ordinal))
        {
            _status.Text = line["[Voortgang] ".Length..];
        }
    }

    private void InvalidatePlan()
    {
        _approvedPlan = null;
        _syncButton.Enabled = false;
    }

    private void Browse()
    {
        using var dialog = new FolderBrowserDialog { Description = "Kies de lokale map voor privé-back-ups en MP3-cache" };
        if (Directory.Exists(_directory.Text)) { dialog.InitialDirectory = _directory.Text; }
        if (dialog.ShowDialog(this) == DialogResult.OK) { _directory.Text = dialog.SelectedPath; }
    }

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(_settingsPath)) { return; }
            using var json = JsonDocument.Parse(File.ReadAllText(_settingsPath));
            _phone.Text = json.RootElement.GetProperty("PhoneName").GetString() ?? "";
            _directory.Text = json.RootElement.GetProperty("BackupDirectory").GetString() ?? "";
        }
        catch (Exception ex) when (ex is IOException or JsonException or KeyNotFoundException or UnauthorizedAccessException)
        {
            _phone.Clear();
            _directory.Clear();
        }
    }

    private void SaveSettings()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        var json = JsonSerializer.Serialize(new { PhoneName = _phone.Text.Trim(), BackupDirectory = _directory.Text.Trim() });
        File.WriteAllText(_settingsPath, json);
    }

    private static string FindScript()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var path = Path.Combine(dir.FullName, "scripts", "Sync-Podcasts.ps1");
            if (File.Exists(path)) { return path; }
        }
        throw new FileNotFoundException("Sync-Podcasts.ps1 niet gevonden. Start de app vanuit de volledige podcast-repository.");
    }
}
