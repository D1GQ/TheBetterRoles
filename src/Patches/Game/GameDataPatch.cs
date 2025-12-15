using HarmonyLib;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Network;
using TheBetterRoles.Roles;
using TheBetterRoles.Roles.Core.Interfaces;
using UnityEngine;

namespace TheBetterRoles.Patches.Game;

[HarmonyPatch(typeof(GameData))]
internal class GameDataPatch
{
    [HarmonyPatch(nameof(GameData.Awake))]
    [HarmonyPrefix]
    private static void Awake_Prefix()
    {
        var obj = new GameObject("CatchedGameData(TBR)");
        obj.AddComponent<CatchedGameData>();
    }

    [HarmonyPatch(nameof(GameData.HandleDisconnect))]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPatch(new Type[] { typeof(PlayerControl), typeof(DisconnectReasons) })]
    [HarmonyPrefix]
    private static void HandleDisconnect_Prefix([HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] DisconnectReasons reason)
    {
        RoleListener.InvokeRoles<IRoleDisconnectAction>(role => role.OnDisconnect(player, reason));
        player.InvokeRoles<IRoleAbilityAction>(role => role.OnResetAbilityState(false));
        CatchedGameData.Instance?.CurrentGameMode?.OnDisconnect(player);
        player.ExtendedData().DisconnectReason = reason;
        player.ClearRoles();

        NewShowNotification(player.Data, reason);
    }

    internal static void NewShowNotification(NetworkedPlayerInfo playerData, DisconnectReasons reason = DisconnectReasons.Unknown, string forceReasonText = "")
    {
        if (playerData.ExtendedData().HasShowDcMsg) return;
        playerData.ExtendedData().HasShowDcMsg = true;

        string playerName = playerData.ExtendedData().RealName ?? string.Empty;

        if (forceReasonText != "")
        {
            var ReasonText = $"<color=#ff0>{playerData.ExtendedData().RealName}</color> {forceReasonText}";

            Logger.Log(ReasonText);

            HudManager.Instance.Notifier.AddDisconnectMessage(ReasonText);
        }
        else
        {
            string ReasonText;

            switch (reason)
            {
                case DisconnectReasons.ExitGame:
                    ReasonText = Translator.GetString("DisconnectReason.Left", [playerName]);
                    break;
                case DisconnectReasons.ClientTimeout:
                    ReasonText = Translator.GetString("DisconnectReason.Disconnect", [playerName]);
                    break;
                case DisconnectReasons.Kicked:
                    ReasonText = Translator.GetString("DisconnectReason.Kicked", [playerName, AmongUsClient.Instance.GetHost().Character.Data.PlayerName]);
                    break;
                case DisconnectReasons.Banned:
                    ReasonText = Translator.GetString("DisconnectReason.Banned", [playerName, AmongUsClient.Instance.GetHost().Character.Data.PlayerName]);
                    break;
                case DisconnectReasons.Hacking:
                    ReasonText = Translator.GetString("DisconnectReason.Cheater", [playerName]);
                    break;
                case DisconnectReasons.Error:
                    ReasonText = Translator.GetString("DisconnectReason.Error", [playerName]);
                    break;
                case DisconnectReasons.Unknown:
                    ReasonText = Translator.GetString("DisconnectReason.Unknown", [playerName]);
                    break;
                default:
                    ReasonText = Translator.GetString("DisconnectReason.Left", [playerName]);
                    break;
            }

            Logger.Log(ReasonText);

            HudManager.Instance.Notifier.AddDisconnectMessage(ReasonText);
        }
    }

    [HarmonyPatch(nameof(GameData.ShowNotification))]
    [HarmonyPrefix]
    private static bool ShowNotification_Prefix() => false;
}