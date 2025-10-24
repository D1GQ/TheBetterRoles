using Hazel;
using Il2CppInterop.Runtime.Attributes;
using Reactor.Networking.Rpc;
using System.Reflection;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Attributes;
using TheBetterRoles.Items.Interfaces;
using TheBetterRoles.Modules;
using TheBetterRoles.Network.RPCs;

namespace TheBetterRoles.Network;

/// <summary>
/// Represents a network class that can be synchronized across a network.
/// This class handles network identifiers, ownership, and dirty state tracking.
/// </summary>
internal class NetworkClass : INetworkClass
{
    /// <summary>
    /// Gets the unique class hash based on its namespace and name.
    /// </summary>
    internal uint ClassHash => Utils.GetHashUInt16(GetType().FullName);

    /// <summary>
    /// A collection of all active <see cref="NetworkClass"/> instances.
    /// </summary>
    internal static List<NetworkClass> AllNetworkClasses { get; private set; } = [];

    /// <summary>
    /// Clears all network classes from the collection.
    /// </summary>
    internal static void DisposeAllNetworkClasses()
    {
        AllNetworkClasses.Clear();
    }

    /// <summary>
    /// Removes the current instance from the collection of network classes.
    /// </summary>
    internal void DisposeNetworkClass()
    {
        AllNetworkClasses.Remove(this);
    }

    [HideFromIl2Cpp]
    public SyncVarAttribute[] SyncVars { get; set; } = [];

    /// <summary>
    /// Sets up the network class with a unique network ID and an owner ID.
    /// </summary>
    /// <param name="netId">The network ID of the class (default is 0).</param>
    /// <param name="ownerId">The owner ID of the class (default is -1).</param>
    [HideFromIl2Cpp]
    internal void SetUpNetworkClass(uint netId = 0, int ownerId = -1)
    {
        if (this is null) return;
        AllNetworkClasses.Add(this);
        NetworkId = netId ^ ClassHash;
        OwnerId = ownerId;
        SyncVars = GetAllSyncVars();
    }

    [HideFromIl2Cpp]
    private SyncVarAttribute[] GetAllSyncVars()
    {
        var list = new List<SyncVarAttribute>();

        foreach (var field in GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            var syncVar = field.GetCustomAttribute<SyncVarAttribute>();
            if (syncVar != null)
            {
                syncVar.Setup(field);
                list.Add(syncVar);
            }
        }

        foreach (var property in GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            var syncVar = property.GetCustomAttribute<SyncVarAttribute>();
            if (syncVar != null)
            {
                syncVar.Setup(property);
                list.Add(syncVar);
            }
        }

        return list.ToArray();
    }

    /// <summary>
    /// Retrieves a <see cref="NetworkClass"/> instance from the collection by network ID.
    /// </summary>
    /// <param name="netId">The network ID to search for.</param>
    /// <returns>The <see cref="NetworkClass"/> instance associated with the given network ID, or null if not found.</returns>
    internal static NetworkClass? GetFromNetId(uint netId) => AllNetworkClasses.FirstOrDefault(net => net.NetworkId == netId);

    /// <summary>
    /// Represents the synchronized bits associated with the network class.
    /// </summary>
    public SyncedBits SyncedBits { get; set; } = new SyncedBits();

    /// <summary>
    /// Determines if the current instance is the owner of this network class.
    /// </summary>
    internal bool AmOwner => OwnerId > -2 && (OwnerId == AmongUsClient.Instance.ClientId || GameState.IsHost);

    /// <summary>
    /// The ID of the owner of the network class.
    /// </summary>
    public int OwnerId { get; set; } = -1;

    /// <summary>
    /// The unique network ID of the network class.
    /// </summary>
    public uint NetworkId { get; set; } = 0;

    /// <summary>
    /// A set of dirty bits used for tracking changes in the network class.
    /// </summary>
    public uint DirtyBits { get; set; }

    /// <summary>
    /// Indicates whether there are any dirty bits set.
    /// </summary>
    internal bool IsDirty => DirtyBits > 0U;

    /// <summary>
    /// Checks if a specific dirty bit is set by its index.
    /// </summary>
    /// <param name="idx">The index of the dirty bit to check.</param>
    /// <returns>True if the specified dirty bit is set, otherwise false.</returns>
    internal bool IsDirtyBitSet(int idx)
    {
        return (DirtyBits & 1U << idx) > 0U;
    }

    /// <summary>
    /// Clears all dirty bits.
    /// </summary>
    internal void ClearDirtyBits()
    {
        DirtyBits = 0U;
    }

    /// <summary>
    /// Unsets the dirty bit at the specified index.
    /// </summary>
    /// <param name="idx">The index of the dirty bit to unset.</param>
    internal void UnsetDirtyBit(int idx)
    {
        DirtyBits &= ~(1U << idx);
    }

    /// <summary>
    /// Sets the dirty bit at the specified index.
    /// </summary>
    /// <param name="idx">The index of the dirty bit to set.</param>
    internal void SetDirtyBit(int idx)
    {
        if (idx < 0 || idx >= 32)
        {
            throw new ArgumentOutOfRangeException(nameof(idx), "Index must be between 0 and 31.");
        }

        DirtyBits |= 1U << idx;
    }

    /// <summary>
    /// Marks the network class as dirty by setting bit 1.
    /// </summary>
    internal void MarkDirty()
    {
        SetDirtyBit(1);
    }

    /// <summary>
    /// Serializes the network class state to a <see cref="MessageWriter"/>.
    /// </summary>
    /// <param name="writer">The writer to serialize the state to.</param>
    public virtual void Serialize(MessageWriter writer) { }

    /// <summary>
    /// Deserializes the network class state from a <see cref="MessageReader"/>.
    /// </summary>
    /// <param name="reader">The reader to deserialize the state from.</param>
    public virtual void Deserialize(MessageReader reader) { }

    internal static void LateUpdate()
    {
        if (!GameState.IsInGame)
        {
            if (AllNetworkClasses.Count > 0) DisposeAllNetworkClasses();
            return;
        }

        foreach (var netClass in AllNetworkClasses)
        {
            if (netClass == null) continue;

            if (netClass.AmOwner)
            {
                if (SyncVarAttribute.AnyDirty(netClass))
                {
                    Rpc<RpcDirtyNetworkClass>.Instance.Send(new(netClass, isSyncVar: true));
                }
            }

            if (netClass.IsDirty)
            {
                if (netClass.AmOwner)
                {
                    Rpc<RpcDirtyNetworkClass>.Instance.Send(new(netClass));
                }
                else
                {
                    netClass.ClearDirtyBits();
                }
            }
        }
    }
}