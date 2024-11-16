
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Patches;

namespace TheBetterRoles.Roles;

public class ImpostorRoleTBR : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 2;
    public override string RoleColor => Utils.GetCustomRoleTeamColor(RoleTeam);
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Impostor;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Impostor;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Vanilla;
    public override BetterOptionTab? SettingsTab => BetterTabs.ImpostorRoles;
    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
            ];
        }
    }
}
