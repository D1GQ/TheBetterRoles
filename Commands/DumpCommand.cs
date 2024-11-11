using System.Text;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;

namespace TheBetterRoles.Commands;

public class DumpCommand : BaseCommand
{
    public override string Name => "dump";
    public override string Description => "Dump the entire log to the user's desktop";
    public override bool ShowCommand() => !GameState.IsInGamePlay || GameState.IsDev;

    public override void Run()
    {
        if (GameState.IsInGamePlay && !GameState.IsDev) return;

        string logFilePath = Path.Combine(BetterDataManager.filePathFolder, "betterrole-log.txt");
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
