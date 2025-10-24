using Hazel;
using Il2CppInterop.Runtime.Attributes;
using Reactor.Networking.Rpc;
using System.Reflection;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Attributes;
using TheBetterRoles.Items.Interfaces;
using TheBetterRoles.Modules;
using TheBetterRoles.Network.RPCs;
using UnityEngine;

namespace TheBetterRoles.Network;

/// <summary>
/// A MonoBehaviour-based class for networked objects, implementing <see cref="INetworkClass"/>.
/// This class handles synchronization, ownership, and network interactions.
/// </summary>
internal class NetworkClassMono : MonoBehaviour, INetworkClass
{
    /// <summary>
    /// Gets the unique class hash based on its namespace and name.
    /// </summary>
    internal uint ClassHash => Utils.GetHashUInt16(GetType().FullName);

    /// <summary>
    /// A collection of all active <see cref="NetworkClassMono"/> instances.
    /// </summary>
    internal static List<NetworkClassMono> AllNetworkClasses { get; private set; } = [];

    /// <summary>
    /// Clears all network class instances from the collection.
    /// </summary>
    internal static void DisposeAllNetworkClasses()
    {
        AllNetworkClasses.Clear();
    }

    /// <summary>
    /// Removes the current instance from the collection of network class instances.
    /// </summary>
    protected void DisposeNetworkClass()
    {
        AllNetworkClasses.Remove(this);
    }

    [HideFromIl2Cpp]
    public SyncVarAttribute[] SyncVars { get; set; } = [];

    /// <summary>
    /// Sets up the network class with a network ID and an owner ID.
    /// </summary>
    /// <param name="netId">The network ID (default is 0).</param>
    /// <param name="ownerId">The owner ID (default is -1).</param>
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

        foreach (var field in GetType().GetFields())
        {
            var syncVar = field.GetCustomAttribute<SyncVarAttribute>();
            if (syncVar != null)
            {
                syncVar.Setup(field);
                list.Add(syncVar);
            }
        }

        foreach (var property in GetType().GetProperties())
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
    /// Called when the object is destroyed. It disposes of the network class and calls <see cref="OnNetDestroy"/>.
    /// </summary>
    private void OnDestroy()
    {
        DisposeNetworkClass();
        OnNetDestroy();
    }

    /// <summary>
    /// Allows subclasses to define custom cleanup logic when the network class is destroyed.
    /// </summary>
    protected virtual void OnNetDestroy() { }

    /// <summary>
    /// Retrieves a <see cref="NetworkClassMono"/> instance by network ID.
    /// </summary>
    /// <param name="netId">The network ID to search for.</param>
    /// <returns>The <see cref="NetworkClassMono"/> instance, or null if not found.</returns>
    internal static NetworkClassMono? GetFromNetId(uint netId) => AllNetworkClasses.FirstOrDefault(net => net.NetworkId == netId);

    /// <summary>
    /// Represents the synchronized bits of the network class.
    /// </summary>
    [HideFromIl2Cpp]
    public SyncedBits SyncedBits { get; set; } = new SyncedBits();

    /// <summary>
    /// Determines whether the current instance is the owner of the network class.
    /// </summary>
    internal bool AmOwner => OwnerId > -2 && (OwnerId == AmongUsClient.Instance.ClientId || GameState.IsHost) || OwnerId <= -2;

    /// <summary>
    /// The owner ID of the network class (default is -1).
    /// </summary>
    public int OwnerId { get; set; } = -1;

    /// <summary>
    /// The network ID of the network class (default is 0).
    /// </summary>
    public uint NetworkId { get; set; } = 0;

    /// <summary>
    /// A set of dirty bits used to track changes in the state of the network class.
    /// </summary>
    public uint DirtyBits { get; set; }

    /// <summary>
    /// Indicates whether any dirty bits are set (i.e., the network class has changed).
    /// </summary>
    internal bool IsDirty => DirtyBits > 0U;

    /// <summary>
    /// Checks whether a specific dirty bit is set based on its index.
    /// </summary>
    /// <param name="idx">The index of the dirty bit to check.</param>
    /// <returns>True if the dirty bit at the specified index is set, otherwise false.</returns>
    internal bool IsDirtyBitSet(int idx)
    {
        return (DirtyBits & 1U << idx) > 0U;
    }

    /// <summary>
    /// Clears all dirty bits (marks the object as not dirty).
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
    new internal void MarkDirty()
    {
        SetDirtyBit(1);
    }

    /// <summary>
    /// Serializes the network class state to a <see cref="MessageWriter"/>.
    /// Subclasses can override this method to implement custom serialization logic.
    /// </summary>
    /// <param name="writer">The writer to serialize the state to.</param>
    public virtual void Serialize(MessageWriter writer) { }

    /// <summary>
    /// Deserializes the network class state from a <see cref="MessageReader"/>.
    /// Subclasses can override this method to implement custom deserialization logic.
    /// </summary>
    /// <param name="reader">The reader to deserialize the state from.</param>
    public virtual void Deserialize(MessageReader reader) { }

    internal void FixedUpdate()
    {
        if (AmOwner)
        {
            if (SyncVarAttribute.AnyDirty(this))
            {
                Rpc<RpcDirtyNetworkClass>.Instance.Send(new(this, isSyncVar: true));
            }
        }

        if (IsDirty)
        {
            if (AmOwner)
            {
                Rpc<RpcDirtyNetworkClass>.Instance.Send(new(this));
            }
            else
            {
                ClearDirtyBits();
            }
        }
    }
}