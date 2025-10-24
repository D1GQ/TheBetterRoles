using Il2CppInterop.Runtime.InteropTypes;

namespace TheBetterRoles.Helpers;

/// <summary>
/// Provides helper methods for safely casting objects to a specified type.
/// </summary>
internal static class CastHelper
{
    /// <summary>
    /// Determines whether the specified object can be cast to type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The target type to check.</typeparam>
    /// <param name="obj">The object to check.</param>
    /// <returns><c>true</c> if the object can be cast to <typeparamref name="T"/>; otherwise, <c>false</c>.</returns>
    internal static bool TryCast<T>(this object obj) => obj is T;

    /// <summary>
    /// Attempts to cast the specified object to type <typeparamref name="T"/> and returns the result.
    /// </summary>
    /// <typeparam name="T">The target type to cast to.</typeparam>
    /// <param name="obj">The object to cast.</param>
    /// <param name="item">The output parameter that holds the casted object if successful, or <c>null</c> if the cast fails.</param>
    /// <returns><c>true</c> if the cast is successful; otherwise, <c>false</c>.</returns>
    internal static bool TryCast<T>(this object obj, out T? item) where T : class
    {
        if (obj != null && obj is T casted)
        {
            item = casted;
            return true;
        }

        item = null;
        return false;
    }

    /// <summary>
    /// Attempts to cast the specified object to type <typeparamref name="T"/> and returns the result.
    /// </summary>
    /// <typeparam name="T">The target type to cast to.</typeparam>
    /// <param name="obj">The object to cast.</param>
    /// <param name="item">The output parameter that holds the casted object if successful, or <c>null</c> if the cast fails.</param>
    /// <returns><c>true</c> if the cast is successful; otherwise, <c>false</c>.</returns>
    internal static bool TryCast<T>(this Il2CppObjectBase obj, out T? item) where T : Il2CppObjectBase => (item = obj.TryCast<T>()) != null;
}