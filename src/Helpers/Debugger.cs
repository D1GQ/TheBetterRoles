
using BepInEx;
using BepInEx.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TheBetterRoles.Data;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;

namespace TheBetterRoles;

internal class Logger
{
    internal static void Log(string info, string tag = "Log", bool logConsole = true, ConsoleColor color = ConsoleColor.White, bool hostOnly = false)
    {
        try
        {
            if (hostOnly && !GameState.IsHost) return;

            string mark = $"{DateTime.Now:HH:mm} [BetterRoleLog][{tag}]";
            string newLine = $"{mark}: {Utils.RemoveHtmlText(info)}";
            File.AppendAllText(TBRDataManager.LogFile, newLine + Environment.NewLine);
            Main.Logger.LogInfo($"[{tag}] {info}");
            if (logConsole)
            {
                ConsoleManager.SetConsoleColor(color);
                ConsoleManager.ConsoleStream.WriteLine($"{DateTime.Now:HH:mm} TheBetterRoles[{tag}]: {Utils.RemoveHtmlText(info)}");
            }
        }
        catch { }
    }

    internal static void LogMethod(
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

    internal static void LogMethodPrivate(
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

    internal static void LogHeader(string info, string tag = "LogHeader", bool hostOnly = false, bool logConsole = true) => Log($"   >-------------- {info} --------------<", tag, hostOnly: hostOnly, logConsole: logConsole);
    internal static void LogCheat(string info, string tag = "AntiCheat", bool hostOnly = false, bool logConsole = true) => Log(info, tag, color: ConsoleColor.Cyan, hostOnly: hostOnly, logConsole: logConsole);
    internal static void Error(string info, string tag = "Error", bool hostOnly = false, bool logConsole = true) => Log(info, tag, color: ConsoleColor.Red, hostOnly: hostOnly, logConsole: logConsole);
    internal static void Error(Exception ex, string tag = "Error", bool hostOnly = false, bool logConsole = true) => Log(ex.ToString(), tag, color: ConsoleColor.Red, hostOnly: hostOnly, logConsole: logConsole);
    internal static void Warning(string info, string tag = "Warning", bool hostOnly = false, bool logConsole = true) => Log(info, tag, color: ConsoleColor.Yellow, hostOnly: hostOnly, logConsole: logConsole);
    internal static void Test()
    {
        Log("------------------> TEST <------------------", "TEST");
        InGame("TEST");
    }
    // Log in game join msg
    internal static void InGame(string info, bool hostOnly = false)
    {
        if (hostOnly && !GameState.IsHost) return;

        if (HudManager.InstanceExists) HudManager.Instance.Notifier.AddDisconnectMessage(info);
        Log(info, "InGame", hostOnly: hostOnly);
    }

    // ------------- Private Log -------------
    // Logs that can only be accessed when dumped

    internal static void LogPrivate(string info, string tag = "Log", bool hostOnly = false)
    {
        try
        {
            if (hostOnly && !GameState.IsHost) return;

#if DEBUG || DEBUG_MULTIACCOUNTS
            if (GameState.IsDev)
            {
                Log(info, tag, hostOnly: hostOnly);
                return;
            }
#endif

            string mark = $"{DateTime.Now:HH:mm} [BetterRoleLog][PrivateLog][{tag}]";
            string newLine = $"{mark}: " + Encryptor.Encrypt($"{info}");
            File.AppendAllText(TBRDataManager.LogFile, newLine + Environment.NewLine);
        }
        catch { }
    }
}

internal class NetworkLogger
{
    internal static void Log(string info, string tag = "Log")
    {
        try
        {
            string mark = $"{DateTime.Now:HH:mm} [Network][{tag}]";
            string newLine = $"{mark}: {Utils.RemoveHtmlText(info)}";
            File.AppendAllText(TBRDataManager.NetworkLogFile, newLine + Environment.NewLine);
        }
        catch { }
    }
    internal static void LogSend(string info, string tag = "Send") => Log(info, tag);
    internal static void LogReceive(string info, string tag = "Receive") => Log(info, tag);
    internal static void Error(string info, string tag = "Error") => Log(info, tag);
    internal static void Error(Exception ex, string tag = "Error") => Log(ex.ToString(), tag);
    internal static void Warning(string info, string tag = "Warning") => Log(info, tag);
}

internal class CustomLogListener : ILogListener
{
    public LogLevel LogLevelFilter { get; set; } = LogLevel.Info | LogLevel.Warning | LogLevel.Error;
    public void LogEvent(object sender, LogEventArgs eventArgs)
    {
        if (eventArgs.Source.SourceName.ToLower().Contains("unity")
            || eventArgs.Source.SourceName.ToLower().Contains("thebetterroles")) return;

        if (eventArgs.Level is LogLevel.None or LogLevel.Info)
        {
            Logger.Log(eventArgs.Data.ToString(), "BepInEx." + eventArgs.Source.SourceName, logConsole: false);
        }
        else if (eventArgs.Level is LogLevel.Warning)
        {
            Logger.Warning(eventArgs.Data.ToString(), "BepInEx." + eventArgs.Source.SourceName, logConsole: false);
        }
        else if (eventArgs.Level is LogLevel.Error or LogLevel.Fatal)
        {
            Logger.Error(eventArgs.Data.ToString(), "BepInEx." + eventArgs.Source.SourceName, logConsole: false);
        }
    }

    public void Dispose()
    {
    }

    public void Flush()
    {
    }
}

