
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles;

public class PestillenceRole : CustomRoleBehavior
{
    // Role Info
    public override bool CanBeAssigned => false;
    public override string RoleColor => "#4F631E";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Pestillence;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Neutral;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Chaos;
    public override BetterOptionTab? SettingsTab => BetterTabs.NeutralRoles;
    public override bool CanKill => true;
    public override bool CanVent => CustomRoleManager.GetRoleInstance<PlaguebearerRole>().CanVent;

    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
            ];
        }
    }

    private bool showMsg = true;
    public override void OnSetUpRole()
    {
        KillButton.Cooldown = CustomRoleManager.GetRoleInstance<PlaguebearerRole>().PestilenceKillCooldown.GetFloat();
    }

    public override bool CheckMurder(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility)
    {
        if (killer != _player && target == _player)
        {
            return false;
        }

        return true;
    }
    public override string AddMeetingStartUpText(ref CustomClip? clip)
    {
        if (showMsg)
        {
            showMsg = false;
            return $"<{CustomRoleManager.GetRoleInstance<PlaguebearerRole>().RoleColor}>{Translator.GetString("Role.Plaguebearer.TransformMsg")}</color>";
        }
        return string.Empty;
    }
}
