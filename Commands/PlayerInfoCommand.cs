using System.Text;
using TheBetterRoles.Modules;

namespace TheBetterRoles.Commands
{
    public class PlayerInfoCommand : BaseCommand
    {
        public override string Name => "player";
        public override string Description => "Get a Players information";

        public PlayerInfoCommand()
        {
            _arguments = new Lazy<BaseArgument[]>(() => new BaseArgument[]
            {
                new PlayerArgument(this),
            });
        }
        private readonly Lazy<BaseArgument[]> _arguments;
        public override BaseArgument[]? Arguments => _arguments.Value;

        private PlayerArgument? playerArgument => (PlayerArgument)Arguments[0];

        public override void Run()
        {
            var player = playerArgument.TryGetTarget<PlayerControl>();
            if (player != null)
            {
                StringBuilder sb = new();
                var hexColor = Utils.Color32ToHex(Palette.PlayerColors[player.CurrentOutfit.ColorId]);
                var format1 = "┌ •";
                var format2 = "├ •";
                var format3 = "└ •";
                sb.Append($"<size=150%><color={hexColor}><b>{player?.Data?.PlayerName}</color> Info:</b></size>\n");
                sb.Append($"{format1} <color=#c1c1c1>ID: {player?.Data?.PlayerId}</color>\n");
                sb.Append($"{format2} <color=#c1c1c1>HashPUID: {Utils.GetHashPuid($"{player?.Data?.Puid}")}</color>\n");
                sb.Append($"{format2} <color=#c1c1c1>Platform: {Utils.GetPlatformName(player)}</color>\n");
                sb.Append($"{format3} <color=#c1c1c1>FriendCode: {player?.Data?.FriendCode}</color>");
                CommandResultText(sb.ToString());
            }
        }
    }
}
