using Hazel;
using Reactor.Networking.Rpc;
using TheBetterRoles.Network.RPCs;

namespace TheBetterRoles.Roles.Core.RoleBase;

internal sealed class RoleNetworked
{
    internal Action<Data> OnReceiveRoleSync;

    internal void Initialize(RoleClass role)
    {
        Role = role;
    }

    internal RoleClass Role { get; private set; }

    /// <summary>
    /// Sends a synchronization message for role abilities.
    /// </summary>
    /// <param name="syncId">The synchronization identifier for the ability.</param>
    /// <param name="additionalParams">Optional additional parameters for the ability.</param>
    internal void SendRoleSync(int syncId = 0, params object[] additionalParams)
    {
        Rpc<RpcSyncRole>.Instance.Send(Role._player, new(syncId, Role.RoleHash, additionalParams));
    }

    /// <summary>
    /// Sends a synchronization message for role abilities.
    /// </summary>
    /// <param name="additionalParams">Optional additional parameters for the ability.</param>
    internal void SendRoleSync(params object[] additionalParams)
    {
        SendRoleSync(0, additionalParams);
    }

    internal readonly struct Data(int syncId, MessageReader messageReader, PlayerControl sender)
    {
        internal readonly int SyncId = syncId;
        internal readonly MessageReader MessageReader = messageReader;
        internal readonly PlayerControl Sender = sender;
    }
}
