using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;

namespace TheBetterRoles.Roles.Impostors;

internal class ImpostorRoleTBR : RoleClass
{
    internal override int RoleId => 2;
    internal override RoleClassTypes RoleType => RoleClassTypes.Impostor;
    internal override RoleClassTeam RoleTeam => RoleClassTeam.Impostor;
    internal override RoleClassCategory RoleCategory => RoleClassCategory.Vanilla;
    internal override OptionTab? SettingsTab => TBRTabs.ImpostorRoles;
    internal override bool VentReliantRole => true;
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
