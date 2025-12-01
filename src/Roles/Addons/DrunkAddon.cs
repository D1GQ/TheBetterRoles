using HarmonyLib;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;

namespace TheBetterRoles.Roles.Addons;

internal sealed class DrunkAddon : AddonClass
{
    internal sealed override int RoleId => 34;
    internal sealed override string RoleColorHex => "#3e2e1d";
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Drunk;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.None;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.HarmfulAddon;
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

    internal sealed override void OnDeinitialize()
    {
        _player.MyPhysics.body.velocity *= 1;
    }

    [HarmonyPatch(typeof(PlayerPhysics))]
    class PlayerPhysicsDrunkPatch
    {
        [HarmonyPatch(nameof(PlayerPhysics.FixedUpdate))]
        [HarmonyPostfix]
        internal static void FixedUpdate_Postfix(PlayerPhysics __instance)
        {
            var player = __instance.myPlayer;
            if (!player.IsLocalPlayer()) return;
            if (!player.Has(RoleClassTypes.Drunk)) return;

            bool flag = !player.CanMove;
            __instance.body.velocity *= flag ? 1 : -1;
        }
    }
}
