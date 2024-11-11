using System.Text;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;

namespace TheBetterRoles.Commands;

public class SetPrefixCommand : BaseCommand
{
    public override string Name => "setprefix";
    public override string Description => "Set command prefix";

    public SetPrefixCommand()
    {
        _arguments = new Lazy<BaseArgument[]>(() => new BaseArgument[]
        {
            new StringArgument(this),
        });
        prefixArgument.suggestion = "{Prefix}";
    }
    private readonly Lazy<BaseArgument[]> _arguments;
    public override BaseArgument[]? Arguments => _arguments.Value;

    private StringArgument? prefixArgument => (StringArgument)Arguments[0];

    public override void Run()
    {
        var oldPrefix = Main.CommandPrefix.Value;
        var prefix = prefixArgument.Arg.ToCharArray().First().ToString();
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
