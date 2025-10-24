using HarmonyLib;

namespace TheBetterRoles.Patches.Manager.GameEnd;

[HarmonyPatch(typeof(EndGameManager))]
internal class EndGameManagerPatch
{
    [HarmonyPatch(nameof(EndGameManager.ShowButtons))]
    [HarmonyPrefix]
    private static bool ShowButtons_Prefix(EndGameManager __instance)
    {
        __instance.FrontMost.gameObject.SetActive(false);
        __instance.Navigation.ShowDefaultNavigation();

        return false;
    }
}