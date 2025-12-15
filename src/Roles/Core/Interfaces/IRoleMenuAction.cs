using TheBetterRoles.Items;

namespace TheBetterRoles.Roles.Core.Interfaces;

internal interface IRoleMenuAction : IRoleAction
{
    /// <summary>
    /// Called when the player chooses an target in player list menu.
    /// Only ran by the local client!
    /// </summary>
    void PlayerMenu(int id, PlayerControl? target, NetworkedPlayerInfo? targetData, PlayerMenu? menu, ShapeshifterPanel? playerPanel, bool close);
}
