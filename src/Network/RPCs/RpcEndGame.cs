using Hazel;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Managers;

namespace TheBetterRoles.Network.RPCs;

[RegisterCustomRpc((uint)ReactorRPCs.EndGame)]
internal class RpcEndGame(Main plugin, uint id) : PlayerCustomRpc<Main, RpcEndGame.Data>(plugin, id)
{
    public override SendOption SendOption => SendOption.Reliable;
    public override RpcLocalHandling LocalHandling => RpcLocalHandling.After;

    public struct Data(List<byte> winners, EndGameReason reason, RoleClassTeam team, bool doEnd = true)
    {
        public bool DoEnd = doEnd;
        public HashSet<byte> Winners = winners.ToHashSet();
        public readonly EndGameReason Reason = reason;
        public readonly RoleClassTeam Team = team;
    }

    public override void Write(MessageWriter writer, Data data)
    {
        byte reasonAndTeam = (byte)(((byte)data.Reason & 0x0F) << 4 | (byte)data.Team & 0x0F);
        writer.Write(CatchedGameData.Instance?.GameHasStarted == true && CatchedGameData.Instance?.GameHasEnded == false);
        writer.Write(reasonAndTeam);
        writer.WriteFast(data.Winners);
    }

    public override Data Read(MessageReader reader)
    {
        byte reasonAndTeam = reader.ReadByte();
        var doEnd = reader.ReadBoolean();
        var reason = (EndGameReason)(reasonAndTeam >> 4 & 0x0F);
        var team = (RoleClassTeam)(reasonAndTeam & 0x0F);
        var winners = reader.ReadFast<List<byte>>();

        return new Data(winners, reason, team, doEnd);
    }

    public override void Handle(PlayerControl player, Data data)
    {
        if (!data.DoEnd) return;
        if (player.IsHost())
        {
            CustomGameManager.EndGame(data.Winners, data.Reason, data.Team);
        }
    }
}
