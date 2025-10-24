using HarmonyLib;

namespace TheBetterRoles.Patches.UI;

[HarmonyPatch(typeof(RoomTracker))]
internal class RoomTrackerPatch
{
    [HarmonyPatch(nameof(RoomTracker.Awake))]
    [HarmonyPostfix]
    private static void Awake_Postfix(RoomTracker __instance)
    {
        __instance.SourceY = -2.7f;
    }
}