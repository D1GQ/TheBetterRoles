using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Interfaces;

namespace TheBetterRoles.Roles.Neutrals;

internal sealed class OpportunistRole : RoleClass, IRoleGameplayAction
{
    internal sealed override int RoleId => 22;
    internal sealed override string RoleColorHex => "#00CA28";
    internal sealed override RoleClass Role => this;
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Opportunist;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Neutral;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Benign;
    internal sealed override OptionTab? SettingsTab => TBRTabs.NeutralRoles;
    internal sealed override OptionAttributes? AdditionalVentOptions => new() { Cooldown = 10f, Duration = 5f, };
    internal sealed override bool CountToPlayerAmount => Main.AllAlivePlayerControls.Count <= 1 && _player.IsAlive(); // Only count to player amount if last player standing

    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
            ];
        }
    }

    void IRoleGameplayAction.GameEnd()
    {
        if (_player.IsAlive() && !_player.HasWon())
        {
            _player.AddSubWinner();
        }
    }
}
