using HarmonyLib;
using TheBetterRoles.Managers;

namespace TheBetterRoles.Patches;

[HarmonyPatch(typeof(CosmeticsCache))]
public class CosmeticsCachePatches
{
    [HarmonyPatch(nameof(CosmeticsCache.GetHat))]
    [HarmonyPrefix]
    public static bool GetHatPrefix(string id, ref HatViewData __result)
    {
        return !CustomHatManager.ViewDataCache.TryGetValue(id, out __result);
    }
}
