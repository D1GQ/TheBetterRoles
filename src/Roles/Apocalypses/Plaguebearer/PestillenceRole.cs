using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Core.Interfaces;

namespace TheBetterRoles.Roles.Apocalypses;

internal sealed class PestillenceRole : RoleClass, IRoleMurderAction, IRoleMeetingAction
{
    internal sealed override int RoleId => 18;
    internal sealed override bool CanBeAssigned => false;
    internal sealed override string RoleColorHex => "#4F631E";
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Pestillence;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Apocalypse;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Killing;
    internal sealed override OptionTab? SettingsTab => TBRTabs.ApocalypseRoles;
    internal sealed override bool CanKill => true;
    internal sealed override bool CanVent => CustomRoleManager.GetRolePrefab<PlaguebearerRole>().CanVent;

    internal sealed override bool HasImpostorVision => CustomRoleManager.GetRolePrefab<PlaguebearerRole>().HasImpostorVision;
    internal OptionItem? PestilenceKillCooldown => CustomRoleManager.GetRolePrefab<PlaguebearerRole>().PestilenceKillCooldown;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
            ];
        }
    }

    internal bool WasTransformed = false;
    internal sealed override void OnSetUpRole()
    {
        if (_player.IsLocalPlayer())
        {
            RoleButtons.KillButton.Cooldown = PestilenceKillCooldown.GetFloat();
            RoleButtons.KillButton.SetCooldown();
        }
    }

    bool IRoleMurderAction.CheckMurder(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility)
    {
        if (killer != _player && target == _player)
        {
            return false;
        }

        return true;
    }

    string IRoleMeetingAction.AddMeetingText(ref CustomClip? clip, out uint priority)
    {
        priority = 0;
        if (WasTransformed)
        {
            clip = new CustomClip() { ClipName = "Transform", Volume = 2f };
            WasTransformed = false;
            return $"<{CustomRoleManager.GetRolePrefab<PlaguebearerRole>().RoleColorHex}>{Translator.GetString("Role.Plaguebearer.TransformMsg")}</color>";
        }
        return string.Empty;
    }
}
