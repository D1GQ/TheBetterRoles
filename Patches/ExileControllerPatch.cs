using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheBetterRoles.Patches;

[HarmonyPatch(typeof(ExileController))]
class ExileControllerPatch
{
    [HarmonyPatch(nameof(ExileController.HandleText))]
    [HarmonyPrefix]
    public static void HandleText_Prefix(ExileController __instance)
    {
        __instance.completeString = "";
    }

    [HarmonyPatch(nameof(ExileController.WrapUp))]
    [HarmonyPrefix]
    public static bool WrapUp_Prefix(ExileController __instance)
    {
        CustomRoleManager.RoleListenerOther(role => role.OnExileEnd(__instance?.initData?.networkedPlayer?.Object, __instance?.initData?.networkedPlayer));
        CustomRoleManager.RoleListener(PlayerControl.LocalPlayer, role => role.SetAllCooldowns());

        if (__instance.initData.networkedPlayer != null)
        {
            PlayerControl @object = __instance.initData.networkedPlayer.Object;
            if (@object)
            {
                @object.Exiled();
            }
            __instance.initData.networkedPlayer.IsDead = true;
        }
        __instance.ReEnableGameplay();
        UnityEngine.Object.Destroy(__instance.gameObject);

        return false;
    }
}
