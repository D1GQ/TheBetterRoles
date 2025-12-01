using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Modules;
using TheBetterRoles.Network.RPCs;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Interfaces;

namespace TheBetterRoles.Roles.Addons;

internal sealed class ButtonBerryAddon : AddonClass, IRoleAbilityAction
{
    internal sealed override int RoleId => 24;
    internal sealed override string RoleColorHex => "#FF00EE";
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.ButtonBerry;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.None;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.AbilityAddon;
    internal sealed override OptionTab? SettingsTab => TBRTabs.Addons;

    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
            ];
        }
    }

    internal BaseAbilityButton? MeetingButton;
    internal sealed override void OnSetUpRole()
    {
        if (_player.IsLocalPlayer())
        {
            MeetingButton = RoleButtons.AddButton(new BaseAbilityButton().Create(5, Translator.GetString("Role.ButtonBerry.Ability.1"), 0, 0, 1, null, this, false));
        }
    }

    void IRoleAbilityAction.OnAbility(int id)
    {
        switch (id)
        {
            case 5:
                _player.RemainingEmergencies++;
                _player.SendRpcReportBody(null);
                RoleButtons.RemoveButton(MeetingButton);
                break;
        }
    }
}
