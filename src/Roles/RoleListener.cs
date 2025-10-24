using Il2CppSystem.IO;
using TheBetterRoles.Managers;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Interfaces;

namespace TheBetterRoles.Roles;

/// <summary>
/// Provides a centralized system for managing and invoking role-related actions and events.
/// This class maintains a registry of all active roles and allows for efficient invocation
/// of role-specific methods based on implemented interfaces.
/// </summary>
internal static class RoleListener
{
    /// <summary>
    /// Registers a role instance with the listener system and maps it to all implemented IRoleAction interfaces.
    /// </summary>
    /// <param name="role">The role instance to register. If null, the method returns without action.</param>
    /// <exception cref="Exception">Any exception during registration is caught and logged.</exception>
    internal static void AddRole(RoleClass? role)
    {
        if (role == null)
        {
            return;
        }

        try
        {
            var roleInterfaces = role.GetType().GetInterfaces()
                .Where(i => typeof(IRoleAction).IsAssignableFrom(i));

            foreach (var interfaceType in roleInterfaces)
            {
                if (!CustomRoleManager.RoleListenerMap.TryGetValue(interfaceType, out var roles))
                {
                    roles = [];
                    CustomRoleManager.RoleListenerMap[interfaceType] = roles;
                }

                if (!roles.Contains(role))
                {
                    roles.Add(role);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error adding role {role?.GetType().Name}: {ex}");
        }
    }

    /// <summary>
    /// Removes a role instance from the listener system and cleans up any empty interface mappings.
    /// </summary>
    /// <param name="role">The role instance to remove. If null, the method returns without action.</param>
    internal static void RemoveRole(RoleClass? role)
    {
        if (role == null) return;

        foreach (var roles in CustomRoleManager.RoleListenerMap.Values)
        {
            roles.Remove(role);
        }

        var emptyKeys = CustomRoleManager.RoleListenerMap.Where(kv => kv.Value.Count == 0)
                               .Select(kv => kv.Key)
                               .ToArray();

        foreach (var key in emptyKeys)
        {
            CustomRoleManager.RoleListenerMap.Remove(key);
        }
    }

    /// <summary>
    /// Invokes an action on all roles of a specific type that are assigned to a player.
    /// </summary>
    /// <typeparam name="T">The IRoleAction interface type to target.</typeparam>
    /// <param name="player">The player whose roles should be targeted.</param>
    /// <param name="action">The action to invoke on matching roles.</param>
    /// <param name="filter">An optional filter to further restrict which roles are targeted.</param>
    internal static void InvokeRoles<T>(this PlayerControl player, Action<T> action, Func<RoleClass, bool>? filter = null) where T : IRoleAction => InvokeRoles<T>(action, filter, player);

    /// <summary>
    /// Invokes an action on all roles implementing a specific IRoleAction interface.
    /// </summary>
    /// <typeparam name="T">The IRoleAction interface type to target.</typeparam>
    /// <param name="action">The action to invoke on matching roles.</param>
    /// <param name="filter">An optional filter to further restrict which roles are targeted.</param>
    /// <param name="player">An optional player parameter to restrict invocation to roles assigned to a specific player.</param>
    internal static void InvokeRoles<T>(Action<T> action, Func<RoleClass, bool>? filter = null, PlayerControl? player = null) where T : IRoleAction
    {
        if (!CustomRoleManager.RoleListenerMap.TryGetValue(typeof(T), out var listeners))
            return;

        bool checkPlayer = player != null;
        bool hasFilter = filter != null;

        foreach (var role in listeners)
        {
            if (role == null) continue;
            if (hasFilter && !filter!(role)) continue;
            if (checkPlayer && role._player != player) continue;

            if (role is T roleAction)
            {
                action(roleAction);
            }
        }
    }

    /// <summary>
    /// Invokes an action on all roles assigned to a player.
    /// </summary>
    /// <param name="player">The player whose roles should be targeted.</param>
    /// <param name="action">The action to invoke on matching roles.</param>
    /// <param name="filter">An optional filter to further restrict which roles are targeted.</param>
    internal static void InvokeRoles(this PlayerControl player, Action<RoleClass> action, Func<RoleClass, bool>? filter = null) => InvokeRoles(action, filter, player);

    /// <summary>
    /// Invokes an action on all registered roles.
    /// </summary>
    /// <param name="action">The action to invoke on roles.</param>
    /// <param name="filter">An optional filter to restrict which roles are targeted.</param>
    /// <param name="player">An optional player parameter to restrict invocation to roles assigned to a specific player.</param>
    internal static void InvokeRoles(Action<RoleClass> action, Func<RoleClass, bool>? filter = null, PlayerControl? player = null)
    {
        bool checkPlayer = player != null;
        bool hasFilter = filter != null;

        foreach (var role in CustomRoleManager.AllActiveRoles)
        {
            if (role == null) continue;
            if (hasFilter && !filter!(role)) continue;
            if (checkPlayer && role._player != player) continue;

            action(role);
        }
    }

    /// <summary>
    /// Checks if all roles of a specific type assigned to a player satisfy a condition.
    /// </summary>
    /// <typeparam name="T">The IRoleAction interface type to check.</typeparam>
    /// <param name="player">The player whose roles should be checked.</param>
    /// <param name="predicate">The condition to evaluate for each role.</param>
    /// <param name="filter">An optional filter to further restrict which roles are checked.</param>
    /// <returns>True if all matching roles satisfy the condition, false otherwise.</returns>
    internal static bool CheckAllRoles<T>(this PlayerControl player, Func<T, bool> predicate, Func<RoleClass, bool>? filter = null) where T : IRoleAction => CheckAllRoles<T>(predicate, filter, player);

    /// <summary>
    /// Checks if all roles implementing a specific IRoleAction interface satisfy a condition.
    /// </summary>
    /// <typeparam name="T">The IRoleAction interface type to check.</typeparam>
    /// <param name="predicate">The condition to evaluate for each role.</param>
    /// <param name="filter">An optional filter to further restrict which roles are checked.</param>
    /// <param name="player">An optional player parameter to restrict checking to roles assigned to a specific player.</param>
    /// <returns>True if all matching roles satisfy the condition, false otherwise. Returns true if no roles match the criteria.</returns>
    internal static bool CheckAllRoles<T>(Func<T, bool> predicate, Func<RoleClass, bool>? filter = null, PlayerControl? player = null) where T : IRoleAction
    {
        if (!CustomRoleManager.RoleListenerMap.TryGetValue(typeof(T), out var listeners))
            return true; // No listeners means condition is vacuously true

        bool checkPlayer = player != null;
        bool hasFilter = filter != null;

        foreach (var role in listeners)
        {
            if (role == null) continue;
            if (hasFilter && !filter!(role)) continue;
            if (checkPlayer && role._player != player) continue;

            if (role is T roleAction)
            {
                if (!predicate(roleAction))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if all roles assigned to a player satisfy a condition.
    /// </summary>
    /// <param name="player">The player whose roles should be checked.</param>
    /// <param name="predicate">The condition to evaluate for each role.</param>
    /// <param name="filter">An optional filter to further restrict which roles are checked.</param>
    /// <returns>True if all matching roles satisfy the condition, false otherwise.</returns>
    internal static bool CheckAllRoles(this PlayerControl player, Func<RoleClass, bool> predicate, Func<RoleClass, bool>? filter = null) => CheckAllRoles(predicate, filter, player);

    /// <summary>
    /// Checks if all registered roles satisfy a condition.
    /// </summary>
    /// <param name="predicate">The condition to evaluate for each role.</param>
    /// <param name="filter">An optional filter to restrict which roles are checked.</param>
    /// <param name="player">An optional player parameter to restrict checking to roles assigned to a specific player.</param>
    /// <returns>True if all matching roles satisfy the condition, false otherwise.</returns>
    internal static bool CheckAllRoles(Func<RoleClass, bool> predicate, Func<RoleClass, bool>? filter = null, PlayerControl? player = null)
    {
        bool checkPlayer = player != null;
        bool hasFilter = filter != null;

        foreach (var role in CustomRoleManager.AllActiveRoles)
        {
            if (role == null) continue;
            if (hasFilter && !filter!(role)) continue;
            if (checkPlayer && role._player != player) continue;

            if (!predicate(role))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if any role of a specific type assigned to a player satisfies a condition.
    /// </summary>
    /// <typeparam name="T">The IRoleAction interface type to check.</typeparam>
    /// <param name="player">The player whose roles should be checked.</param>
    /// <param name="predicate">The condition to evaluate for each role.</param>
    /// <param name="filter">An optional filter to further restrict which roles are checked.</param>
    /// <returns>True if any matching role satisfies the condition, false otherwise.</returns>
    internal static bool CheckAnyRoles<T>(this PlayerControl player, Func<T, bool> predicate, Func<RoleClass, bool>? filter = null) where T : IRoleAction => CheckAnyRoles<T>(predicate, filter, player);

    /// <summary>
    /// Checks if any role implementing a specific IRoleAction interface satisfies a condition.
    /// </summary>
    /// <typeparam name="T">The IRoleAction interface type to check.</typeparam>
    /// <param name="predicate">The condition to evaluate for each role.</param>
    /// <param name="filter">An optional filter to further restrict which roles are checked.</param>
    /// <param name="player">An optional player parameter to restrict checking to roles assigned to a specific player.</param>
    /// <returns>True if any matching role satisfies the condition, false otherwise. Returns false if no roles match the criteria.</returns>
    internal static bool CheckAnyRoles<T>(Func<T, bool> predicate, Func<RoleClass, bool>? filter = null, PlayerControl? player = null) where T : IRoleAction
    {
        if (!CustomRoleManager.RoleListenerMap.TryGetValue(typeof(T), out var listeners))
            return false; // No listeners means condition is false

        bool checkPlayer = player != null;
        bool hasFilter = filter != null;

        foreach (var role in listeners)
        {
            if (role == null) continue;
            if (hasFilter && !filter!(role)) continue;
            if (checkPlayer && role._player != player) continue;

            if (role is T roleAction)
            {
                if (predicate(roleAction))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if any role assigned to a player satisfies a condition.
    /// </summary>
    /// <param name="player">The player whose roles should be checked.</param>
    /// <param name="predicate">The condition to evaluate for each role.</param>
    /// <param name="filter">An optional filter to further restrict which roles are checked.</param>
    /// <returns>True if any matching role satisfies the condition, false otherwise.</returns>
    internal static bool CheckAnyRoles(this PlayerControl player, Func<RoleClass, bool> predicate, Func<RoleClass, bool>? filter = null) => CheckAnyRoles(predicate, filter, player);

    /// <summary>
    /// Checks if any registered role satisfies a condition.
    /// </summary>
    /// <param name="predicate">The condition to evaluate for each role.</param>
    /// <param name="filter">An optional filter to restrict which roles are checked.</param>
    /// <param name="player">An optional player parameter to restrict checking to roles assigned to a specific player.</param>
    /// <returns>True if any matching role satisfies the condition, false otherwise.</returns>
    internal static bool CheckAnyRoles(Func<RoleClass, bool> predicate, Func<RoleClass, bool>? filter = null, PlayerControl? player = null)
    {
        bool checkPlayer = player != null;
        bool hasFilter = filter != null;

        foreach (var role in CustomRoleManager.AllActiveRoles)
        {
            if (role == null) continue;
            if (hasFilter && !filter!(role)) continue;
            if (checkPlayer && role._player != player) continue;

            if (predicate(role))
                return true;
        }

        return false;
    }
}