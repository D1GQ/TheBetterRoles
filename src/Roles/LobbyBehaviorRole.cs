using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;

namespace TheBetterRoles.Roles;

internal sealed class LobbyBehaviorRole : RoleClass
{
    internal override int RoleId => -1;
    internal override string RoleColorHex => "#FFFFFF";
    internal override RoleClassTypes RoleType => RoleClassTypes.LobbyBehavior;
    internal override RoleClassTeam RoleTeam => RoleClassTeam.None;
    internal override RoleClassCategory RoleCategory => RoleClassCategory.Vanilla;
    internal override OptionTab? SettingsTab => TBRTabs.SystemSettings;
    internal override bool CanBeAssigned => false;
    internal override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
            ];
        }
    }
    protected override void SetUpSettings() { }
}
