using HarmonyLib;

namespace TheBetterRoles.Patches.UI;

[HarmonyPatch(typeof(NotificationPopper))]
internal class NotificationPopperPatch
{
    // Stops the original setting notifications
    [HarmonyPatch(nameof(NotificationPopper.AddSettingsChangeMessage))]
    [HarmonyPrefix]
    private static bool AddSettingsChangeMessage_Prefix(/*NotificationPopper __instance*/)
    {
        return false;
    }
}