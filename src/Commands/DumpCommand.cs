using TheBetterRoles.Data;
using TheBetterRoles.Items.Attributes;
using TheBetterRoles.Modules;

namespace TheBetterRoles.Commands;

[RegisterCommand]
internal sealed class DumpCommand : BaseCommand
{
    internal override CommandType Type => CommandType.Normal;
    protected override string CommandName => "Dump";
    internal override uint ShortNamesAmount => 0;
    internal override bool ShowCommand() => !GameState.IsInGamePlay || GameState.IsDev;

    internal override void Run()
    {
        if (GameState.IsInGamePlay && !GameState.IsDev) return;

        string logFilePath = Path.Combine(TBRDataManager.filePathFolder, "betterrole-log.txt");
        string log = File.ReadAllText(logFilePath);
        string newLog = string.Empty;
        string[] logArray = log.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        foreach (string text in logArray)
        {
            if (text.Contains("[PrivateLog]"))
            {
                newLog += text.Split(':')[0] + ":" + text.Split(':')[1].Replace("[PrivateLog]", "") + ": " + Encryptor.Decrypt(text.Split(':')[2][1..]) + "\n";
            }
            else
            {
                newLog += text + "\n";
            }
        }

        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string logFolderPath = Path.Combine(desktopPath, "BetterRoleLogs");
        if (!Directory.Exists(logFolderPath))
        {
            Directory.CreateDirectory(logFolderPath);
        }
        string logFileName = "log-" + Main.GetVersionText().Replace(' ', '-').ToLower() + "-" + DateTime.Now.ToString("yyyy.MM.dd-HH.mm.ss") + ".log";
        string newLogFilePath = Path.Combine(logFolderPath, logFileName);
        File.WriteAllText(newLogFilePath, newLog);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
        {
            FileName = logFolderPath,
            UseShellExecute = true,
            Verb = "open"
        });
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
        {
            FileName = newLogFilePath,
            UseShellExecute = true,
            Verb = "open"
        });

        CommandResultText($"Dump logs at <color=#b1b1b1>'{newLogFilePath}'</color>");
    }
}
