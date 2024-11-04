
using BepInEx;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;

namespace TheBetterRoles;

class Logger
{
    public static void Log(string info, string tag = "Log", bool logConsole = true, ConsoleColor color = ConsoleColor.White, bool hostOnly = false)
    {
        if (hostOnly && !GameState.IsHost) return;

        string mark = $"{DateTime.Now:HH:mm} [BetterRoleLog][{tag}]";
        string logFilePath = Path.Combine(BetterDataManager.filePathFolder, "betterrole-log.txt");
        string newLine = $"{mark}: {Utils.RemoveHtmlText(info)}";
        File.AppendAllText(logFilePath, newLine + Environment.NewLine);
        Main.Logger.LogInfo($"[{tag}] {info}");
        ConsoleManager.SetConsoleColor(color);
        if (logConsole) ConsoleManager.ConsoleStream.WriteLine($"{DateTime.Now:HH:mm} TheBetterRoles[{tag}]: {Utils.RemoveHtmlText(info)}");
    }

    public static void LogMethod(
        string info = "",
        Type? runtimeType = null,
        bool hostOnly = false,
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0,
        [CallerMemberName] string callerMemberName = "")
    {
        var loggedMethodFrame = new StackFrame(1, true);

        var loggedMethod = loggedMethodFrame.GetMethod();
        string loggedMethodName = loggedMethod.Name;
        string? loggedClassFullName = runtimeType?.FullName ?? loggedMethod.DeclaringType?.FullName;
        string? loggedClassName = runtimeType?.Name ?? loggedMethod.DeclaringType?.Name;

        string logMessage = string.IsNullOrEmpty(info)
            ? $"{loggedClassFullName}.{loggedMethodName} was called from {Path.GetFileName(callerFilePath)}({callerLineNumber}) in {callerMemberName}."
            : $"{loggedClassFullName}.{loggedMethodName} was called from {Path.GetFileName(callerFilePath)}({callerLineNumber}) in {callerMemberName}. Info: {info}.";

        Log(logMessage, "MethodLog", hostOnly);
    }

    public static void LogMethodPrivate(
    string info = "",
    Type? runtimeType = null,
    bool hostOnly = false,
    [CallerFilePath] string callerFilePath = "",
    [CallerLineNumber] int callerLineNumber = 0,
    [CallerMemberName] string callerMemberName = "")
    {
        var loggedMethodFrame = new StackFrame(1, true);

        var loggedMethod = loggedMethodFrame.GetMethod();
        string loggedMethodName = loggedMethod.Name;
        string? loggedClassFullName = runtimeType?.FullName ?? loggedMethod.DeclaringType?.FullName;
        string? loggedClassName = runtimeType?.Name ?? loggedMethod.DeclaringType?.Name;

        string logMessage = string.IsNullOrEmpty(info)
            ? $"{loggedClassFullName}.{loggedMethodName} was called from {Path.GetFileName(callerFilePath)}({callerLineNumber}) in {callerMemberName}."
            : $"{loggedClassFullName}.{loggedMethodName} was called from {Path.GetFileName(callerFilePath)}({callerLineNumber}) in {callerMemberName}. Info: {info}.";

        LogPrivate(logMessage, "MethodLog", hostOnly);
    }

    public static void LogHeader(string info, string tag = "LogHeader", bool hostOnly = false) => Log($"   >-------------- {info} --------------<", tag, hostOnly: hostOnly);
    public static void LogCheat(string info, string tag = "AntiCheat", bool hostOnly = false) => Log(info, tag, color: ConsoleColor.Cyan, hostOnly: hostOnly);
    public static void Error(string info, string tag = "Error", bool hostOnly = false) => Log(info, tag, color: ConsoleColor.Red, hostOnly: hostOnly);
    public static void Error(Exception ex, string tag = "Error", bool hostOnly = false) => Log(ex.ToString(), tag, color: ConsoleColor.Red, hostOnly: hostOnly);
    public static void Warning(string info, string tag = "Warning", bool hostOnly = false) => Log(info, tag, color: ConsoleColor.Yellow, hostOnly: hostOnly);
    public static void Test()
    {
        Log("------------------> TEST <------------------", "TEST");
        InGame("TEST");
    }
    // Log in game join msg
    public static void InGame(string info, bool hostOnly = false)
    {
        if (hostOnly && !GameState.IsHost) return;

        if (DestroyableSingleton<HudManager>._instance) DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage(info);
        Log(info, "InGame", hostOnly: hostOnly);
    }

    // ------------- Private Log -------------
    // Logs that can only be accessed when dumped

    public static void LogPrivate(string info, string tag = "Log", bool hostOnly = false)
    {
        if (hostOnly && !GameState.IsHost) return;

#if DEBUG
        if (GameState.IsDev)
        {
            Log(info, tag, hostOnly: hostOnly);
            return;
        }
#endif

        string mark = $"{DateTime.Now:HH:mm} [BetterRoleLog][PrivateLog][{tag}]";
        string logFilePath = Path.Combine(BetterDataManager.filePathFolder, "betterrole-log.txt");
        string newLine = $"{mark}: " + Encryptor.Encrypt($"{info}");
        File.AppendAllText(logFilePath, newLine + Environment.NewLine);
    }
}

