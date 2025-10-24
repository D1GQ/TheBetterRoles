namespace TheBetterRoles.Items.Interfaces;

/// <summary>
/// This interface, originally from Among Us, is directly copied to ensure compatibility with the game's IL2CPP structure.
/// In Among Us, interfaces act like classes due to IL2CPP implementation, allowing seamless conversion between them.
/// This interface defines the properties and methods required for objects that can be used or interacted with, such as consoles.
/// </summary>
internal interface IIUsable
{
    /// <summary>
    /// Gets the maximum distance at which the usable object can be interacted with.
    /// This distance determines how close a player needs to be to interact with the object.
    /// It is typically used for objects like consoles or other interactive objects within the game world.
    /// </summary>
    float UsableDistance { get; }

    /// <summary>
    /// Gets the cooldown time for using the object. This cooldown is individual for each usable object,
    /// meaning each object can have its own independent cooldown timer to prevent spamming interactions.
    /// This ensures that players can only interact with the object after the cooldown period has passed.
    /// </summary>
    float PercentCool { get; }

    /// <summary>
    /// Determines which image or icon should be displayed when the usable object is selected or highlighted.
    /// This is useful for providing visual feedback to players about which object they are interacting with,
    /// and is usually associated with an icon representing the object's function (e.g., a wrench for fixing, a button for pressing).
    /// </summary>
    ImageNames UseIcon { get; }

    /// <summary>
    /// Sets the outline for the object when it is selected, providing visual feedback to the player about which object is currently active.
    /// This helps the player know which object is interactable, and it may be visually emphasized further if it is the main target or selected item.
    /// </summary>
    /// <param name="on">Indicates whether the outline should be activated (highlighted) or deactivated (unhighlighted).</param>
    /// <param name="mainTarget">Indicates whether the object is the main target for interaction, which may trigger additional highlighting for emphasis.</param>
    void SetOutline(bool on, bool mainTarget);

    /// <summary>
    /// Determines if the object is usable by the player. It checks if the player can interact with the object, 
    /// based on certain conditions such as whether the player is close enough or has the necessary requirements.
    /// It also checks if the object can be used at this time (e.g., if it's not on cooldown).
    /// </summary>
    /// <param name="pc">The player data of the player attempting to use the object. This is usually the local player or a networked player.</param>
    /// <param name="canUse">Outputs a boolean indicating whether the player can use the object (e.g., within range and not on cooldown).</param>
    /// <param name="couldUse">Outputs a boolean indicating whether the player could potentially use the object, even if conditions aren't currently met.</param>
    /// <returns>A float representing the current state of the object, possibly related to the cooldown or progress of an interaction.</returns>
    float CanUse(NetworkedPlayerInfo pc, out bool canUse, out bool couldUse);

    /// <summary>
    /// Implements the logic that is executed when the object is used or interacted with by the player.
    /// This may involve performing the object's function, such as repairing, pressing a button, or activating a console.
    /// </summary>
    void Use();
}