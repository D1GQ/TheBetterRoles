using UnityEngine;

namespace TheBetterRoles.Helpers;

/// <summary>
/// Provides utility methods for performing physics-related checks, such as detecting objects within a certain range.
/// </summary>
internal static class PhysicsHelper
{
    // Constants.ShipAndAllObjectsMask

    /// <summary>
    /// Checks if there are any enabled colliders within a specified range around a 2D position.
    /// </summary>
    /// <param name="position">The 2D position to check around.</param>
    /// <param name="range">The range within which to check for colliders.</param>
    /// <param name="layerMask">The layer mask to filter colliders by.</param>
    /// <returns>True if an enabled collider is found within the range; otherwise, false.</returns>
    internal static bool AnythingAround(this Vector2 position, float range, int layerMask)
    {
        return Physics2D.OverlapCircle(position, range, layerMask)?.enabled == true;
    }

    /// <summary>
    /// Checks if there are any enabled colliders within a specified range around a 3D position.
    /// Converts the 3D position to 2D for the check.
    /// </summary>
    /// <param name="position">The 3D position to check around.</param>
    /// <param name="range">The range within which to check for colliders.</param>
    /// <param name="layerMask">The layer mask to filter colliders by.</param>
    /// <returns>True if an enabled collider is found within the range; otherwise, false.</returns>
    internal static bool AnythingAround(this Vector3 position, float range, int layerMask)
    {
        return AnythingAround((Vector2)position, range, layerMask);
    }
}