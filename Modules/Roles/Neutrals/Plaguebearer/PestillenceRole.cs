
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;

namespace TheBetterRoles.Roles;

public class PestillenceRole : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 18;
    public override bool CanBeAssigned => false;
    public override string RoleColor => "#4F631E";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Pestillence;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Neutral;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Chaos;
    public override BetterOptionTab? SettingsTab => BetterTabs.NeutralRoles;
    public override bool CanKill => true;
    public override bool CanVent => CustomRoleManager.GetRoleInstance<PlaguebearerRole>().CanVent;
    public override bool HasImpostorVision => CustomRoleManager.GetRoleInstance<PlaguebearerRole>().HasImpostorVision;

    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
            ];
        }
    }

    public bool WasTransformed = false;
    public override void OnSetUpRole()
    {
        KillButton.Cooldown = CustomRoleManager.GetRoleInstance<PlaguebearerRole>().PestilenceKillCooldown.GetFloat();
        KillButton.SetCooldown();
    }

    public override bool CheckMurder(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility)
    {
        if (killer != _player && target == _player)
        {
            return false;
        }

        return true;
    }

    public override string AddMeetingText(ref CustomClip? clip, out uint priority)
    {
        priority = 0;
        if (WasTransformed)
        {
            clip = new CustomClip() { ClipName = "Transform", Volume = 2f };
            WasTransformed = false;
            return $"<{CustomRoleManager.GetRoleInstance<PlaguebearerRole>().RoleColor}>{Translator.GetString("Role.Plaguebearer.TransformMsg")}</color>";
        }
        return string.Empty;
    }
}
