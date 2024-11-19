using Hazel;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheBetterRoles.Managers;

namespace TheBetterRoles.RPCs;

[RegisterCustomRpc((uint)ReactorRPCs.DirtyRole)]
public class RpcDirtyRole(Main plugin, uint id) : PlayerCustomRpc<Main, RpcDirtyRole.Data>(plugin, id)
{
    public override SendOption SendOption => SendOption.Reliable;
    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public readonly struct Data(ushort roleHash, MessageReader? reader = null)
    {
        public readonly MessageReader? Reader = reader;
        public readonly ushort RoleHash = roleHash;
    }

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.RoleHash);
        CustomRoleManager.GetActiveRoleFromPlayers(role => role.RoleHash == data.RoleHash)?.Serialize(writer);
    }

    public override Data Read(MessageReader reader)
    {
        var RoleHash = reader.ReadUInt16();
        return new Data(RoleHash, reader);
    }

    public override void Handle(PlayerControl player, Data data)
    {
        CustomRoleManager.GetActiveRoleFromPlayers(role => role.RoleHash == data.RoleHash)?.Deserialize(data.Reader);
    }
}
