using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Network.RPCs;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Core.Interfaces;

namespace TheBetterRoles.Roles.Addons;

internal sealed class ReboundAddon : AddonClass, IRoleGuessAction
{
    internal sealed override int RoleId => 39;
    internal sealed override string RoleColorHex => "#3298FF";
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Rebound;
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
            _player.SendRpcGuessPlayer(guesser, guesser.Role().RoleType, false);
            return false;
        }

        return true;
    }
}
