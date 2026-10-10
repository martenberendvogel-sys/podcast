using PodcastSync.BackupInspector;

if (args.Length != 2 || args[0] != "inspect")
{
    Console.Error.WriteLine("Gebruik: podcast-sync inspect <back-upbestand>");
    return 2;
}

try
{
    var report = await BackupInspector.InspectAsync(args[1]);
    Console.WriteLine(ReportWriter.ToJson(report));
    return 0;
}
catch (Exception exception) when (exception is IOException or InvalidDataException or ArgumentException)
{
    Console.Error.WriteLine($"Inspectie mislukt: {exception.Message}");
    return 1;
}

