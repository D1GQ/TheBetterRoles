using HarmonyLib;
using UnityEngine;

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

            CustomRoleManager.RoleUpdate(__instance);

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

    [HarmonyPatch(typeof(PlayerPhysics))]
    class PlayerPhysicsPatch
    {
        [HarmonyPatch(nameof(PlayerPhysics.HandleAnimation))]
        [HarmonyPrefix]
        public static void HandleAnimation_Prefix(PlayerPhysics __instance, ref bool amDead)
        {
            amDead = amDead && !__instance.myPlayer.BetterData().IsFakeAlive;
        }
    }

    public static void ClearRoleData(PlayerControl player) => player.ClearRoles();
}
