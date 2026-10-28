using System.Windows.Forms;

namespace PodcastSync.Desktop;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--self-test")
        {
            SyncOutputSelfTest.Run();
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new SyncForm());
    }
}
