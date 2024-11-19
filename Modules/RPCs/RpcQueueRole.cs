using Hazel;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheBetterRoles.Commands;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;

namespace TheBetterRoles.RPCs;

[RegisterCustomRpc((uint)ReactorRPCs.QueueRole)]
public class RpcQueueRole(Main plugin, uint id) : PlayerCustomRpc<Main, RpcQueueRole.Data>(plugin, id)
{
    public override SendOption SendOption => SendOption.Reliable;
    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public readonly struct Data(CustomRoles role)
    {
        public readonly CustomRoles Role = role;
    }

    public override void Write(MessageWriter writer, Data data)
    {
        writer.WritePacked((int)data.Role);
    }

    public override Data Read(MessageReader reader)
    {
        return new Data((CustomRoles)reader.ReadPackedInt32());
    }

    public override void Handle(PlayerControl player, Data data)
    {
        SetRoleCommand.RequestQueueRole(player, data.Role);
    }
}
