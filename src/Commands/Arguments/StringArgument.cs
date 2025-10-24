namespace TheBetterRoles.Commands;

internal class StringArgument(BaseCommand? command, string argInfoTranStr, (string prefix, string postfix) prefix_postfix) : BaseArgument(command, argInfoTranStr, prefix_postfix)
{
}