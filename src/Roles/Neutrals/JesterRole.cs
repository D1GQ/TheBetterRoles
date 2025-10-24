using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Interfaces;
using UnityEngine;

namespace TheBetterRoles.Roles.Neutrals;

internal sealed class JesterRole : RoleClass, IRoleMeetingAction, IRoleGameplayAction
{
    internal sealed override int RoleId => 20;
    internal sealed override string RoleColorHex => "#FF82F8";
    internal sealed override RoleClass Role => this;
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Jester;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Neutral;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Evil;
    internal sealed override OptionTab? SettingsTab => TBRTabs.NeutralRoles;
    internal sealed override AudioClip? IntroSound => Prefab.GetCachedPrefab<EngineerRole>().IntroSound;
    internal sealed override bool MeetingReliantRole => true;
    internal sealed override bool AlwaysShowVoteOutMsg => true;

    internal sealed override OptionAttributes? AdditionalVentOptions => new() { Cooldown = 10f, Duration = 5f, };
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
            ];
        }
    }

    private bool HasBeenVotedOut = false;
    void IRoleMeetingAction.ExileEnd(PlayerControl? exiled, NetworkedPlayerInfo? exiledData)
    {
        if (exiled == _player)
        {
            HasBeenVotedOut = true;
            CheckWinCondition();
        }
    }

    bool IRoleGameplayAction.WinCondition() => HasBeenVotedOut;
}