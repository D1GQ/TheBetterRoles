using HarmonyLib;
using TheBetterRoles.Helpers;
using UnityEngine;

namespace TheBetterRoles.Patches.Game;

[HarmonyPatch(typeof(LobbyInfoPane))]
internal class LobbyInfoPanePatch
{
    [HarmonyPatch(nameof(LobbyInfoPane.Update))]
    [HarmonyPostfix]
    private static void Update_Postfix(LobbyInfoPane __instance)
    {
        if (__instance.EditButton != null)
        {
            var text = __instance.transform.Find("AspectSize/GameSettingsButtons/ButtonSettingsHeader");
            if (text != null)
            {
                text.gameObject.DestroyObj();
            }
            var Background = __instance.transform.Find("AspectSize/Background");
            if (Background != null)
            {
                var scale = Background.transform.localScale;
                float offset = 0.15f;
                Background.transform.localScale = scale - new Vector3(0f, offset, 0f);
                var position = Background.transform.localPosition + new Vector3(0f, offset * 2.2f, 0f); ;
                Background.transform.localPosition = position;
            }
            __instance.EditButton.DestroyObj();
        }

        if (__instance.HostViewButton != null)
        {
            __instance.HostViewButton.DestroyObj();
        }

        if (__instance.ClientViewButton != null)
        {
            __instance.ClientViewButton.DestroyObj();
        }
    }
}
