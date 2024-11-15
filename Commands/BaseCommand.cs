using System.Reflection;
using TheBetterRoles.Modules;

namespace TheBetterRoles.Commands;

public enum CommandType
{
    Normal,
    Sponsor,
    Debug,
}

public abstract class BaseArgument(BaseCommand? command)
{
    public BaseCommand? Command { get; } = command;
    public abstract string Suggestion { get; }
    public string Arg { get; set; } = string.Empty;
    public abstract T? TryGetTarget<T>() where T : class;
}

public abstract class BaseCommand
{
    public static readonly BaseCommand?[] allCommands = GetAllCustomRoleInstances();

    public static BaseCommand?[] GetAllCustomRoleInstances() => Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(t => t.IsSubclassOf(typeof(BaseCommand)) && !t.IsAbstract)
        .Select(t => (BaseCommand)Activator.CreateInstance(t))
        .ToArray();

    public virtual CommandType Type => CommandType.Normal;
    public string[] Names => ShortNames.Concat(new[] { Name }).ToArray();
    public abstract string Name { get; }
    public virtual string[] ShortNames => [];
    public abstract string Description { get; }
    public virtual BaseArgument[]? Arguments { get; } = [];
    public virtual bool SetChatTimer() => false;
    public virtual bool ShowCommand() => true;
    public virtual bool ShowSuggestion() => true;
    public abstract void Run();

    public static void CommandResultText(string text)
    {
        Utils.AddChatPrivate(text);
    }

    public static void CommandErrorText(string error)
    {
        string er = "<color=#f50000><size=150%><b>Error:</b></size></color>";
        Utils.AddChatPrivate($"<color=#730000>{er}\n{error}");
    }
}
