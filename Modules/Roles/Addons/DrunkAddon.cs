using TheBetterRoles.Helpers;
using TheBetterRoles.Helpers.Random;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using TheBetterRoles.RPCs;

namespace TheBetterRoles.Roles;

public class DrunkAddon : CustomAddonBehavior
{
    // Role Info
    public override int RoleId => 34;
    public override string RoleColor => "#3e2e1d";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Drunk;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.None;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.HarmfulAddon;
    public override BetterOptionTab? SettingsTab => BetterTabs.Addons;
    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
            ];
        }
    }

    public override void OnDeinitialize()
    {
        _player.MyPhysics.body.velocity *= 1;
    }

    public override void FixedUpdate()
    {
        if (_player.IsLocalPlayer())
        {
            bool flag = !_player.CanMove;
            _player.MyPhysics.body.velocity *= flag ? 1 : -1;
        }
    }
}
