using TheBetterRoles.Items.Enums;

namespace TheBetterRoles.Roles.Interfaces;

internal interface IRoleDeathAction : IRoleAction
{
    /// <summary>
    /// Triggered when the local player dies, regardless of the cause.
    /// This method allows for custom logic to handle player death events, including deaths from
    /// murders, abilities, or other game mechanics.
    /// </summary>
    virtual void OnDeath(PlayerControl player, DeathReasons reason) { }

    /// <summary>
    /// Triggered when a player dies, regardless of the cause.
    /// This method allows for custom logic to handle player death events, including deaths from
    /// murders, abilities, or other game mechanics.
    /// </summary>
    virtual void OnDeathOther(PlayerControl player, DeathReasons reason) { }

    /// <summary>
    /// Executes when a player is successfully murdered.
    /// Custom logic for what happens to the body.
    /// </summary>
    virtual void DeadBodyDropOther(PlayerControl killer, DeadBody body) { }

    /// <summary>
    /// Executes when the player is successfully murdered.
    /// Custom logic for what happens to the body.
    /// </summary>
    virtual void DeadBodyDrop(PlayerControl killer, DeadBody myBody) { }
}
