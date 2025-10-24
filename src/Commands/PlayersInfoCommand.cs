using System.Text;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Attributes;
using TheBetterRoles.Modules;

namespace TheBetterRoles.Commands;

[RegisterCommand]
internal sealed class PlayersInfoCommand : BaseCommand
{
    internal override CommandType Type => CommandType.Normal;
    protected override string CommandName => "PlayersInfo";
    internal override uint ShortNamesAmount => 0;

    internal override void Run()
    {
        StringBuilder sb = new();
        foreach (PlayerControl player in Main.AllPlayerControls.Where(player => !player.isDummy))
        {
            var hexColor = Colors.Color32ToHex(Palette.PlayerColors[player.CurrentOutfit.ColorId]);
            sb.Append($"<color={hexColor}><b>{player?.Data?.PlayerName}</color> Info:</b>\n");
            sb.Append($"<color=#c1c1c1>{player?.Data?.PlayerId}</color> - ");
            sb.Append($"<color=#c1c1c1>{Utils.GetHashStr($"{player?.Data?.Puid}")}</color> - ");
            sb.Append($"<color=#c1c1c1>{Utils.GetPlatformName(player)}</color> - ");
            sb.Append($"<color=#c1c1c1>{player?.Data?.FriendCode}</color>");
            sb.Append("\n\n");
        }
        CommandResultText(sb.ToString());
    }
}
