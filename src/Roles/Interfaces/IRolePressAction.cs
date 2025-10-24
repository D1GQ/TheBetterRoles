namespace TheBetterRoles.Roles.Interfaces;

internal interface IRolePressAction : IRoleAction
{
    /// <summary>
    /// Called when another player presses an action button by directly clicking on a player with the mouse.
    /// </summary>
    void PlayerPressOther(PlayerControl player, PlayerControl target) { }

    /// <summary>
    /// Called when the local player presses an action button by directly clicking on another player with the mouse.
    /// </summary>
    void PlayerPress(PlayerControl player, PlayerControl target) { }
}
