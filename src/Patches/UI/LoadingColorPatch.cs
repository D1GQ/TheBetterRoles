using HarmonyLib;
using TheBetterRoles.Modules;
using UnityEngine;

namespace TheBetterRoles.Patches.UI;

// Sets the color of the little mini crewmate on loading screens
internal class LoadingColorPatch
{
    private static SpriteRenderer? loading;
    internal static void LateUpdate()
    {
        if (loading == null)
        {
            loading = GameObject.Find("GameLoadAnimation")?.GetComponentInChildren<SpriteRenderer>();
            if (loading != null)
            {
                PlayerMaterial.SetColors(CustomColors.WaterId, loading);
            }
        }
    }

    [HarmonyPatch(typeof(LoadingMarquee))]
    private class LoadingMarqueePatch
    {
        [HarmonyPatch(nameof(LoadingMarquee.Start))]
        [HarmonyPostfix]
        internal static void Start_Postfix(RegionMenu __instance)
        {
            PlayerMaterial.SetColors(CustomColors.WaterId, __instance.GetComponent<SpriteRenderer>());
        }
    }

    [HarmonyPatch(typeof(WaitingRotate))]
    private class WaitingRotatePatch
    {
        [HarmonyPatch(nameof(WaitingRotate.Start))]
        [HarmonyPostfix]
        internal static void Start_Postfix(WaitingRotate __instance)
        {
            PlayerMaterial.SetColors(CustomColors.WaterId, __instance.GetComponent<SpriteRenderer>());
        }
    }

    [HarmonyPatch(typeof(AmongUsLoadingBar))]
    private class AmongUsLoadingBarPatch
    {
        [HarmonyPatch(nameof(AmongUsLoadingBar.OnEnable))]
        [HarmonyPostfix]
        internal static void Start_Postfix(AmongUsLoadingBar __instance)
        {
            PlayerMaterial.SetColors(CustomColors.WaterId, __instance.crewmate.GetComponent<SpriteRenderer>());
        }
    }
}
