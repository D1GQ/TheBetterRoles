using HarmonyLib;
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

    [HarmonyPatch(typeof(PlayerPhysics))]
    class PlayerPhysicsDrunkPatch
    {
        [HarmonyPatch(nameof(PlayerPhysics.FixedUpdate))]
        [HarmonyPostfix]
        public static void FixedUpdate_Postfix(PlayerPhysics __instance)
        {
            var player = __instance.myPlayer;
            if (!player.IsLocalPlayer()) return;
            if (!player.Has(CustomRoles.Drunk)) return;

            bool flag = !player.CanMove;
            __instance.body.velocity *= flag ? 1 : -1;
        }
    }
}
