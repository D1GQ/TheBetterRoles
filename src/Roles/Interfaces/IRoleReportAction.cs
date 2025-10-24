namespace TheBetterRoles.Roles.Interfaces;

internal interface IRoleReportAction : IRoleAction
{
    /// <summary>
    /// Check when another player attempts to report a body.
    /// If this method returns false, it will cancel the designated action.
    /// </summary>
    bool CheckBodyReportOther(PlayerControl reporter, NetworkedPlayerInfo? bodyData, bool isButton) => true;

    /// <summary>
    /// Check when the local player attempts to report a body.
    /// If this method returns false, it will cancel the designated action.
    /// </summary>
    bool CheckBodyReport(PlayerControl reporter, NetworkedPlayerInfo? bodyData, bool isButton) => true;

    /// <summary>
    /// Check when another player attempts to report a body.
    /// This code is only ran by the local client!
    /// If this method returns false, it will cancel the designated action.
    /// </summary>
    bool CheckBodyOther(DeadBody body) => true;

    /// <summary>
    /// Check when the local player attempts to report a body. If the check fails, the report action will be canceled.
    /// This code is only ran by the local client!
    /// If this method returns false, it will cancel the designated action.
    /// </summary>
    bool CheckBody(DeadBody body) => true;

    /// <summary>
    /// Called after a player reports a body. This executes the logic after the report is approved.
    /// </summary>
    void BodyReportOther(PlayerControl reporter, NetworkedPlayerInfo? body, bool isButton) { }

    /// <summary>
    /// Called after the local player reports a body. This executes the logic after the report is approved.
    /// </summary>
    void BodyReport(PlayerControl reporter, NetworkedPlayerInfo? body, bool isButton) { }
}
