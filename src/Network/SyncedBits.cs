namespace TheBetterRoles.Network;

/// <summary>
/// Represents synchronized dirty bits for network classes, indicating changes in the state.
/// </summary>
public class SyncedBits
{
    /// <summary>
    /// Indicates whether there are any synchronized dirty bits set.
    /// </summary>
    internal bool IsDirty => SyncedDirtyBits > 0U;

    /// <summary>
    /// A set of synchronized dirty bits used for tracking changes in the state.
    /// </summary>
    internal uint SyncedDirtyBits { get; set; }

    /// <summary>
    /// Checks if a specific synchronized dirty bit is set by its index.
    /// </summary>
    /// <param name="idx">The index of the dirty bit to check.</param>
    /// <returns>True if the specified synchronized dirty bit is set, otherwise false.</returns>
    internal bool IsDirtyBitSet(int idx)
    {
        return (SyncedDirtyBits & 1U << idx) > 0U;
    }
}
