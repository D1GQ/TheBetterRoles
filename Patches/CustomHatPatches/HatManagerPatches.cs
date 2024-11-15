using Cpp2IL.Core.Extensions;
using HarmonyLib;
using TheBetterRoles.Managers;

namespace TheBetterRoles.Patches;

[HarmonyPatch(typeof(HatManager))]
internal static class HatManagerPatches
{
    private static bool isRunning;
    private static bool isLoaded;
    private static List<HatData> allHats;

    [HarmonyPatch(nameof(HatManager.GetHatById))]
    [HarmonyPrefix]
    private static void GetHatByIdPrefix(HatManager __instance)
    {
        if (isRunning || isLoaded) return;

        isRunning = true;
        allHats = [.. __instance.allHats];

        var unregisteredHatsCache = CustomHatManager.UnregisteredHats.Clone();
        foreach (var hat in unregisteredHatsCache)
        {
            if (hat == null) continue;

            try
            {
                allHats.Add(CustomHatManager.CreateHatBehaviour(hat));
                CustomHatManager.UnregisteredHats.Remove(hat);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        if (CustomHatManager.UnregisteredHats.Count == 0)
            isLoaded = true;

        unregisteredHatsCache.Clear();
        __instance.allHats = allHats.ToArray();
    }

    [HarmonyPatch(nameof(HatManager.GetHatById))]
    [HarmonyPostfix]
    private static void GetHatByIdPostfix()
    {
        isRunning = false;
    }
}