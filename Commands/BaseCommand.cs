namespace TheBetterRoles.Commands;

public enum ArgumentType
{
    None,
    Player,
}

public abstract class TBRCommandArgument
{
    public abstract uint ArgumentNum { get; }
    public abstract ArgumentType Type { get; }
    public object? target;
    public abstract T TryGetTarget<T>(string arg);
}

public abstract class TBRCommand
{
    public virtual TBRCommandArgument[]? Arguments { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract void Run();
    public virtual bool CheckCommand() => true;
}
