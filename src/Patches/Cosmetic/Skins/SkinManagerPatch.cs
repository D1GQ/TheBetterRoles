using Cpp2IL.Core.Extensions;
using HarmonyLib;
using TheBetterRoles.Managers;

namespace TheBetterRoles.Patches.Cosmetic.Skins;

[HarmonyPatch(typeof(HatManager))]
internal static class SkinManagerPatch
{
    private static bool isRunning;

    [HarmonyPatch(nameof(HatManager.GetSkinById))]
    [HarmonyPrefix]
    private static void GetSkinById_Prefix(HatManager __instance)
    {
        if (isRunning || CustomHatManager.UnregisteredSkins.Count <= 0) return;

        isRunning = true;
        var allSkins = __instance.allSkins.ToList();

        var unregisteredSkinsCache = CustomHatManager.UnregisteredSkins.Clone();
        foreach (var skin in unregisteredSkinsCache)
        {
            if (skin == null) continue;
            allSkins.Add(CustomHatManager.CreateSkinBehaviour(skin));
            CustomHatManager.UnregisteredSkins.Remove(skin);
        }

        unregisteredSkinsCache.Clear();
        __instance.allSkins = allSkins.ToArray();
        isRunning = false;
    }
}