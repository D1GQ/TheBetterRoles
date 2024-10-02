
using TheBetterRoles.Patches;

namespace TheBetterRoles;

public class ImpostorRoleTBR : CustomRoleBehavior
{
    // Role Info
    public override string RoleColor => Utils.GetCustomRoleTeamColor(RoleTeam);
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Impostor;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Impostor;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Vanilla;
    public override BetterOptionTab? SettingsTab => BetterTabs.ImpostorRoles;
    public override bool CanKill => true;
    public override bool CanSabotage => true;
    public override bool CanVent => true;

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
