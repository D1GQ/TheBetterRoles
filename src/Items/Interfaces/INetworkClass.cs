using Hazel;
using TheBetterRoles.Items.Attributes;
using TheBetterRoles.Network;

namespace TheBetterRoles.Items.Interfaces;

/// <summary>
/// This interface defines the contract for network synchronization logic, providing essential methods for serializing
/// and deserializing data. It ensures that the state of the network class can be synchronized across all clients
/// in a multiplayer environment, especially when certain bits or data fields change (becoming "dirty").
/// The interface is designed to allow for synchronization of objects that require network communication, such as game states.
/// </summary>
internal interface INetworkClass
{
    SyncVarAttribute[] SyncVars { get; }

    /// <summary>
    /// Gets the set of bits that are currently synced across the network. These bits are used to determine which parts 
    /// of the class are synchronized between the host and clients. The `SyncedBits` property tracks which bits have been
    /// synchronized, ensuring that the relevant data is kept consistent across the network.
    /// </summary>
    SyncedBits SyncedBits { get; }

    /// <summary>
    /// Gets the set of dirty bits that need to be synchronized with other clients. A "dirty" bit indicates that a 
    /// specific part of the class has changed and needs to be sent to other clients for synchronization.
    /// The `DirtyBits` property tracks the changes and determines what data needs to be updated across the network.
    /// </summary>
    uint DirtyBits { get; }

    /// <summary>
    /// Gets the unique network identifier (ID) for the NetworkClass. This ID ensures that each NetworkClass instance 
    /// is uniquely identifiable within the network, allowing the system to know which class or object is being synchronized.
    /// This ID should always be unique across all networked objects to avoid conflicts and ensure accurate data synchronization.
    /// </summary>
    uint NetworkId { get; }

    /// <summary>
    /// Gets the unique player client ID that owns this NetworkClass instance.  
    /// This ID is used to determine ownership and properly serialize/deserialize the object across the network.
    /// </summary>
    int OwnerId { get; }

    /// <summary>
    /// Implements the logic for serializing the data of the network class to be sent to other clients. This method is 
    /// invoked when any of the dirty bits are set, signaling that the data has changed and needs to be transmitted.
    /// Ensure that any bits that have been marked as dirty are appropriately handled to avoid synchronization issues or
    /// infinite loops where the data keeps being sent without being cleared.
    /// </summary>
    /// <param name="writer">The message writer used to serialize the data. It is responsible for packing and sending the data.</param>
    public void Serialize(MessageWriter writer);

    /// <summary>
    /// Implements the logic for deserializing the data received from the network. This method is used to update the state
    /// of the object on the client-side when new data is received. The deserialization process ensures that the client 
    /// is in sync with the host and can reflect the most current state of the networked object.
    /// </summary>
    /// <param name="reader">The message reader that contains the serialized data. It is used to unpack and update the state of the object.</param>
    public void Deserialize(MessageReader reader);
}