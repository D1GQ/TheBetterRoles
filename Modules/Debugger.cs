
using BepInEx;

namespace TheBetterRoles;

class Logger
{
    public static void Log(string info, string tag = "Log", bool logConsole = true)
    {
        string mark = $"{DateTime.Now:HH:mm} [BetterRoleLog][{tag}]";
        string logFilePath = Path.Combine(BetterDataManager.filePathFolder, "betterrole-log.txt");
        string newLine = $"{mark}: {Utils.RemoveHtmlText(info)}";
        File.AppendAllText(logFilePath, newLine + Environment.NewLine);
        Main.Logger.LogInfo($"[{tag}] {info}");
        if (logConsole) ConsoleManager.ConsoleStream.WriteLine($"{DateTime.Now:HH:mm} TheBetterRoles[{tag}]: {Utils.RemoveHtmlText(info)}");
    }
    public static void LogHeader(string info, string tag = "LogHeader") => Log($"   >-------------- {info} --------------<", tag);
    public static void LogCheat(string info, string tag = "AntiCheat") => Log(info, tag);
    public static void Error(string info, string tag = "Error") => Log(info, tag);
    public static void Error(Exception ex, string tag = "Error") => Log(ex.ToString(), tag);
    public static void Test()
    {
        Log("------------------> TEST <------------------", "TEST");
        InGame("TEST");
    }
    // Log in game join msg
    public static void InGame(string info)
    {
        if (DestroyableSingleton<HudManager>._instance) DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage(info);
        Log(info, "InGame");
    }

    // ------------- Private Log -------------
    // Logs that can only be accessed when dumped

    public static void LogPrivate(string info, string tag = "Log")
    {
#if DEBUG
        if (GameStates.IsDev)
        {
            Log(info, tag);
            return;
        }
#endif

        string mark = $"{DateTime.Now:HH:mm} [BetterRoleLog][PrivateLog][{tag}]";
        string logFilePath = Path.Combine(BetterDataManager.filePathFolder, "betterrole-log.txt");
        string newLine = $"{mark}: " + Encryptor.Encrypt($"{info}");
        File.AppendAllText(logFilePath, newLine + Environment.NewLine);
    }
}

