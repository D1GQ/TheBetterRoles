using Cpp2IL.Core.Extensions;
using HarmonyLib;
using TheBetterRoles.Managers;

namespace TheBetterRoles.Patches;

[HarmonyPatch(typeof(HatManager))]
internal static class HatManagerPatches
{
    private static bool isRunning;

    [HarmonyPatch(nameof(HatManager.GetHatById))]
    [HarmonyPrefix]
    private static void GetHatByIdPrefix(HatManager __instance)
    {
        if (isRunning || CustomHatManager.UnregisteredHats.Count <= 0) return;

        isRunning = true;
        var allHats = __instance.allHats.ToList();

        var unregisteredHatsCache = CustomHatManager.UnregisteredHats.Clone();
        foreach (var hat in unregisteredHatsCache)
        {
            if (hat == null) continue;
            allHats.Add(CustomHatManager.CreateHatBehaviour(hat));
            CustomHatManager.UnregisteredHats.Remove(hat);
        }

        unregisteredHatsCache.Clear();
        __instance.allHats = allHats.ToArray();
        isRunning = false;
    }
}