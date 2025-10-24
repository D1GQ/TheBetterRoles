using HarmonyLib;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;

namespace TheBetterRoles.Patches.Game;

[HarmonyPatch(typeof(CosmeticsLayer))]
internal class CosmeticsLayerPatch
{
    [HarmonyPatch(nameof(CosmeticsLayer.SetColor))]
    [HarmonyPostfix]
    internal static void RawSetColor_Postfix(CosmeticsLayer __instance, int color)
    {
        __instance.SetColorBlindColor(color);
    }

    [HarmonyPatch(nameof(CosmeticsLayer.SetPetPosition))]
    [HarmonyPrefix]
    internal static bool SetPetPosition_Prefix(CosmeticsLayer __instance)
    {
        if (__instance.GetPet()?.targetPlayer == null) return false;
        return true;
    }

    [HarmonyPatch(nameof(CosmeticsLayer.TogglePetVisible))]
    [HarmonyPrefix]
    internal static bool TogglePetVisible_Prefix(CosmeticsLayer __instance)
    {
        if (__instance.GetPet()?.targetPlayer == null) return false;
        return true;
    }

    [HarmonyPatch(nameof(CosmeticsLayer.GetColorBlindText))]
    [HarmonyPrefix]
    private static bool GetColorBlindText_Prefix(CosmeticsLayer __instance, ref string __result)
    {
        string colorName = Palette.GetColorName(__instance.bodyMatProperties.ColorId);

        if (!string.IsNullOrEmpty(colorName))
        {
            __result = (char.ToUpperInvariant(colorName[0]) + colorName[1..].ToLowerInvariant()).ToColor(Palette.PlayerColors[__instance.bodyMatProperties.ColorId]);
        }
        else
        {
            __result = string.Empty;
        }

        return false;
    }
}