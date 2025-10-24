using TheBetterRoles.Data;
using TheBetterRoles.Items.Attributes;
using TheBetterRoles.Modules;
using TheBetterRoles.Network.Configs;
using TheBetterRoles.Network.RPCs;

namespace TheBetterRoles.Commands;

[RegisterCommand]
internal sealed class ReviveCommand : BaseCommand
{
    internal override CommandType Type => CommandType.Debug;
    protected override string CommandName => "Revive";
    internal override uint ShortNamesAmount => 0;

    private PlayerArgument? PlayerArgument => (PlayerArgument)Arguments[0];
    internal ReviveCommand()
    {
        Arguments = [new PlayerArgument(this, "Command.Arg.Player", ("[", "]"))];
    }

    internal override bool ShowCommand() => (GameState.IsFreePlay || TBRGameSettings.Debugging.GetBool()) && Main.MyData.HasAll() && Main.MyData.IsVerified();

    internal override void Run()
    {
        var player = PlayerArgument.TryGetTarget(false) ?? PlayerControl.LocalPlayer;
        player.SendRpcRevive();
    }
}
