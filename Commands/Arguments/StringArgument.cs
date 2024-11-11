namespace TheBetterRoles.Commands;

public class StringArgument(BaseCommand? command) : BaseArgument(command)
{
    public override string Suggestion => suggestion;
    public string suggestion = "{String}";
    public override T? TryGetTarget<T>() where T : class
    {
        return null;
    }
}