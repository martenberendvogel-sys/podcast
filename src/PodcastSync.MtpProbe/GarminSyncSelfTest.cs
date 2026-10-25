internal static class GarminSyncSelfTest
{
    internal static int Run()
    {
        var first = new string('a', 64);
        var second = new string('b', 64);
        var old = new string('c', 64);
        var firstHash = new string('d', 64);
        var secondHash = new string('e', 64);
        var manifest = new ReadyManifest(1, "Recente podcasts", "GarminPodcastDescendingTrack", [first, second],
            [new ReadyFile(1, 2, first, "P", "A", 1000, firstHash),
             new ReadyFile(2, 1, second, "P", "B", 2000, secondHash)]);
        var tests = new (DeviceState[] Device, int Keep, int Add, int Replace, int Remove, int Unknown)[]
        {
            ([new(first + ".mp3", 1000, firstHash), new(second + ".mp3", 2000, secondHash)], 2, 0, 0, 0, 0),
            ([new(first + ".mp3", 1000, firstHash)], 1, 1, 0, 0, 0),
            ([new(first + ".mp3", 999, firstHash), new(second + ".mp3", 2000, secondHash)], 1, 0, 1, 0, 0),
            ([new(first + ".mp3", 1000, firstHash), new(second + ".mp3", 2000, secondHash), new(old + ".mp3", 1, null)], 2, 0, 0, 1, 0),
            ([new(first + ".mp3", 1000, firstHash), new(second + ".mp3", 2000, secondHash), new("handmatig.mp3", 1, null)], 2, 0, 0, 0, 1)
        };
        foreach (var test in tests)
        {
            var plan = GarminSync.Compare(manifest, test.Device);
            if (plan.Keep.Length != test.Keep || plan.Add.Length != test.Add ||
                plan.Replace.Length != test.Replace || plan.Remove.Length != test.Remove ||
                plan.Unknown.Length != test.Unknown)
                throw new InvalidOperationException("Garmin-syncplan zelftest mislukt.");
        }
        Console.WriteLine($"{tests.Length} Garmin-syncplancontroles geslaagd.");
        return 0;
    }
}
