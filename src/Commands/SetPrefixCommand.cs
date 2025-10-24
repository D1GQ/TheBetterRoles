using TheBetterRoles.Items.Attributes;

namespace TheBetterRoles.Commands;

[RegisterCommand]
internal sealed class SetPrefixCommand : BaseCommand
{
    internal override CommandType Type => CommandType.Normal;
    protected override string CommandName => "SetPrefix";
    internal override uint ShortNamesAmount => 0;

    private StringArgument? PrefixArgument => (StringArgument)Arguments[0];
    internal SetPrefixCommand()
    {
        Arguments = [new StringArgument(this, "Command.Arg.Prefix", ("{", "}"))];
    }

    internal override void Run()
    {
        var oldPrefix = Main.CommandPrefix.Value;
        var prefix = PrefixArgument.Arg.ToCharArray().First().ToString();
        if (!string.IsNullOrEmpty(prefix))
        {
            Main.CommandPrefix.Value = prefix;
            CommandResultText($"Command prefix set from <#c1c100>{oldPrefix}</color> to <#c1c100>{prefix}</color>");
        }
        else
        {
            CommandErrorText("Invalid Syntax!");
        }
    }
}
