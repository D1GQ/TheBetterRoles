using Hazel;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;

namespace TheBetterRoles.RPCs
{
    [RegisterCustomRpc((uint)ReactorRPCs.EndGame)]
    public class RpcEndGame : PlayerCustomRpc<Main, RpcEndGame.Data>
    {
        public override SendOption SendOption => SendOption.Reliable;
        public override RpcLocalHandling LocalHandling => RpcLocalHandling.After;
        public RpcEndGame(Main plugin, uint id) : base(plugin, id)
        {
        }

        public struct Data(List<byte> winners, EndGameReason reason, CustomRoleTeam team)
        {
            public List<byte> Winners = winners;
            public readonly EndGameReason Reason = reason;
            public readonly CustomRoleTeam Team = team;
        }

        public override void Write(MessageWriter writer, Data data)
        {
            writer.Write((byte)data.Reason);
            writer.Write((byte)data.Team);
            writer.Write((byte)data.Winners.Count);
            foreach (byte id in data.Winners)
            {
                writer.Write(id);
            }
        }

        public override Data Read(MessageReader reader)
        {
            var reason = (EndGameReason)reader.ReadByte();
            var team = (CustomRoleTeam)reader.ReadByte();
            int count = reader.ReadByte();

            List<byte> winners = [];
            for (int i = 0; i < count; i++)
            {
                winners.Add(reader.ReadByte());
            }

            return new Data(winners, reason, team);
        }

        public override void Handle(PlayerControl player, Data data)
        {
            if (player.IsHost())
            {
                CustomGameManager.EndGame(data.Winners, data.Reason, data.Team);
            }
        }
    }
}
