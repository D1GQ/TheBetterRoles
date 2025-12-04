using Hazel;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Managers;

namespace TheBetterRoles.Network.RPCs;

[RegisterCustomRpc((uint)ReactorRPCs.SyncRole)]
internal class RpcSyncRole(Main plugin, uint id) : PlayerCustomRpc<Main, RpcSyncRole.Data>(plugin, id)
{
    public override SendOption SendOption => SendOption.Reliable;
    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public readonly struct Data(int syncId, ushort roleHash, object[] writerParams = null, MessageReader? reader = null)
    {
        public readonly MessageReader? Reader = reader;
        public readonly object[]? WriterParams = writerParams;

        public readonly int SyncId = syncId;
        public readonly ushort RoleHash = roleHash;
    }

    public override void Write(MessageWriter writer, Data data)
    {
        try
        {
            writer.WritePacked(data.SyncId);
            writer.Write(data.RoleHash);
            foreach (var value in data.WriterParams)
            {
                writer.WriteFast(value);
            }
        }
        catch (Exception ex)
        {
            Logger.Error("OnSendRoleSync: " + ex);
        }
    }

    public override Data Read(MessageReader reader)
    {
        var SyncId = reader.ReadPackedInt32();
        var RoleHash = reader.ReadUInt16();

        return new Data(SyncId, RoleHash, null, reader);
    }

    public override void Handle(PlayerControl player, Data data)
    {
        try
        {
            var role = CustomRoleManager.GetActiveRoleFromPlayers(role => role.RoleHash == data.RoleHash);
            role?.Networked?.OnReceiveRoleSync?.Invoke(new(data.SyncId, data.Reader, player));
        }
        catch (Exception ex)
        {
            Logger.Error("OnReceiveRoleSync: " + ex);
        }
    }
}
