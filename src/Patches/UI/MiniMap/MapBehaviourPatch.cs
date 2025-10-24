using HarmonyLib;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;
using TheBetterRoles.Roles;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Patches.UI.MiniMap;

[HarmonyPatch(typeof(MapBehaviour))]
internal class MapBehaviourPatch
{
    [HarmonyPatch(nameof(MapBehaviour.Awake))]
    [HarmonyPostfix]
    private static void Awake_Postfix(MapBehaviour __instance)
    {
        if (ReverseMapSystem.IsReverseActive())
        {
            __instance.GetComponent<AspectSize>().DestroyMono();
            __instance.transform.localScale = new Vector3(-1, 1, 1);
            __instance.gameObject.AddComponent<AspectSize>().PercentWidth = 0.95f;
            __instance.GetComponentsInChildren<TextMeshPro>(true).ToList().ForEach(text => text.transform.localScale = new Vector3(-1, 1, 1));
            foreach (var childObj in __instance.infectedOverlay.transform)
            {
                Transform child = childObj.Cast<Transform>();
                if (child != null)
                {
                    child.GetComponentsInChildren<SpriteRenderer>(true).ToList().ForEach(sprite => sprite.transform.localScale = new Vector3(-0.8f, 0.8f, 1));
                }
            }
        }
    }

    [HarmonyPatch(nameof(MapBehaviour.Show))]
    [HarmonyPrefix]
    private static void Show_Prefix(MapBehaviour __instance, ref MapOptions opts)
    {
        if (opts != null && !GameState.IsMeeting && !GameState.IsExilling)
        {
            if (PlayerControl.LocalPlayer.CheckAnyRoles(role => role.CanSabotage && role.RoleButtons.SabotageButton != null && !role.RoleButtons.SabotageButton.Hacked)
                && opts.Mode != MapOptions.Modes.CountOverlay)
            {
                opts.Mode = MapOptions.Modes.Sabotage;
            }
        }
    }

    [HarmonyPatch(nameof(MapBehaviour.ShowNormalMap))]
    [HarmonyPostfix]
    private static void ShowNormalMap_Postfix(MapBehaviour __instance) => __instance.ColorControl.SetColor(new Color(0.05f, 0.6f, 1f, 1f));

    [HarmonyPatch(nameof(MapBehaviour.ShowSabotageMap))]
    [HarmonyPostfix]
    private static void ShowSabotageMap_Postfix(MapBehaviour __instance) => __instance.ColorControl.SetColor(new Color(1f, 0.3f, 0f, 1f));

    [HarmonyPatch(nameof(MapBehaviour.ShowCountOverlay))]
    [HarmonyPostfix]
    private static void ShowCountOverlay_Postfix(MapBehaviour __instance) => __instance.ColorControl.SetColor(new Color(0.2f, 0.5f, 0f, 1f));
}
