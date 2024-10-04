using HarmonyLib;
using UnityEngine;

namespace TheBetterRoles.Patches;

[HarmonyPatch(typeof(ModManager))]
public class ModManagerPatch
{
    [HarmonyPatch(nameof(ModManager.LateUpdate))]
    [HarmonyPostfix]
    public static void LateUpdate_Postfix(ModManager __instance)
    {
        __instance.ShowModStamp();

        FileChecker.UpdateUnauthorizedFiles();
        LateTask.Update(Time.deltaTime);
        BetterNotificationManager.Update();
        RolePatch.Update();
    }
}
