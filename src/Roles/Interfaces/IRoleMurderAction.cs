namespace TheBetterRoles.Roles.Interfaces;

internal interface IRoleMurderAction : IRoleAction
{
    /// <summary>
    /// Check for the ability to murder another player. Returns false if the murder should be prevented.
    /// If this method returns false, it will cancel the designated action.
    /// </summary>
    bool CheckMurderOther(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility) => true;

    /// <summary>
    /// Check for the local player attempting to murder. This checks if the murder action is allowed.
    /// If this method returns false, it will cancel the designated action.
    /// </summary>
    bool CheckMurder(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility) => true;

    /// <summary>
    /// Executes when the host has allowed another player (not the local player) to successfully murder a target.
    /// Custom logic for what happens after the murder action is validated by the host can be placed here.
    /// </summary>
    void MurderOther(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility) { }

    /// <summary>
    /// Executes when the host has allowed the local player to successfully murder a target.
    /// Custom logic for what happens after the murder action is validated by the host can be placed here.
    /// </summary>
    void Murder(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility) { }

    /// <summary>
    /// Executes when a player is successfully murdered.
    /// Custom logic for what happens to the body.
    /// </summary>
    void DeadBodyDropOther(PlayerControl killer, DeadBody body) { }

    /// <summary>
    /// Executes when the player is successfully murdered.
    /// Custom logic for what happens to the body.
    /// </summary>
    void DeadBodyDrop(PlayerControl killer, DeadBody myBody) { }
}
