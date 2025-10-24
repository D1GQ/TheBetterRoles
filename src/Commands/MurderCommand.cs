using TheBetterRoles.Data;
using TheBetterRoles.Items.Attributes;
using TheBetterRoles.Modules;
using TheBetterRoles.Network.Configs;
using TheBetterRoles.Network.RPCs;

namespace TheBetterRoles.Commands;

[RegisterCommand]
internal sealed class MurderCommand : BaseCommand
{
    internal override CommandType Type => CommandType.Debug;
    protected override string CommandName => "Murder";
    internal override uint ShortNamesAmount => 0;

    private PlayerArgument? KillerArgument => (PlayerArgument)Arguments[0];
    private PlayerArgument? TargetArgument => (PlayerArgument)Arguments[1];
    internal MurderCommand()
    {
        Arguments = [new PlayerArgument(this, "Command.Arg.Killer", ("[", "]")), new PlayerArgument(this, "Command.Arg.Target", ("[", "]"))];
    }

    internal override bool ShowCommand() => (GameState.IsFreePlay || TBRGameSettings.Debugging.GetBool()) && Main.MyData.HasAll() && Main.MyData.IsVerified();

    internal override void Run()
    {
        PlayerControl killer = KillerArgument.TryGetTarget(false) ?? PlayerControl.LocalPlayer;
        PlayerControl target = TargetArgument.TryGetTarget(false) ?? killer;
        killer.SendRpcMurder(target, true);
    }
}
