using Hazel;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheBetterRoles.Commands;
using TheBetterRoles.Items.Enums;

namespace TheBetterRoles.Network.RPCs;

[RegisterCustomRpc((uint)ReactorRPCs.QueueRole)]
internal class RpcQueueRole(Main plugin, uint id) : PlayerCustomRpc<Main, RpcQueueRole.Data>(plugin, id)
{
    public override SendOption SendOption => SendOption.Reliable;
    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public readonly struct Data(RoleClassTypes role)
    {
        public readonly RoleClassTypes Role = role;
    }

    public override void Write(MessageWriter writer, Data data)
    {
        writer.WritePacked((int)data.Role);
    }

    public override Data Read(MessageReader reader)
    {
        return new Data((RoleClassTypes)reader.ReadPackedInt32());
    }

    public override void Handle(PlayerControl player, Data data)
    {
        UpCommand.RequestQueueRole(player, data.Role);
    }
}
