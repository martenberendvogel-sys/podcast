using System.Text;

namespace PodcastSync.Desktop;

internal static class PowerShellCommand
{
    internal static string Encode(string script, string[] arguments)
    {
        static string Quote(string value) => "'" + value.Replace("'", "''") + "'";
        var command = new StringBuilder("$ErrorActionPreference='Stop'; $OutputEncoding=[Console]::OutputEncoding=[Text.UTF8Encoding]::new($false); & ");
        command.Append(Quote(script));
        for (var index = 0; index < arguments.Length; index++)
        {
            var parameter = arguments[index];
            if (parameter == "-Apply") { command.Append(" -Apply"); continue; }
            if (parameter is not ("-PhoneName" or "-BackupDirectory" or "-BackupPath") || index + 1 >= arguments.Length)
                throw new ArgumentException("Onbekende syncparameter.");
            command.Append(' ').Append(parameter).Append(' ').Append(Quote(arguments[++index]));
        }
        command.Append("; if (-not $?) { exit 1 }");
        return Convert.ToBase64String(Encoding.Unicode.GetBytes(command.ToString()));
    }
}
