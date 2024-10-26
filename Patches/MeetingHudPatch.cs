using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using System.Collections;
using System.Text;
using TheBetterRoles.Patches;
using TMPro;
using UnityEngine;
using static Il2CppSystem.Linq.Expressions.Interpreter.CastInstruction.CastInstructionNoT;

namespace TheBetterRoles
{
    [HarmonyPatch(typeof(MeetingHud))]
    class MeetingHudPatch
    {
        public static void AdjustVotesOnGuess(PlayerControl pc)
        {
            PlayerVoteArea voteArea = MeetingHud.Instance.playerStates.First(
                x => x.TargetPlayerId == pc.PlayerId
            );
            if (voteArea == null) return;
            if (voteArea.DidVote) voteArea.UnsetVote();
            voteArea.AmDead = true;
            voteArea.Overlay.gameObject.SetActive(true);
            voteArea.Overlay.color = Color.white;
            voteArea.XMark.gameObject.SetActive(true);
            voteArea.XMark.transform.localScale = Vector3.one;
            voteArea.ThumbsDown.gameObject.SetActive(false);
            var votes = voteArea.GetComponentInChildren<VoteSpreader>().Votes;
            foreach (var item in votes)
            {
                item.gameObject.DestroyObj();
            }
            foreach (var playerVoteArea in MeetingHud.Instance.playerStates)
            {
                if (playerVoteArea.VotedFor != pc.PlayerId) continue;
                playerVoteArea.UnsetVote();
                var voteAreaPlayer = Utils.PlayerFromPlayerId(playerVoteArea.TargetPlayerId);
                if (!voteAreaPlayer.AmOwner) continue;
                MeetingHud.Instance.ClearVote();
            }
            if (MeetingHud.Instance.state is MeetingHud.VoteStates.Discussion or MeetingHud.VoteStates.NotVoted or MeetingHud.VoteStates.Voted)
            {
                MeetingHud.Instance.CheckForEndVoting();
            }
        }

        [HarmonyPatch(nameof(MeetingHud.Start))]
        [HarmonyPostfix]
        public static void StartPostfix(MeetingHud __instance)
        {
            PlayerMeetingButton.AllButtons.Clear();

            var Guess = new PlayerMeetingButton().Create("Guess", sprite: Utils.LoadSprite($"TheBetterRoles.Resources.Images.Icons.TargetIcon.png", 100));
            Guess.ClickAction = (PassiveButton? button, PlayerVoteArea? pva, NetworkedPlayerInfo? targetData) =>
            {
                CustomSoundsManager.Play("Gunload", 2f);
                var guessManager = HudManager.Instance.gameObject.AddComponent<GuessManager>();
                guessManager.TargetId = pva.TargetPlayerId;
            };
            var role = PlayerControl.LocalPlayer.BetterData().RoleInfo.Role;
            Guess.Enabled = (BetterGameSettings.CrewmatesCanGuess.GetBool() && role.IsCrewmate)
                || (BetterGameSettings.ImpostersCanGuess.GetBool() && role.IsImpostor)
                || (BetterGameSettings.KillingNeutralsCanGuess.GetBool() && role.IsNeutral && role.CanKill)
                || (BetterGameSettings.BenignNeutralsCanGuess.GetBool() && role.IsNeutral && !role.CanKill)
                || role.GuessReliantRole;

            CustomRoleManager.RoleListenerOther(role => role.OnMeetingStart(__instance));

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
                LevelDisplay.GetComponent<SpriteRenderer>().color = new UnityEngine.Color(1f, 0f, 1f, 1f);
                var IdLabel = LevelDisplay.transform.Find("LevelLabel");
                var IdNumber = LevelDisplay.transform.Find("LevelNumber");
                IdLabel.gameObject.DestroyTextTranslator();
                IdLabel.GetComponent<TextMeshPro>().text = "ID";
                IdNumber.GetComponent<TextMeshPro>().text = pva.TargetPlayerId.ToString();
                IdLabel.name = "IdLabel";
                IdNumber.name = "IdNumber";
                PlayerLevel.transform.position += new Vector3(0.23f, 0f);
            }

