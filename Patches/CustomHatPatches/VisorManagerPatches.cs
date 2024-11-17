using Cpp2IL.Core.Extensions;
using HarmonyLib;

namespace TheBetterRoles.Patches;

[HarmonyPatch(typeof(HatManager))]
internal static class VisorManagerPatches
{
    private static bool Done;

    [HarmonyPatch(nameof(HatManager.GetVisorById))]
    [HarmonyPrefix]
    private static void GetVisorByIdPrefix(HatManager __instance)
    {
        if (Done) return;

        var visors = __instance.allVisors.ToList();
        foreach (var visor in visors)
        {
            visor.behindHats = false;
        }

        Done = true;
    }
}