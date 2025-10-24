namespace TheBetterRoles.Roles.Interfaces;

internal interface IRoleInteractedAction : IRoleAction
{
    /// <summary>
    /// Called when another player interacts or gets interaction with a Target button.
    /// </summary>
    void PlayerInteractedOther(PlayerControl player, PlayerControl target) { }

    /// <summary>
    /// Called when the local player interacts or gets interaction with a Target button.
    /// </summary>
    void PlayerInteracted(PlayerControl player, PlayerControl target) { }
}
