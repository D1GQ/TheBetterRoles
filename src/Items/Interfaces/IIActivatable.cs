namespace TheBetterRoles.Items.Interfaces;

/// <summary>
/// This interface, originally from Among Us, is directly copied to ensure compatibility with the game's IL2CPP structure.
/// In Among Us, interfaces act like classes due to IL2CPP implementation, allowing seamless conversion between them.
/// This interface provides a property to determine whether the system is currently active.
/// </summary>
internal interface IIActivatable
{
    /// <summary>
    /// Gets a boolean value indicating whether the system is currently active.
    /// </summary>
    bool IsActive { get; }
}