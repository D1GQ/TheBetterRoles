using TheBetterRoles.Items.Enums;
using UnityEngine;

namespace TheBetterRoles.Roles.Core.Interfaces;

internal interface IRoleAbilityAction<T> : IRoleAbilityAction where T : MonoBehaviour
{
    /// <summary>
    /// Called after a ability button has been pressed from the local player.
    /// This runs the logic after the ability is allowed.
    /// </summary>
    void OnAbility(int id, T target) { }

    /// <summary>
    /// Check for an ability being used by the local player.
    /// If this method returns false, it will cancel the designated action.
    /// </summary>
    bool CheckAbility(int id, T target) => true;
}

internal interface IRoleAbilityAction : IRoleAction
{
    /// <summary>
    /// Called after a ability button has been pressed from the local player.
    /// This runs the logic after the ability is allowed.
    /// </summary>
    void OnAbility(int id) { }

    /// <summary>
    /// Check for an ability being used by the local player.
    /// If this method returns false, it will cancel the designated action.
    /// </summary>
    bool CheckAbility(int id) => true;

    /// <summary>
    /// Called when the duration of an ability ends, typically to clean up or reset states related to the ability.
    /// </summary>
    void AbilityDurationEnd(int id, bool isTimeOut) { }

    /// <summary>
    /// Resets the state of any ability-related cooldowns or flags for this role.
    /// </summary>
    void OnResetAbilityState(bool isTimeOut) { }

    /// <summary>
    /// Called after a ability button has been pressed from a player.
    /// Executes custom logic once the ability is allowed.
    /// </summary>
    void OnAbilityOther(int id, RoleClass role, TargetType targetType, MonoBehaviour target) { }

    /// <summary>
    /// Check for an ability being used by another player.
    /// If this method returns false, it will cancel the designated action.
    /// </summary>
    bool CheckAbilityOther(int id, RoleClass role, TargetType targetType, MonoBehaviour target) => true;
}
