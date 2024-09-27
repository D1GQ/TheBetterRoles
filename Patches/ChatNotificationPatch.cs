using HarmonyLib;
using UnityEngine;

namespace TheBetterRoles.Patches;

[HarmonyPatch(typeof(ChatNotification))]
public class ChatNotificationPatch
{
    [HarmonyPatch(nameof(ChatNotification.Update))]
    [HarmonyPostfix]
    public static void Update_Postfix(ChatNotification __instance)
    {
        __instance.GetComponent<AspectPosition>().DistanceFromEdge = new Vector3(-2.8f, 0.3f, -40f);
        __instance.transform.localScale = new Vector3(0.45f, 0.42f);
    }
}
