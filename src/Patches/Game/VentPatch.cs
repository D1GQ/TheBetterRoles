using HarmonyLib;
using TheBetterRoles.Helpers;

namespace TheBetterRoles.Patches.Game;

[HarmonyPatch(typeof(Vent))]
internal class VentRolePatch
{
    [HarmonyPatch(nameof(Vent.SetButtons))]
    [HarmonyPostfix]
    private static void SetButtons_Postfix(Vent __instance)
    {
        Vent[] nearbyVents = __instance.NearbyVents;
        for (int i = 0; i < __instance.Buttons.Length; i++)
        {
            ButtonBehavior buttonBehavior = __instance.Buttons[i];
            Vent vent = nearbyVents[i];
            if (vent)
            {
                __instance.ToggleNeighborVentBeingCleaned(!vent.IsEnabled(), buttonBehavior, __instance.CleaningIndicators[i]);
            }
        }
    }

    [HarmonyPatch(nameof(Vent.UpdateArrows))]
    [HarmonyPostfix]
    private static void UpdateArrows_Postfix(Vent __instance)
    {
        Vent[] nearbyVents = __instance.NearbyVents;
        for (int i = 0; i < __instance.Buttons.Length; i++)
        {
            ButtonBehavior buttonBehavior = __instance.Buttons[i];
            Vent vent = nearbyVents[i];
            if (vent)
            {
                __instance.ToggleNeighborVentBeingCleaned(!vent.IsEnabled(), buttonBehavior, __instance.CleaningIndicators[i]);
            }
        }
    }
}
