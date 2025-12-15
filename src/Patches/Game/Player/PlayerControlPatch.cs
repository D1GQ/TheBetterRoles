using AmongUs.GameOptions;
using HarmonyLib;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Modules;
using TheBetterRoles.Monos;
using TheBetterRoles.Network;
using TheBetterRoles.Network.RPCs;
using TheBetterRoles.Roles;
using TheBetterRoles.Roles.Core.Interfaces;
using UnityEngine;

namespace TheBetterRoles.Patches.Game.Player;

[HarmonyPatch(typeof(PlayerControl))]
internal class PlayerControlPatch
{
    [HarmonyPatch(nameof(PlayerControl.Awake))]
    [HarmonyPostfix]
    internal static void Awake_Postfix(PlayerControl __instance)
    {
        var box = __instance.gameObject.GetComponent<BoxCollider2D>();
        box?.size = new Vector2(0.8f, 1f);

        var passiveButton = __instance.gameObject.GetComponent<PassiveButton>();
        if (passiveButton != null)
        {
            passiveButton.OnClick.AddListener((Action)(() =>
            {
                if (GameManager.Instance.GameHasStarted)
                {
                    PlayerControl.LocalPlayer.SendRpcPlayerPress(__instance);
                }
            }));
        }
    }

    [HarmonyPatch(nameof(PlayerControl.Start))]
    [HarmonyPostfix]
    internal static void Start_Postfix(PlayerControl __instance)
    {
        Main.AllPlayerControls.Add(__instance);
    }

    [HarmonyPatch(nameof(PlayerControl.OnDestroy))]
    [HarmonyPostfix]
    internal static void OnDestroy_Postfix(PlayerControl __instance)
    {
        Main.AllPlayerControls.Remove(__instance);
    }

    [HarmonyPatch(nameof(PlayerControl.FixedUpdate))]
    [HarmonyPrefix]
    private static void FixedUpdate_Prefix(PlayerControl __instance)
    {
        var extendedPc = __instance.ExtendedPC();
        if (extendedPc != null)
        {
            __instance.cosmetics.nameText.transform.parent.gameObject.SetActive(extendedPc.PlayerTextActiveQueue);
            if (extendedPc.CosmeticsActiveQueue.ValueChanged())
            {
                int z = extendedPc.CosmeticsActiveQueue ? 0 : 100;
                var pos = __instance.cosmetics.transform.localPosition;
                __instance.cosmetics.transform.localPosition = new Vector3(pos.x, pos.y, z);
            }
        }

        var box = __instance.gameObject.GetComponent<BoxCollider2D>();
        box?.enabled = true;
    }

    [HarmonyPatch(nameof(PlayerControl.RawSetColor))]
    [HarmonyPrefix]
    private static bool RawSetColor_Prefix(PlayerControl __instance, int bodyColor)
    {
        var extendedPc = __instance.ExtendedPC();
        if (extendedPc != null)
        {
            if (!extendedPc.CamouflagedQueue)
            {
                extendedPc.CamouflageBackToColor = bodyColor;
                return false;
            }
        }

        return true;
    }

    [HarmonyPatch(nameof(PlayerControl.CompleteTask))]
    [HarmonyPostfix]
    internal static void CompleteTask_Postfix(PlayerControl __instance, [HarmonyArgument(0)] uint idx)
    {
        PlayerTask playerTask = __instance.myTasks.FirstOrDefaultIL2CPP(p => p.Id == idx);
        if (playerTask)
        {
            if (__instance.IsLocalPlayer()) RoleListener.InvokeRoles<IRoleTaskAction>(role => role.TaskComplete(__instance, idx), player: __instance);
            RoleListener.InvokeRoles<IRoleTaskAction>(role => role.TaskCompleteOther(__instance, idx));
            __instance?.UpdateName();
        }
    }

    [HarmonyPatch(nameof(PlayerControl.RpcSendChat))]
    [HarmonyPrefix]
    internal static bool RpcSendChat_Prefix(PlayerControl __instance, [HarmonyArgument(0)] string chatText)
    {
        __instance.SendRpcChatMsg(chatText);
        return false;
    }

    [HarmonyPatch(nameof(PlayerControl.Exiled))]
    [HarmonyPrefix]
    internal static bool Exiled_Prefix(PlayerControl __instance)
    {
        __instance.CustomExiled(true);
        return false;
    }

    [HarmonyPatch(nameof(PlayerControl.Die))]
    [HarmonyPostfix]
    internal static void Die_Postfix(PlayerControl __instance)
    {
        __instance.RawSetRole(RoleTypes.CrewmateGhost);
        if (GameState.IsMeeting)
        {
            PlayerVoteAreaButton.UpdateAllButtonStates();
        }
        __instance.UpdateName();
    }
}
