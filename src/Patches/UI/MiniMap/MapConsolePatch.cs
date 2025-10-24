using HarmonyLib;
using UnityEngine;

namespace TheBetterRoles.Patches.UI.MiniMap;

[HarmonyPatch(typeof(MapConsole))]
internal class MapConsolePatch
{
    [HarmonyPatch(nameof(MapConsole.Use))]
    [HarmonyPostfix]
    private static void ShowCountOverlay_Postfix() => MapBehaviour.Instance.ColorControl.SetColor(new Color(0.2f, 0.5f, 0f, 1f));
}