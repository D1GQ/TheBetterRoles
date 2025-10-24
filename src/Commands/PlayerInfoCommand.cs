using System.Text;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Attributes;
using TheBetterRoles.Modules;

namespace TheBetterRoles.Commands;

[RegisterCommand]
internal sealed class PlayerInfoCommand : BaseCommand
{
    internal override CommandType Type => CommandType.Normal;
    protected override string CommandName => "PlayerInfo";
    internal override uint ShortNamesAmount => 0;

    private PlayerArgument? PlayerArgument => (PlayerArgument)Arguments[0];
    internal PlayerInfoCommand()
    {
        Arguments = [new PlayerArgument(this, "Command.Arg.Player", ("{", "}"))];
    }

    internal override void Run()
    {
        var player = PlayerArgument.TryGetTarget();
        if (player != null)
        {
            StringBuilder sb = new();
            var hexColor = Colors.Color32ToHex(Palette.PlayerColors[player.CurrentOutfit.ColorId]);
            var format1 = "┌ •";
            var format2 = "├ •";
            var format3 = "└ •";
            sb.Append($"<size=150%><color={hexColor}><b>{player?.Data?.PlayerName}</color> Info:</b></size>\n");
            sb.Append($"{format1} <color=#c1c1c1>ID: {player?.Data?.PlayerId}</color>\n");
            sb.Append($"{format2} <color=#c1c1c1>HashPUID: {Utils.GetHashStr($"{player?.Data?.Puid}")}</color>\n");
            sb.Append($"{format2} <color=#c1c1c1>Platform: {Utils.GetPlatformName(player)}</color>\n");
            sb.Append($"{format3} <color=#c1c1c1>FriendCode: {player?.Data?.FriendCode}</color>");
            CommandResultText(sb.ToString());
        }
    }
}
