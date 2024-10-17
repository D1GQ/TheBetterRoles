using HarmonyLib;
using System.Text;
using TMPro;
using UnityEngine;

namespace TheBetterRoles
{
    [HarmonyPatch(typeof(MeetingHud))]
    class MeetingHudPatche
    {
        [HarmonyPatch(nameof(MeetingHud.Start))]
        [HarmonyPostfix]
        public static void StartPostfix(MeetingHud __instance)
        {
            foreach (var pva in __instance.playerStates)
            {
                var TextTopMeeting = UnityEngine.Object.Instantiate(pva.NameText, pva.NameText.transform);
                TextTopMeeting.gameObject.name = "TextTop";
                TextTopMeeting.DestroyChildren();
                TextTopMeeting.transform.position = pva.NameText.transform.position;
                TextTopMeeting.transform.position += new Vector3(0f, 0.16f);
                TextTopMeeting.GetComponent<TextMeshPro>().text = "";

                var TextInfoMeeting = UnityEngine.Object.Instantiate(pva.NameText, pva.NameText.transform);
                TextInfoMeeting.gameObject.name = "TextInfo";
                TextInfoMeeting.DestroyChildren();
                TextInfoMeeting.transform.position = pva.NameText.transform.position;
                TextInfoMeeting.transform.position += new Vector3(0f, -0.16f);
                TextInfoMeeting.GetComponent<TextMeshPro>().text = "";

                var PlayerLevel = pva.transform.Find("PlayerLevel");
                PlayerLevel.localPosition = new Vector3(PlayerLevel.localPosition.x, PlayerLevel.localPosition.y, -2f);
                var LevelDisplay = UnityEngine.Object.Instantiate(PlayerLevel, pva.transform);
                LevelDisplay.transform.SetSiblingIndex(pva.transform.Find("PlayerLevel").GetSiblingIndex() + 1);
                LevelDisplay.gameObject.name = "PlayerId";
                LevelDisplay.GetComponent<SpriteRenderer>().color = new Color(1f, 0f, 1f, 1f);
                var IdLabel = LevelDisplay.transform.Find("LevelLabel");
                var IdNumber = LevelDisplay.transform.Find("LevelNumber");
                UnityEngine.Object.Destroy(IdLabel.GetComponent<TextTranslatorTMP>());
                IdLabel.GetComponent<TextMeshPro>().text = "ID";
                IdNumber.GetComponent<TextMeshPro>().text = pva.TargetPlayerId.ToString();
                IdLabel.name = "IdLabel";
                IdNumber.name = "IdNumber";
                PlayerLevel.transform.position += new Vector3(0.23f, 0f);
            }

            Logger.LogHeader("Meeting Has Started");
        }

