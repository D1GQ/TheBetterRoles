using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;

namespace TheBetterRoles.Roles.Crewmates;

internal class CrewmateRoleTBR : RoleClass
{
    internal override int RoleId => 1;
    internal override RoleClass Role => this;
    internal override RoleClassTypes RoleType => RoleClassTypes.Crewmate;
    internal override RoleClassTeam RoleTeam => RoleClassTeam.Crewmate;
    internal override RoleClassCategory RoleCategory => RoleClassCategory.Vanilla;
    internal override OptionTab? SettingsTab => TBRTabs.CrewmateRoles;
    internal override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
            ];
        }
    }
}
