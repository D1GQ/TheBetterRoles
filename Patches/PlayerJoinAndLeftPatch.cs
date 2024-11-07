using HarmonyLib;
using InnerNet;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using TheBetterRoles.RPCs;

namespace TheBetterRoles;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
class OnGameJoinedPatch
{
    public static void Postfix(/*AmongUsClient __instance*/)
    {
        BetterDataManager.HostSettings.Clear();

        try
        {
            // Fix host icon in lobby on modded servers
            if (!GameState.IsVanillaServer)
            {
                var host = AmongUsClient.Instance.GetHost().Character;
                host.SetColor(-2);
                host.SetColor(host.CurrentOutfit.ColorId);
            }

            Logger.Log($"Successfully joined {GameCode.IntToGameName(AmongUsClient.Instance.GameId)}", "OnGameJoinedPatch");
        }
        catch { };
    }
}
[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
public static class OnPlayerJoinedPatch
{
    public static void Postfix(/*AmongUsClient __instance,*/ [HarmonyArgument(0)] ClientData client)
    {
        _ = new LateTask(() =>
        {
            if (GameState.IsInGame)
            {
                var player = Utils.PlayerFromClientId(client.Id);

                if (GameState.IsHost) PlayerControl.LocalPlayer?.SendVersionRequest(Main.GetVersionText());
            }
        }, 2.5f, "OnPlayerJoinedPatch", false);
    }
}
[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
class OnPlayerLeftPatch
{
    public static void Postfix(/*AmongUsClient __instance,*/ [HarmonyArgument(0)] ClientData data, [HarmonyArgument(1)] DisconnectReasons reason)
    {
    }
}

[HarmonyPatch(typeof(GameData))]
[HarmonyPatch("HandleDisconnect")]
[HarmonyPatch(MethodType.Normal)]
[HarmonyPatch(new Type[] { typeof(PlayerControl), typeof(DisconnectReasons) })]
class GameDataHandleDisconnectPatch
{
    public static void Prefix(/*GameData __instance,*/ [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] DisconnectReasons reason)
    {
        CustomRoleManager.RoleListenerOther(role => role.OnDisconnect(player, reason));
        CustomRoleManager.RoleListener(player, role => role.OnResetAbilityState(false));
        player.BetterData().DisconnectReason = reason;
        RolePatch.ClearRoleData(player);

        GameDataShowNotificationPatch.BetterShowNotification(player.Data, reason);
    }
}

[HarmonyPatch(typeof(GameData), nameof(GameData.ShowNotification))]
class GameDataShowNotificationPatch
{
    public static void BetterShowNotification(NetworkedPlayerInfo playerData, DisconnectReasons reason = DisconnectReasons.Unknown, string forceReasonText = "")
    {
        if (playerData.BetterData().HasShowDcMsg) return;
        playerData.BetterData().HasShowDcMsg = true;

        string? playerName = playerData.BetterData().RealName;

        if (forceReasonText != "")
        {
            var ReasonText = $"<color=#ff0>{playerData.BetterData().RealName}</color> {forceReasonText}";

            Logger.Log(ReasonText);

            DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage(ReasonText);
        }
        else
        {
            string ReasonText;

            switch (reason)
            {
                case DisconnectReasons.ExitGame:
                    ReasonText = string.Format(Translator.GetString("DisconnectReason.Left"), playerName);
                    break;
                case DisconnectReasons.ClientTimeout:
                    ReasonText = string.Format(Translator.GetString("DisconnectReason.Disconnect"), playerName);
                    break;
                case DisconnectReasons.Kicked:
                    ReasonText = string.Format(Translator.GetString("DisconnectReason.Kicked"), playerName, AmongUsClient.Instance.GetHost().Character.Data.PlayerName);
                    break;
                case DisconnectReasons.Banned:
                    ReasonText = string.Format(Translator.GetString("DisconnectReason.Banned"), playerName, AmongUsClient.Instance.GetHost().Character.Data.PlayerName);
                    break;
                case DisconnectReasons.Hacking:
                    ReasonText = string.Format(Translator.GetString("DisconnectReason.Cheater"), playerName);
                    break;
                case DisconnectReasons.Error:
                    ReasonText = string.Format(Translator.GetString("DisconnectReason.Error"), playerName);
                    break;
                case DisconnectReasons.Unknown:
                    ReasonText = string.Format(Translator.GetString("DisconnectReason.Unknown"), playerName);
                    break;
                default:
                    ReasonText = string.Format(Translator.GetString("DisconnectReason.Left"), playerName);
                    break;
            }

            Logger.Log(ReasonText);

            DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage(ReasonText);
        }
    }

    public static bool Prefix()
    {
        return false;
    }
}
