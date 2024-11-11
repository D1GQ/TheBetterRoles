namespace TheBetterRoles.Commands;

public class NameArgument(BaseCommand? command) : BaseArgument(command)
{
    public override ArgumentType Type => ArgumentType.Name;
    public override string Suggestion => "{Name}";
    public override T? TryGetTarget<T>() where T : class
    {
        return null;
    }
}