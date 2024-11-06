using AmongUs.GameOptions;
using HarmonyLib;
using TheBetterRoles.Helpers;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using UnityEngine;

namespace TheBetterRoles.Patches;

public class RolePatch
{
    [HarmonyPatch(typeof(PlayerControl))]
    class PlayerControlPatch
    {
        [HarmonyPatch(nameof(PlayerControl.FixedUpdate))]
        [HarmonyPrefix]
        public static void FixedUpdate_Prefix(PlayerControl __instance)
        {
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
                    PlayerControl.LocalPlayer.SendRpcPlayerPress(__instance);
                }));
            }
        }
    }

    [HarmonyPatch(typeof(PlayerPhysics))]
    class PlayerPhysicsPatch
    {
        [HarmonyPatch(nameof(PlayerPhysics.ResetAnimState))]
        [HarmonyPrefix]
        public static bool ResetAnimState_Prefix(PlayerPhysics __instance)
        {
            __instance.myPlayer.FootSteps.Stop();
            __instance.myPlayer.FootSteps.loop = false;
            __instance.myPlayer.cosmetics.SetHatAndVisorIdle(__instance.myPlayer.CurrentOutfit.ColorId);
            NetworkedPlayerInfo data = __instance.myPlayer.Data;
            if (data == null || !data.IsDead || data.BetterData().IsFakeAlive)
            {
                __instance.myPlayer.cosmetics.AnimateSkinIdle();
                __instance.Animations.PlayIdleAnimation();
                __instance.myPlayer.Visible = true;
                __instance.myPlayer.SetHatAndVisorAlpha(1f);
                return false;
            }
            __instance.myPlayer.cosmetics.SetGhost();
            if (data.Role != null)
            {
                if (data.Role.Role == RoleTypes.GuardianAngel)
                {
                    __instance.Animations.PlayGuardianAngelIdleAnimation();
                }
                else
                {
                    __instance.Animations.PlayGhostIdleAnimation();
                }
            }
            __instance.myPlayer.SetHatAndVisorAlpha(0.5f);

            return false;
        }
    }

    [HarmonyPatch(typeof(Vent))]
    class VentPatch
    {
        [HarmonyPatch(nameof(Vent.SetButtons))]
        [HarmonyPostfix]
        public static void SetButtons_Postfix(Vent __instance)
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
        public static void UpdateArrows_Postfix(Vent __instance)
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

    public static void ClearRoleData(PlayerControl player) => player.ClearRoles();
}
