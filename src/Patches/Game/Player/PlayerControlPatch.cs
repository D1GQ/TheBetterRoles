using AmongUs.GameOptions;
using HarmonyLib;
using System.Text;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Modules;
using TheBetterRoles.Monos;
using TheBetterRoles.Network;
using TheBetterRoles.Network.RPCs;
using TheBetterRoles.Roles;
using TheBetterRoles.Roles.Interfaces;
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
        if (box != null)
        {
            box.size = new Vector2(0.8f, 1f);
        }

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
        SetPlayerInfo(__instance);
        __instance.UpdateColorBlindTextPosition();

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
        if (box != null)
        {
            box.enabled = true;
        }
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

    private static void SetPlayerInfo(PlayerControl player)
    {
        if (player?.Data == null || player?.ExtendedData()?.DirtyName <= 0 || GameState.IsMeeting) return;

        var extendedData = player.ExtendedData();
        if (extendedData == null) return;
        var userData = extendedData.MyUserData;
        extendedData.DirtyName -= 1;
        var cosmetics = player.cosmetics;

        var sbTag = new StringBuilder();
        var sbTagTop = new StringBuilder();
        var sbTagBottom = new StringBuilder();

        bool isLobbyState = GameState.IsLobby && !GameState.IsFreePlay;
        bool isLocalPlayerAlive = PlayerControl.LocalPlayer.IsAlive();
        bool isLocalPlayer = player.IsLocalPlayer();

        if (isLobbyState)
        {
            if (userData != null)
            {
                var playerTag = !string.IsNullOrEmpty(userData.OverheadColor) ? $"<{userData.OverheadColor}>{userData.OverheadTag}</color>" : userData.OverheadTag;
                if (!string.IsNullOrEmpty(playerTag))
                {
                    sbTagTop.Append($"{playerTag}---");
                }
            }
            cosmetics.nameText.color = extendedData.HasMod || isLocalPlayer
                ? new UnityEngine.Color(0.47f, 1f, 0.95f, 1f)
                : UnityEngine.Color.white;
        }
        else
        {
            bool hideInfo = PlayerControl.LocalPlayer.CheckAnyRoles(role => role.HidePlayerInfoOther(player));

            bool canRevealDeath = (!isLocalPlayerAlive && !PlayerControl.LocalPlayer.IsGhostRole() ||
                     PlayerControl.LocalPlayer.CheckAnyRoles(role => role.RevealPlayerDeath(player))) && !player.IsAlive();

            if (canRevealDeath && !hideInfo)
            {
                sbTagBottom.Append($"{player.FormatDeathReason()}---");
            }

            bool canRevealRole = isLocalPlayer || !isLocalPlayerAlive || player.IsImpostorTeammate() ||
                                 PlayerControl.LocalPlayer.CheckAnyRoles(role => role.RevealPlayerRole(player));

            string hexColor = "";

            if (canRevealRole && !hideInfo && player.Role() != null && player.Role().ShowRoleAboveName)
            {
                hexColor = player.Role()?.RoleColorHex ?? "#FFFFFF";
                sbTag.Append($"{player.Role()?.RoleNameAndAbilityAmount}{player.FormatTasksToText()}---");
            }

            bool canRevealAddons = isLocalPlayer || !isLocalPlayerAlive || player.IsImpostorTeammate() ||
                                   PlayerControl.LocalPlayer.CheckAllRoles(role => role.RevealPlayerAddons(player));

            if (canRevealAddons && !hideInfo)
            {
                foreach (var addon in extendedData.RoleInfo.Addons)
                {
                    if (!addon.ShowRoleAboveName) continue;
                    sbTagTop.Append($"<size=55%>{addon.RoleNameAndAbilityAmount}</size>+++");
                }
            }

            if (!string.IsNullOrEmpty(extendedData.NameColor))
            {
                hexColor = extendedData.NameColor;
            }

            if (!string.IsNullOrEmpty(hexColor))
            {
                var color = Colors.HexToColor(hexColor);
                cosmetics.nameText.color = new Color(color.r, color.g, color.b, cosmetics.nameText.color.a);
            }
            else
            {
                cosmetics.nameText.color = new Color(1f, 1f, 1f, cosmetics.nameText.color.a);
            }
        }

        // Format and set the strings
        player.RawSetName(Utils.FormatPlayerName(player));
        player.SetPlayerTextInfo(Utils.FormatStringBuilder(sbTagTop).ToString());
        player.SetPlayerTextInfo(Utils.FormatStringBuilder(sbTagBottom).ToString(), isBottom: true);
        player.SetPlayerTextInfo(Utils.FormatStringBuilder(sbTag).ToString(), isInfo: true);
    }

    [HarmonyPatch(nameof(PlayerControl.CompleteTask))]
    [HarmonyPostfix]
    internal static void CompleteTask_Postfix(PlayerControl __instance, [HarmonyArgument(0)] uint idx)
    {
        PlayerTask playerTask = __instance.myTasks.ToArray().FirstOrDefault(p => p.Id == idx);
        if (playerTask)
        {
            if (__instance.IsLocalPlayer()) RoleListener.InvokeRoles<IRoleTaskAction>(role => role.TaskComplete(__instance, idx), player: __instance);
            RoleListener.InvokeRoles<IRoleTaskAction>(role => role.TaskCompleteOther(__instance, idx));
            __instance?.DirtyName();
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
        __instance.DirtyName();
    }
}
