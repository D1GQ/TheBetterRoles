using HarmonyLib;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;
using UnityEngine;


namespace TheBetterRoles.Patches;

public class MiniMapBehaviourPatch
{
    [HarmonyPatch(typeof(MapBehaviour))]
    class MapBehaviourPatch
    {
        [HarmonyPatch(nameof(MapBehaviour.Show))]
        [HarmonyPrefix]
        public static void Show_Prefix(/*MapBehaviour __instance,*/ ref MapOptions opts)
        {
            if (opts != null && !GameState.IsMeeting && !GameState.IsExilling)
            {
                if (PlayerControl.LocalPlayer.RoleAssigned())
                {
                    if (PlayerControl.LocalPlayer.BetterData().RoleInfo.Role.CanSabotage && opts.Mode != MapOptions.Modes.CountOverlay)
                    {
                        opts.Mode = MapOptions.Modes.Sabotage;
                    }
                }
            }
        }
        [HarmonyPatch(nameof(MapBehaviour.ShowNormalMap))]
        [HarmonyPostfix]
        public static void ShowNormalMap_Postfix(MapBehaviour __instance) => __instance.ColorControl.SetColor(new Color(0.05f, 0.6f, 1f, 1f));
        [HarmonyPatch(nameof(MapBehaviour.ShowSabotageMap))]
        [HarmonyPostfix]
        public static void ShowSabotageMap_Postfix(MapBehaviour __instance) => __instance.ColorControl.SetColor(new Color(1f, 0.3f, 0f, 1f));
        [HarmonyPatch(nameof(MapBehaviour.ShowCountOverlay))]
        [HarmonyPostfix]
        public static void ShowCountOverlay_Postfix(MapBehaviour __instance) => __instance.ColorControl.SetColor(new Color(0.2f, 0.5f, 0f, 1f));
    }

    [HarmonyPatch(typeof(MapConsole))]
    class MapConsolePatch
    {
        [HarmonyPatch(nameof(MapConsole.Use))]
        [HarmonyPostfix]
        public static void ShowCountOverlay_Postfix() => MapBehaviour.Instance.ColorControl.SetColor(new Color(0.2f, 0.5f, 0f, 1f));
    }
}
