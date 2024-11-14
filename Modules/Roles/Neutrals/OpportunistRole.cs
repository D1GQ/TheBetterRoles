using TheBetterRoles.Helpers;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Patches;

namespace TheBetterRoles.Roles;

public class OpportunistRole : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 22;
    public override string RoleColor => "#00CA28";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Opportunist;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Neutral;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Benign;
    public override BetterOptionTab? SettingsTab => BetterTabs.NeutralRoles;
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
    public override void OnGameEnd(ref List<byte> OtherWinnerIds)
    {
        if (_player.IsAlive() && !OtherWinnerIds.Contains(_player.PlayerId))
        {
            OtherWinnerIds.Add(_data.PlayerId);
        }
    }
}
