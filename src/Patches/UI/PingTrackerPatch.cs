using HarmonyLib;
using TheBetterRoles.Monos;

namespace TheBetterRoles.Patches.UI;

[HarmonyPatch(typeof(PingTracker))]
internal class PingTrackerPatch
{
    [HarmonyPatch(nameof(PingTracker.Update))]
    [HarmonyPrefix]
    private static bool Prefix(PingTracker __instance)
    {
        if (BetterPingTracker.Instance == null)
        {
            var betterPingTracker = __instance.gameObject.AddComponent<BetterPingTracker>();
            betterPingTracker.SetUp(__instance.text, __instance.aspectPosition);
        }
        __instance.enabled = false;

        return false;
    }
}