namespace TheBetterRoles.Commands;

internal class BoolArgument(BaseCommand? command, string argInfoTranStr, (string prefix, string postfix) prefix_postfix) : BaseArgument(command, argInfoTranStr, prefix_postfix)
{
    protected override string[] ArgSuggestions => ["true", "false"];
    internal bool? GetBool()
    {
        if (Arg.ToLower() is "true")
        {
            return true;
        }
        else if (Arg.ToLower() is "false" or "")
        {
            return false;
        }
        else
        {
            BaseCommand.CommandErrorText($"Invalid Syntax!");
        }

        return null;
    }
}