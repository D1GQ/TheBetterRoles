using HarmonyLib;
using InnerNet;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace TheBetterRoles.Patches;

class RolePatch
{
    [HarmonyPatch(typeof(PlayerControl))]
    class PlayerControlPatch
    {
        [HarmonyPatch(nameof(PlayerControl.FixedUpdate))]
        [HarmonyPrefix]
        public static void FixedUpdate_Prefix(PlayerControl __instance)
        {
            if (__instance?.BetterData()?.RoleInfo?.RoleAssigned == true)
            {
                __instance.BetterData().RoleInfo.Role.Update();
            }

            if (__instance.IsLocalPlayer())
            {
                foreach (var button in __instance.BetterData().RoleInfo.Role.Buttons)
                {
                    button.Update();
                }
            }
        }

        [HarmonyPatch(nameof(PlayerControl.Awake))]
        [HarmonyPostfix]
        public static void Awake_Postfix(PlayerControl __instance)
        {
            var box = __instance.gameObject.GetComponent<BoxCollider2D>();
            if (box != null)
            {
                box.size = new Vector2(0.8f, 1f);
            }
            var passiveButton = __instance.gameObject.GetComponent<PassiveButton>();
            if (passiveButton != null)
            {
                passiveButton.OnClick.AddListener((Action)(() =>
                {
                    PlayerControl.LocalPlayer.PlayerPressSync(__instance);
                }));
            }
        }
    }

    public static void ClearRoleData(PlayerControl player) => player.ClearRoles();
}
