using AmongUs.GameOptions;
using HarmonyLib;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.RPCs;
using UnityEngine;
using static PlayerMaterial;

namespace TheBetterRoles.Patches;

public class RolePatch
{
    public static DeadBodyAbilityButton? ReportButton { get; private set; }

    [HarmonyPatch(typeof(HudManager))]
    class HudManagerRolePatch
    {
        [HarmonyPatch(nameof(HudManager.Start))]
        [HarmonyPostfix]
        public static void Start_Postfix(HudManager __instance)
        {
            ReportButton = new DeadBodyAbilityButton().Create(0, Translator.GetString(StringNames.ReportButton), 0f, 0f, 0, __instance.ReportButton.graphic.sprite, null, true, 4f, 2);
            ReportButton.Text.SetOutlineColor(Color.black);
            ReportButton.ShowHighLight = false;
            ReportButton.VisibleCondition = () => { return !GameState.IsLobby; };
            ReportButton.OnClick = () =>
            {
                if (ReportButton.lastDeadBody != null)
                {
                    var data = Utils.PlayerDataFromPlayerId(ReportButton.lastDeadBody.ParentId);
                    if (data != null)
                    {
                        if (CustomRoleManager.RoleChecks(PlayerControl.LocalPlayer, role => role.CheckBody(ReportButton.lastDeadBody)) == false) return;
                        if (CustomRoleManager.RoleChecksOther(role => role.CheckBodyOther(ReportButton.lastDeadBody)) == false) return;

                        PlayerControl.LocalPlayer.SendRpcReportBody(data);
                    }
                }
            };
        }
    }

    [HarmonyPatch(typeof(PlayerControl))]
    class PlayerControlRolePatch
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
            var betterData = __instance.BetterData();

            __instance.cosmetics.nameText.transform.parent.gameObject.SetActive(betterData.PlayerTextActiveQueue);
            if (betterData.CosmeticsActiveQueue.ValueChanged())
            {
                int z = betterData.CosmeticsActiveQueue ? 0 : 100;
                var pos = __instance.cosmetics.transform.localPosition;
                __instance.cosmetics.transform.localPosition = new Vector3(pos.x, pos.y, z);
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

        [HarmonyPatch(nameof(PlayerControl.CompleteTask))]
        [HarmonyPostfix]
        public static void CompleteTask_Postfix(PlayerControl __instance, [HarmonyArgument(0)] uint idx)
        {
            PlayerTask playerTask = __instance.myTasks.ToArray().FirstOrDefault(p => p.Id == idx);
            if (playerTask)
            {
                if (__instance.IsLocalPlayer()) CustomRoleManager.RoleListener(__instance, role => role.OnTaskComplete(__instance, idx));
                CustomRoleManager.RoleListener(__instance, role => role.OnTaskCompleteOther(__instance, idx));
                __instance?.DirtyName();
            }
        }
    }

    [HarmonyPatch(typeof(CosmeticsLayer))]
    class CosmeticsLayerRolePatch
    {
        [HarmonyPatch(nameof(CosmeticsLayer.SetColor))]
        [HarmonyPrefix]
        public static bool RawSetColor_Prefix(CosmeticsLayer __instance, int color)
        {
            var player = Main.AllPlayerControls.FirstOrDefault(pc => pc.cosmetics == __instance);
            if (player == null) return true;
            if (!player.BetterData().CamouflagedQueue)
            {
                player.BetterData().CamouflageBackToColor = color;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerPhysics))]
    class PlayerPhysicsRolePatch
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
                __instance.myPlayer.Visible = __instance.myPlayer.Visible;
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
    class VentRolePatch
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

    // Set hand color during camouflage comms
    [HarmonyPatch(typeof(ZiplineBehaviour))]
    class ZiplineBehaviourRolePatch
    {
        [HarmonyPatch(nameof(ZiplineBehaviour.CoAnimatePlayerJumpingOnToZipline))]
        [HarmonyPostfix]
        public static void CoAnimatePlayerJumpingOnToZipline_Postfix(/*ZiplineBehaviour __instance,*/ [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(2)] HandZiplinePoolable hand)
        {
            SetColors(player.cosmetics.bodyMatProperties.ColorId, hand.handRenderer);
        }
    }

    public static void ClearRoleData(PlayerControl player) => player.ClearRoles();
}
