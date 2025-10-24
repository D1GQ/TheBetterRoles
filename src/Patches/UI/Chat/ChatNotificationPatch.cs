using HarmonyLib;
using UnityEngine;

namespace TheBetterRoles.Patches.UI.Chat;

[HarmonyPatch(typeof(ChatNotification))]
internal class ChatNotificationPatch
{
    [HarmonyPatch(nameof(ChatNotification.SetUp))]
    [HarmonyPostfix]
    internal static void SetUp_Postfix(ChatNotification __instance)
    {
        __instance.GetComponent<AspectPosition>().DistanceFromEdge = new Vector3(-2.8f, 0.3f, -40f);
        __instance.transform.localScale = new Vector3(0.45f, 0.42f, 1f);
    }
}
