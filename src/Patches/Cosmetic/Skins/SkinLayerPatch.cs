using HarmonyLib;
using TheBetterRoles.Monos;

namespace TheBetterRoles.Patches.Cosmetic.Visors;

[HarmonyPatch(typeof(SkinLayer))]
internal static class SkinLayerPatch
{
    [HarmonyPatch(nameof(SkinLayer.UpdateMaterial))]
    [HarmonyPostfix]
    private static void UpdateMaterial_Postfix(SkinLayer __instance)
    {
        var com = __instance.GetComponent<CustomSkinAnimator>();
        if (com == null)
        {
            var customSkinAnimator = __instance.gameObject.AddComponent<CustomSkinAnimator>();
            customSkinAnimator.skinLayer = __instance;
        }
    }
}
