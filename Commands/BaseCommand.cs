using TheBetterRoles.Modules;

namespace TheBetterRoles.Commands;

public enum ArgumentType
{
    None,
    Player,
}

public enum CommandType
{
    Normal,
    Sponsor,
    Debug,
}

public abstract class BaseArgument (BaseCommand? command)
{
    public BaseCommand? Command { get; } = command;
    public abstract string Suggestion { get; }
    public abstract ArgumentType Type { get; }
    public string Arg { protected get; set; } = string.Empty;
    public object? Target { get; private set; }
    public abstract T? TryGetTarget<T>() where T : class;
}

public abstract class BaseCommand
{
    public virtual CommandType Type => CommandType.Normal;
    public abstract string Name { get; }
    public abstract string Description { get; }
    public virtual BaseArgument[]? Arguments { get; } = [];
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
