using HarmonyLib;
using TheBetterRoles.Items.OptionItems;

namespace TheBetterRoles.Patches.UI.GameSettings;

[HarmonyPatch(typeof(GameOptionsMenu))]
internal class GameOptionsMenuPatch
{
    [HarmonyPatch(nameof(GameOptionsMenu.CreateSettings))]
    [HarmonyPrefix]
    private static bool CreateSettings_Prefix(GameOptionsMenu __instance)
    {
        foreach (var tab in OptionTab.AllTabs)
        {
            if (tab.AUTab == __instance)
            {
                return false;
            }
        }

        return true;
    }
}