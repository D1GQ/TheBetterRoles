
using TheBetterRoles.Patches;

namespace TheBetterRoles;

public class CrewmateRoleTBR : CustomRoleBehavior
{
    // Role Info
    public override string RoleColor => Utils.GetCustomRoleTeamColor(RoleTeam);
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Crewmate;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Crewmate;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Vanilla;
    public override BetterOptionTab? SettingsTab => BetterTabs.CrewmateRoles;
    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
            ];
        }
    }

    public override void SetUpRole()
    {
    }
}
