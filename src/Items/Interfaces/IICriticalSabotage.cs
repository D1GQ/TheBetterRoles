namespace TheBetterRoles.Items.Interfaces;

/// <summary>
/// This interface, originally from Among Us, is directly copied to ensure compatibility with the game's IL2CPP structure.
/// In Among Us, interfaces act like classes due to IL2CPP implementation, allowing seamless conversion between them.
/// This interface defines the properties and methods related to critical sabotage systems in the game.
/// </summary>
internal interface IICriticalSabotage
{
    /// <summary>
    /// Gets a boolean value indicating whether the critical sabotage is currently active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Gets the countdown time remaining until the critical sabotage ends the game.
    /// </summary>
    float Countdown { get; }

    /// <summary>
    /// Gets the number of players currently required at consoles to fix the critical sabotage.
    /// </summary>
    int UserCount { get; }

    /// <summary>
    /// Clears the active critical sabotage, resetting the system and allowing normal gameplay to resume.
    /// </summary>
    void ClearSabotage();
}