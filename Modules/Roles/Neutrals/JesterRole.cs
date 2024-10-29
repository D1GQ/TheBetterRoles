
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Patches;

namespace TheBetterRoles.Roles;

public class JesterRole : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 20;
    public override string RoleColor => "#FF82F8";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Jester;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Neutral;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Evil;
    public override BetterOptionTab? SettingsTab => BetterTabs.NeutralRoles;
    public override bool AlwaysShowVoteOutMsg => true;
    public override OptionAttributes? AdditionalVentOptions => new() { Cooldown = 10f, Duration = 5f, };
    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
            ];
        }
    }
    private bool HasBeenVotedOut = false;
    public override void OnExileEnd(PlayerControl? exiled, NetworkedPlayerInfo? exiledData)
    {
        if (exiled == _player)
        {
            HasBeenVotedOut = true;
        }
    }
    public override bool WinCondition() => HasBeenVotedOut;
}
