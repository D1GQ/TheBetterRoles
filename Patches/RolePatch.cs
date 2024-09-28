using HarmonyLib;
using InnerNet;
using System.Reflection;

namespace TheBetterRoles.Patches;

class RolePatch
{
    [HarmonyPatch(typeof(PlayerControl))]
    class PlayerControlPatch
    {
        [HarmonyPatch(nameof(PlayerControl.FixedUpdate))]
        [HarmonyPrefix]
        public static void FixedUpdate_Prefix(PlayerControl __instance)
        {
            if (__instance?.BetterData()?.RoleInfo?.RoleAssigned == true)
            {
                __instance.BetterData().RoleInfo.Role.Update();
            }

            if (__instance.IsLocalPlayer())
            {
                foreach (var button in __instance.BetterData().RoleInfo.Role.Buttons)
                {
                    button.Update();
                }
            }
        }
    }

    public static void ClearRoleData(PlayerControl player) => player.ClearRoles();
}