            var textTemplate = UnityEngine.Object.Instantiate(__instance.TitleText, __instance.transform);
            textTemplate.name = "TextTemplate";
            var pos = textTemplate.gameObject.AddComponent<AspectPosition>();
            pos.Alignment = AspectPosition.EdgeAlignments.Center;
            pos.DistanceFromEdge = new Vector3(0f, -2f, -10f);
            textTemplate.DestroyTextTranslator();
            textTemplate.enableAutoSizing = false;
            textTemplate.fontSize = 2f;
            textTemplate.text = "Text Here";
            textTemplate.gameObject.SetActive(false);

            Dictionary<string, CustomClip?> texts = [];

            CustomRoleManager.RoleListenerOther(role =>
            {
                CustomClip? clip = null;
                string text = role.AddMeetingStartUpText(ref clip);
                if (!string.IsNullOrEmpty(text))
                {
                    texts[text] = clip;
                }
            });

            List<TextMeshPro> textPros = [];

            __instance.StartCoroutine(DisplayTextsQueue(texts, textPros, __instance, textTemplate));

            Logger.LogHeader("Meeting Has Started");
        }

        private static IEnumerator DisplayTextsQueue(Dictionary<string, CustomClip?> texts, List<TextMeshPro> textPros, MonoBehaviour instance, TextMeshPro textTemplate)
        {
            while (true)
            {
                if (MeetingHud.Instance == null || MeetingHud.Instance.state != MeetingHud.VoteStates.Animating)
                {
                    break;
                }

                yield return new WaitForSeconds(0.5f);
            }

            foreach (var kvp in texts)
            {
                var textPro = UnityEngine.Object.Instantiate(textTemplate, instance.transform);
                textPro.name = $"Text({textPros.Count})";
                textPro.gameObject.SetActive(true);
                textPro.text = kvp.Key;

                textPros.Add(textPro);

                var clip = kvp.Value;
                if (clip != null)
                {
                    CustomSoundsManager.Play(clip.ClipName, clip.Volume);
                }

                yield return instance.StartCoroutine(FadeText(textPro));

                textPros.Remove(textPro);
                UnityEngine.Object.Destroy(textPro.gameObject);
            }
        }

        public static IEnumerator FadeText(TextMeshPro text)
        {
            float displayDuration = 3.5f;
            float fadeDuration = 2f;
            float animationDuration = 0.15f;
            Vector3 originalScale = text.transform.localScale;
            Vector3 enlargedScale = originalScale * 10f;
            Color originalColor = text.color;

            // "Fly-in" animation to enlarge then scale back down
            text.transform.localScale = enlargedScale;

            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                text.transform.localScale = Vector3.Lerp(enlargedScale, originalScale, elapsed / animationDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            text.transform.localScale = originalScale;

            // Wait for display duration before fading out
            yield return new WaitForSeconds(displayDuration);

            // Fade-out effect on text color
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                float alpha = Mathf.Lerp(1, 0, elapsed / fadeDuration);
                text.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                elapsed += Time.deltaTime;
                yield return null;
            }

            text.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0);
        }

        [HarmonyPatch(nameof(MeetingHud.Update))]
        [HarmonyPostfix]
        public static void UpdatePostfix(MeetingHud __instance)
        {
            var buttons = PlayerMeetingButton.AllButtons;
            foreach (var button in buttons)
            {
                if (button == null) continue;
                button.Update();
            }

            if (__instance.playerStates == null || !GameStates.IsInGame) return;

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
                        var color = Utils.HexToColor32(player.BetterData().NameColor);
                        pva.NameText.color = new UnityEngine.Color(color.r, color.g, color.b, pva.NameText.color.a);
                    }
                    else
                    {
                        pva.NameText.color = new UnityEngine.Color(1f, 1f, 1f, pva.NameText.color.a);
                    }

                    if (player.IsLocalPlayer() || !PlayerControl.LocalPlayer.IsAlive(true) || player.IsImpostorTeammate() || CustomRoleManager.RoleChecksAny(PlayerControl.LocalPlayer, role => role.RevealPlayerRole(player)))
                    {
                        sbTag.Append($"{player.GetRoleNameAndColor()}{player.FormatTasksToText()}---");
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
