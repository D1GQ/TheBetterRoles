
using Hazel;
using TheBetterRoles.Patches;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace TheBetterRoles;

public class SnitchRole : CustomRoleBehavior
{
    // Role Info
    public override bool TaskReliantRole => true;
    public override string RoleColor => "#F3CE35";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Snitch;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Crewmate;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Information;
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
    public override void OnSetUpRole()
    {
        TryOverrideTasks();
    }
}
