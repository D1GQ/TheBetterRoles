using HarmonyLib;
using TheBetterRoles.Patches.Manager;

namespace TheBetterRoles.Patches.Game.Player;

[HarmonyPatch(typeof(AmongUsClient))]
internal static class PlayerJoinAndLeftPatch
{
    [HarmonyPatch(nameof(AmongUsClient.OnPlayerLeft))]
    [HarmonyPostfix]
    private static void OnPlayerLeft_Postfix()
    {
        MeetingHudPatch.UpdateHostIcon();
    }
}
