using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Interfaces;

namespace TheBetterRoles.Roles.Addons;

internal sealed class OnboundAddon : AddonClass, IRoleGuessAction
{
    internal sealed override int RoleId => 40;
    internal sealed override string RoleColorHex => "#3298FF";
    internal sealed override RoleClass Role => this;
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Onbound;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.None;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.HelpfulAddon;
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

    bool IRoleGuessAction.CheckGuess(PlayerControl guesser, PlayerControl target, RoleClassTypes role)
    {
        if (target == _player)
        {
            if (guesser.IsLocalPlayer())
            {
                HudManager.Instance.ShowPopUp(Translator.GetString("GuestManager.UnableToGuess", [target.GetPlayerNameAndColor()]));
            }

            return false;
        }

        return true;
    }
}
