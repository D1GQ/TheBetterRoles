using System.Text;
using TheBetterRoles.Modules;

namespace TheBetterRoles.Commands;

public class PlayersInfoCommand : BaseCommand
{
    public override string Name => "players";
    public override string Description => "Get all Player information";

    public override void Run()
    {
        StringBuilder sb = new();
        foreach (PlayerControl player in Main.AllPlayerControls.Where(player => !player.isDummy))
        {
            var hexColor = Utils.Color32ToHex(Palette.PlayerColors[player.CurrentOutfit.ColorId]);
            sb.Append($"<color={hexColor}><b>{player?.Data?.PlayerName}</color> Info:</b>\n");
            sb.Append($"<color=#c1c1c1>{player?.Data?.PlayerId}</color> - ");
            sb.Append($"<color=#c1c1c1>{Utils.GetHashPuid($"{player?.Data?.Puid}")}</color> - ");
            sb.Append($"<color=#c1c1c1>{Utils.GetPlatformName(player)}</color> - ");
            sb.Append($"<color=#c1c1c1>{player?.Data?.FriendCode}</color>");
            sb.Append("\n\n");
        }
        CommandResultText(sb.ToString());
    }
}
