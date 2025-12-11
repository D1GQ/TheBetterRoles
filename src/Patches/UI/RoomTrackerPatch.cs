using HarmonyLib;
using UnityEngine;

namespace TheBetterRoles.Patches.UI;

[HarmonyPatch(typeof(RoomTracker))]
internal class RoomTrackerPatch
{
    [HarmonyPatch(nameof(RoomTracker.Awake))]
    [HarmonyPostfix]
    private static void Awake_Postfix(RoomTracker __instance)
    {
        __instance.SourceY = -2.7f;

        // Fix aspect ratio issues
        var originalParent = __instance.transform.parent;
        var holder = new GameObject("RoomTrackerHolder");
        holder.transform.SetParent(originalParent);
        __instance.transform.SetParent(holder.transform);
        var aspectPosition = holder.AddComponent<AspectPosition>();
        aspectPosition.Alignment = AspectPosition.EdgeAlignments.Bottom;
        aspectPosition.DistanceFromEdge = new Vector3(0f, 3f, 0f);
        aspectPosition.updateAlways = true;
    }
}