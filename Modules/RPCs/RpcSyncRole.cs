using Hazel;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheBetterRoles.Managers;

namespace TheBetterRoles.RPCs
{
    [RegisterCustomRpc((uint)ReactorRPCs.SyncRole)]
    public class RpcSyncRole : PlayerCustomRpc<Main, RpcSyncRole.Data>
    {
        public override SendOption SendOption => SendOption.Reliable;
        public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;
        public RpcSyncRole(Main plugin, uint id) : base(plugin, id)
        {
        }

        public readonly struct Data(int syncId, ushort roleHash, object[]? writerParams = null, MessageReader? reader = null)
        {
            public readonly MessageReader? Reader = reader;
            public readonly object[]? WriterParams = writerParams;

            public readonly int SyncId = syncId;
            public readonly ushort RoleHash = roleHash;
        }

        public override void Write(MessageWriter writer, Data data)
        {
            writer.WritePacked(data.SyncId);
            writer.Write(data.RoleHash);
            CustomRoleManager.GetActiveRoleFromPlayers(role => role.RoleHash == data.RoleHash)?.OnSendRoleSync(data.SyncId, writer, data.WriterParams);
        }

        public override Data Read(MessageReader reader)
        {
            var SyncId = reader.ReadPackedInt32();
            var RoleHash = reader.ReadUInt16();

            return new Data(SyncId, RoleHash, null, reader);
        }

        public override void Handle(PlayerControl player, Data data)
        {
            CustomRoleManager.GetActiveRoleFromPlayers(role => role.RoleHash == data.RoleHash)?.OnReceiveRoleSync(data.SyncId, data.Reader, player);
        }
    }
}
