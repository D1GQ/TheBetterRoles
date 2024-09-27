using HarmonyLib;
using InnerNet;

namespace TheBetterRoles;

[HarmonyPatch(typeof(MatchMakerGameButton))]
class MatchMakerGameButtonPatch
{
    [HarmonyPatch(nameof(MatchMakerGameButton.SetGame))]
    [HarmonyPostfix]
    private static void SetGame_Postfix(MatchMakerGameButton __instance)
    {
        __instance.NameText.text = $"<b><color=#f4f400>{__instance.myListing.HostName}</color></b>\n";
        __instance.NameText.text += $"<size=65%><voffset=0.4m><color=#9c9c9c>{Utils.GetPlatformName(__instance.myListing.Platform)} - {GameCode.IntToGameName(__instance.myListing.GameId)}</color></voffset></size>";
    }
}