using HarmonyLib;
using InnerNet;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace TheBetterRoles.Patches;

public class RolePatch
{
    public static void Update()
    {
        if (!GameStates.IsInGamePlay && CustomRoleBehavior.SubTeam.Count > 0)
        {
            CustomRoleBehavior.SubTeam.Clear();
        }
    }

    [HarmonyPatch(typeof(PlayerControl))]
    class PlayerControlPatch
    {
        [HarmonyPatch(nameof(PlayerControl.FixedUpdate))]
        [HarmonyPrefix]
        public static void FixedUpdate_Prefix(PlayerControl __instance)
        {
            if (__instance.RoleAssigned())
            {
                CustomRoleManager.RoleUpdate(__instance);
            }

            var box = __instance.gameObject.GetComponent<BoxCollider2D>();
            if (box != null)
            {
                box.enabled = true;
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
