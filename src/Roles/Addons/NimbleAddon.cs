using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Interfaces;

namespace TheBetterRoles.Roles.Addons;

internal sealed class NimbleAddon : AddonClass, IRoleOtherAction
{
    internal sealed override Func<RoleClass, bool> AssignmentConditionWithRole => (RoleClass role) => { return !role.CanVent; };
    internal sealed override int RoleId => 42;
    internal sealed override string RoleColorHex => "#F4DC89";
    internal sealed override RoleClass Role => this;
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Nimble;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.None;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.GoodAddon;
    internal sealed override OptionTab? SettingsTab => TBRTabs.Addons;
    internal sealed override bool CanVent => canVent;

    internal sealed override OptionAttributes? AdditionalVentOptions => new() { Duration = 0f, Cooldown = 5f };
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
            ];
        }
    }

    private bool canVent = false;
    internal sealed override void OnSetUpRole()
    {
        if (_player.IsLocalPlayer())
        {
            var role = _player.Role();
            if (!role.CanVent && role.IsCrewmate)
            {
                SetVentButton(role.RoleButtons.VentButton);
            }
            else
            {
                canVent = false;
            }
        }
    }

    void IRoleOtherAction.SetUpRoleOther(PlayerControl player, RoleClass role)
    {
        if (player.IsLocalPlayer())
        {
            if (!role.CanVent && role.IsCrewmate)
            {
                SetVentButton(role.RoleButtons.VentButton);
            }
            else
            {
                canVent = false;
            }
        }
    }

    private void SetVentButton(VentAbilityButton? ventButton)
    {
        if (ventButton != null)
        {
            canVent = true;
            ventButton.SetFromRole(this);
        }
    }
}
