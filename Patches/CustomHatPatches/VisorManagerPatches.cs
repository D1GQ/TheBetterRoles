using Cpp2IL.Core.Extensions;
using HarmonyLib;
using TheBetterRoles.Managers;

namespace TheBetterRoles.Patches;

[HarmonyPatch(typeof(HatManager))]
internal static class VisorManagerPatches
{
    private static bool isRunning;
    private static bool isLoaded;

    [HarmonyPatch(nameof(HatManager.GetVisorById))]
    [HarmonyPrefix]
    private static void GetVisorByIdPrefix(HatManager __instance)
    {
        if (isRunning || isLoaded) return;

        isRunning = true;

        var visors = __instance.allVisors.ToList().Clone();
        foreach (var visor in visors)
        {
            visor.behindHats = false;
        }
        isLoaded = true;
    }

    [HarmonyPatch(nameof(HatManager.GetHatById))]
    [HarmonyPostfix]
    private static void GetVisorByIdPostfix()
    {
        isRunning = false;
    }
}