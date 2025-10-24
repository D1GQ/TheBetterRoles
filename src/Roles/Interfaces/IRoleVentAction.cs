namespace TheBetterRoles.Roles.Interfaces;

internal interface IRoleVentAction : IRoleAction
{
    /// <summary>
    /// Check when another player attempts to use or exit a vent.
    /// If this method returns false, it will cancel the designated action.
    /// </summary>
    bool CheckVentOther(PlayerControl venter, int ventId, bool Exit) => true;

    /// <summary>
    /// Check when the local player attempts to use or exit a vent.
    /// If this method returns false, it will cancel the designated action.
    /// </summary>
    bool CheckVent(PlayerControl venter, int ventId, bool Exit) => true;

    /// <summary>
    /// Called after a player vents. This handles the logic once the vent action is approved.
    /// </summary>
    void OnVentOther(PlayerControl venter, int ventId, bool Exit) { }

    /// <summary>
    /// Called after the local player vents. This handles the logic once the vent action is approved.
    /// </summary>
    void OnVent(PlayerControl venter, int ventId, bool Exit) { }
}
