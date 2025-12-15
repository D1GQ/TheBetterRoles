namespace TheBetterRoles.Roles.Core.Interfaces;

internal interface IRoleOtherAction : IRoleAction
{
    /// <summary>
    /// Additional setup logic for a role.
    /// </summary>
    void SetUpRoleOther(PlayerControl player, RoleClass role) { }

    void DeinitializeOther(RoleClass role) { }
}
