using HarmonyLib;
using InnerNet;
using TheBetterRoles.Helpers;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;

namespace TheBetterRoles;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
class OnGameJoinedPatch
{
    public static void Postfix(/*AmongUsClient __instance*/)
    {
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

                if (GameState.IsHost) RPC.SendModRequest();

                /*
                // Auto ban player on ban list
                if (BetterGameSettings.UseBanPlayerList.GetBool())
                {
                    if (player != null)
                    {
                        try
                        {
                            string banPlayerListContent = File.ReadAllText(BetterDataManager.banPlayerListFile);

                            string[] listPlayerArray = banPlayerListContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                            foreach (string text in listPlayerArray)
                            {
                                if (!string.IsNullOrEmpty(player.Data.FriendCode) && text.Contains(player.Data.FriendCode)
                                    || !string.IsNullOrEmpty(Utils.GetHashPuid(player)) && text.Contains(Utils.GetHashPuid(player)))
                                {
                                    player.Kick(true, Translator.GetString("AntiCheat.BanPlayerListMessage"), bypassDataCheck: true);
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                        }
                    }
                }

                if (BetterGameSettings.UseBanNameList.GetBool())
                {
                    try
                    {
                        // Normalize and remove spaces and special characters from names
                        Func<string, string> normalizeName = name => new string(name.Where(c => char.IsLetterOrDigit(c)).ToArray()).ToLower();

                        // Read all banned names into a HashSet with normalized names
                        HashSet<string> bannedNames = new HashSet<string>(
                            File.ReadLines(BetterDataManager.banNameListFile)
                                .Select(normalizeName)
                                .Where(name => !string.IsNullOrWhiteSpace(name))
                        );

                        string normalizedPlayerName = normalizeName(player.Data.PlayerName);

                        // Check if any banned name is a prefix of the player's normalized name
                        bool isNameBanned = bannedNames.Any(bannedName =>
                            normalizedPlayerName.StartsWith(bannedName)
                        );

                        if (!string.IsNullOrEmpty(normalizedPlayerName) && isNameBanned)
                        {
                            player.Kick(false, Translator.GetString("AntiCheat.BanNameListMessage"), bypassDataCheck: true);
                        }
                    }
                    catch { }
                }
                */
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
