namespace TheBetterRoles.Roles.Core.Interfaces;

internal interface IRoleDisconnectAction : IRoleAction
{
    /// <summary>
    /// Executes when a player disconnects.
    /// Custom logic for handling disconnections.
    /// </summary>
    void OnDisconnect(PlayerControl player, DisconnectReasons reason);
}
