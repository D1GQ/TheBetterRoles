using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Patches;

namespace TheBetterRoles.Roles;

public class ImpostorRoleTBR : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 2;
    public override CustomRoleBehavior Role => this;
    public override CustomRoleType RoleType => CustomRoleType.Impostor;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Impostor;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Vanilla;
    public override TBROptionTab? SettingsTab => BetterTabs.ImpostorRoles;
    public override TBROptionItem[]? OptionItems
    {
        get
        {
            return
            [
            ];
        }
    }
}
