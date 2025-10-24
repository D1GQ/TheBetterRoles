using Hazel;

namespace TheBetterRoles.Items.Interfaces;

/// <summary>
/// This interface, originally from Among Us, is directly copied to ensure compatibility with the game's IL2CPP structure.
/// In Among Us, interfaces act like classes due to IL2CPP implementation, allowing seamless conversion between them.
/// This interface defines the properties and methods related to system management and synchronization in a multiplayer environment,
/// typically for critical game systems like sabotages or timers that need to be updated and synchronized across clients.
/// </summary>
internal interface IISystemType
{
    /// <summary>
    /// Gets a boolean value indicating whether the system needs to be synchronized with other clients.
    /// When the system's state has changed, this property will return true, indicating that the system is "dirty" and requires 
    /// synchronization with the host or other clients.
    /// </summary>
    bool IsDirty { get; }

    /// <summary>
    /// Updates the system's state over time. This is primarily used for systems that require a countdown or time-based changes, 
    /// such as a critical sabotage system. The deltaTime parameter represents the time that has passed since the last update 
    /// (usually frame time or game ticks).
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update, which is used to update the system's state over time.</param>
    void Deteriorate(float deltaTime);

    /// <summary>
    /// Invoked when a player requests an update to the system, or when the system's state needs to be synchronized with the host.
    /// This method ensures that the system's state is updated correctly by the host, and it handles actions like activating or 
    /// fixing sabotages. The update may also include information from the client that must be synchronized across the network.
    /// </summary>
    /// <param name="player">The player requesting the system update.</param>
    /// <param name="msgReader">The message reader, which typically contains instructions on what specific parts of the system need to be updated.</param>
    void UpdateSystem(PlayerControl player, MessageReader msgReader);

    /// <summary>
    /// Serializes the system's state and sends it to other clients. This method is run only by the host and is used to 
    /// ensure that the system's state (such as a sabotage) is accurately transmitted to all connected clients.
    /// The initialState flag determines whether this is the first synchronization or a subsequent update.
    /// </summary>
    /// <param name="writer">The message writer that will be used to serialize the system's state.</param>
    /// <param name="initialState">A flag indicating whether this is the initial synchronization or a regular state update.</param>
    void Serialize(MessageWriter writer, bool initialState);

    /// <summary>
    /// Deserializes the system's state received from the host, updating the system on the client side. This ensures that the 
    /// client is in sync with the host's state, whether it’s the first time the client connects or a routine update.
    /// The initialState flag indicates whether this is the first deserialization or just an update from the host.
    /// </summary>
    /// <param name="reader">The message reader that contains the serialized system state from the host.</param>
    /// <param name="initialState">A flag indicating whether this is the first deserialization or a regular state update.</param>
    void Deserialize(MessageReader reader, bool initialState);
}