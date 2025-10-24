using Cpp2IL.Core.Extensions;
using HarmonyLib;
using TheBetterRoles.Managers;

namespace TheBetterRoles.Patches.Cosmetic.Visors;

[HarmonyPatch(typeof(HatManager))]
internal static class VisorManagerPatch
{
    private static bool isRunning;

    [HarmonyPatch(nameof(HatManager.GetVisorById))]
    [HarmonyPrefix]
    private static void GetVisorById_Prefix(HatManager __instance)
    {
        if (isRunning || CustomHatManager.UnregisteredVisors.Count <= 0) return;

        isRunning = true;
        var allVisors = __instance.allVisors.ToList();

        var unregisteredVisorsCache = CustomHatManager.UnregisteredVisors.Clone();
        foreach (var visor in unregisteredVisorsCache)
        {
            if (visor == null) continue;
            allVisors.Add(CustomHatManager.CreateVisorBehaviour(visor));
            CustomHatManager.UnregisteredVisors.Remove(visor);
        }

        unregisteredVisorsCache.Clear();
        __instance.allVisors = allVisors.ToArray();
        isRunning = false;
    }
}