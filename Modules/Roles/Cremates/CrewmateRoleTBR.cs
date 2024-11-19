
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Patches;

namespace TheBetterRoles.Roles;

public class CrewmateRoleTBR : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 1;
    public override string RoleColor => Utils.GetCustomRoleTeamColor(RoleTeam);
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Crewmate;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Crewmate;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Vanilla;
    public override TBROptionTab? SettingsTab => BetterTabs.CrewmateRoles;
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