        [HarmonyPatch(nameof(MeetingHud.Update))]
        [HarmonyPostfix]
        public static void UpdatePostfix(MeetingHud __instance)
        {
            if (__instance.playerStates == null) return;

            foreach (var pva in __instance.playerStates)
            {
                if (pva == null) continue;

                if (pva.ColorBlindName != null && pva.ColorBlindName.isActiveAndEnabled)
                {
                    pva.ColorBlindName.transform.localPosition = new Vector3(-0.91f, -0.19f, -0.05f);
                    pva.ColorBlindName.color = Palette.PlayerColors[pva.PlayerIcon.ColorId];
                    pva.ColorBlindName.outlineWidth = 0.2745f;
                }

                if (pva.NameText == null) continue;

                TextMeshPro? TopText = pva.NameText.transform.Find("TextTop")?.gameObject?.GetComponent<TextMeshPro>();
                TextMeshPro? InfoText = pva.NameText.transform.Find("TextInfo")?.gameObject?.GetComponent<TextMeshPro>();

                var playerData = GameData.Instance?.GetPlayerById(pva.TargetPlayerId);
                if (playerData == null) continue;

                bool flag = Main.AllPlayerControls.Any(player => player.PlayerId == pva.TargetPlayerId);
                PlayerControl? player = Utils.PlayerFromPlayerId(pva.TargetPlayerId);

                if (!flag)
                {
                    string DisconnectText;
                    switch (playerData.BetterData().DisconnectReason)
                    {
                        case DisconnectReasons.ExitGame:
                            DisconnectText = Translator.GetString("DisconnectReasonMeeting.Left");
                            break;
                        case DisconnectReasons.Banned:
                            DisconnectText = Translator.GetString("DisconnectReasonMeeting.Banned");
                            break;
                        case DisconnectReasons.Kicked:
                            DisconnectText = Translator.GetString("DisconnectReasonMeeting.Kicked");
                            break;
                        case DisconnectReasons.Hacking:
                            DisconnectText = Translator.GetString("DisconnectReasonMeeting.Cheater");
                            break;
                        default:
                            DisconnectText = Translator.GetString("DisconnectReasonMeeting.Disconnect");
                            break;
                    }

                    pva.NameText.text = playerData.PlayerName;
                    SetPlayerTextInfoMeeting(pva, "", isInfo: true);
                    SetPlayerTextInfoMeeting(pva, $"<color=#6b6b6b>{DisconnectText}</color>");
                    pva.transform.Find("votePlayerBase")?.gameObject?.SetActive(false);
                    pva.transform.Find("deadX_border")?.gameObject?.SetActive(false);
                    pva.ClearForResults();
                    pva.SetDisabled();
                }
                else if (TopText != null && InfoText != null)
                {
                    if (player == null || player.BetterData() == null) continue;

                    var sbTagTop = new StringBuilder();
                    var sbTag = new StringBuilder();

                    if (!string.IsNullOrEmpty(player.BetterData().NameColor))
                    {
                        pva.NameText.color = Utils.HexToColor32(player.BetterData().NameColor);
                    }
                    else
                    {
                        pva.NameText.color = new Color(1f, 1f, 1f, 1f);
                    }

                    if (player.IsLocalPlayer() || !PlayerControl.LocalPlayer.IsAlive(true) || player.IsImpostorTeammate() || CustomRoleManager.RoleChecksAny(PlayerControl.LocalPlayer, role => role.RevealPlayerRole(player)))
                    {
                        sbTag.Append($"{player.GetRoleNameAndColor()}---");
                    }

                    if (player.IsLocalPlayer() || !PlayerControl.LocalPlayer.IsAlive(true) || player.IsImpostorTeammate() || CustomRoleManager.RoleChecksAny(PlayerControl.LocalPlayer, role => role.RevealPlayerAddons(player)))
                    {
                        foreach (var addon in player.BetterData().RoleInfo.Addons)
                        {
                            sbTagTop.Append($"<size=55%><color={addon.RoleColor}>{addon.RoleName}</color></size>+++");
                        }
                    }

                    sbTagTop = Utils.FormatStringBuilder(sbTagTop);
                    sbTag = Utils.FormatStringBuilder(sbTag);

                    pva.NameText.text = Utils.FormatPlayerName(player.Data);
                    SetPlayerTextInfoMeeting(pva, sbTagTop.ToString(), true);
                    SetPlayerTextInfoMeeting(pva, sbTag.ToString());
                }
            }
        }

        private static void SetPlayerTextInfoMeeting(PlayerVoteArea pva, string text, bool isInfo = false)
        {
            string InfoType = "TextTop";
            if (isInfo)
            {
                InfoType = "TextInfo";
            }

            text = "<size=65%>" + text + "</size>";
            GameObject? TextObj = pva.NameText.transform.Find(InfoType)?.gameObject;
            if (TextObj != null)
            {
                TextObj.GetComponent<TextMeshPro>().text = text;
            }
        }

        [HarmonyPatch(nameof(MeetingHud.VotingComplete))]
        [HarmonyPrefix]
        public static void VotingCompletePrefix(MeetingHud __instance, [HarmonyArgument(0)] MeetingHud.VoterState[] states, [HarmonyArgument(1)] NetworkedPlayerInfo exiled, [HarmonyArgument(2)] bool tie)
        {
            CustomRoleManager.RoleListenerOther(role => role.OnVotingComplete(states, exiled, tie));
        }

        [HarmonyPatch(nameof(MeetingHud.OnDestroy))]
        [HarmonyPostfix]
        public static void OnDestroyPostfix()
        {
            Logger.LogHeader("Meeting Has Endded");
        }
    }
}
