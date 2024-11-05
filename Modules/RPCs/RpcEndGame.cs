using Hazel;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheBetterRoles.Helpers;
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
            // Pack Reason and Team into a single byte
            byte reasonAndTeam = (byte)((((byte)data.Reason & 0x0F) << 4) | ((byte)data.Team & 0x0F));
            writer.Write(reasonAndTeam);

            // Write the count of winners
            writer.Write((byte)data.Winners.Count);

            // Pack each pair of winner IDs into a single byte
            for (int i = 0; i < data.Winners.Count; i += 2)
            {
                byte packedIds;
                if (i + 1 < data.Winners.Count)
                {
                    // Combine two IDs into one byte if there's a pair
                    packedIds = (byte)((data.Winners[i] & 0x0F) << 4 | (data.Winners[i + 1] & 0x0F));
                }
                else
                {
                    // If there's an odd number, pack only one ID
                    packedIds = (byte)((data.Winners[i] & 0x0F) << 4);
                }
                writer.Write(packedIds);
            }
        }

        public override Data Read(MessageReader reader)
        {
            // Unpack Reason and Team from the single byte
            byte reasonAndTeam = reader.ReadByte();
            var reason = (EndGameReason)((reasonAndTeam >> 4) & 0x0F);
            var team = (CustomRoleTeam)(reasonAndTeam & 0x0F);

            int count = reader.ReadByte();

            List<byte> winners = new List<byte>();
            for (int i = 0; i < (count + 1) / 2; i++)
            {
                // Read each packed byte
                byte packedIds = reader.ReadByte();

                // Unpack two 4-bit IDs from each byte
                winners.Add((byte)((packedIds >> 4) & 0x0F));
                if (winners.Count < count)
                {
                    winners.Add((byte)(packedIds & 0x0F));
                }
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
