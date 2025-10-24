using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System.Collections;
using System.Text;
using TheBetterRoles.Data;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Network;
using TheBetterRoles.Roles;
using TheBetterRoles.Roles.Interfaces;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Patches.Manager;

[HarmonyPatch(typeof(MeetingHud))]
internal class MeetingHudPatch
{
    internal static void AdjustVotesOnGuess(PlayerControl pc)
    {
        PlayerVoteArea voteArea = MeetingHud.Instance.playerStates.First(
            x => x.TargetPlayerId == pc.PlayerId
        );
        if (voteArea == null) return;
        if (voteArea.DidVote) voteArea.UnsetVote();
        voteArea.Buttons.SetActive(false);
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

    [HarmonyPatch(nameof(MeetingHud.Awake))]
    [HarmonyPrefix]
    private static void Start_Prefix(MeetingHud __instance)
    {
        RoleListener.InvokeRoles<IRoleMeetingAction>(role => role.MeetingBegin(__instance));
    }

    [HarmonyPatch(nameof(MeetingHud.Start))]
    [HarmonyPostfix]
    private static void Start_Postfix(MeetingHud __instance)
    {
        PlayerVoteAreaButton.AllButtons.Clear();

        var role = PlayerControl.LocalPlayer.Role();
        if (TBRGameSettings.ImpostorsCanGuess.GetBool() && role.IsImpostor ||
            TBRGameSettings.CrewmatesCanGuess.GetBool() && role.IsCrewmate ||
            TBRGameSettings.BenignNeutralsCanGuess.GetBool() && role.IsNeutral && !role.IsKillingRole ||
            TBRGameSettings.KillingNeutralsCanGuess.GetBool() && role.IsNeutral && !role.IsKillingRole ||
            role.GuessReliantRole)
        {
            var Guess = new PlayerVoteAreaButton().Create("Guess", sprite: Utils.LoadSprite($"TheBetterRoles.Resources.Images.Icons.TargetIcon.png", 100));
            Guess.ClickAction = (button, pva, targetData) =>
            {
                CustomSoundsManager.Instance.Play(Sounds.Gunload, 2f);
                var guessManager = HudManager.Instance.gameObject.AddComponent<GuessManager>();
                guessManager.TargetId = pva.TargetPlayerId;
            };
        }

        RoleListener.InvokeRoles<IRoleMeetingAction>(role => role.MeetingStart(__instance));

        foreach (var pva in __instance.playerStates)
        {
            var player = Utils.PlayerFromPlayerId(pva.TargetPlayerId);
            player?.DirtyName();

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
            IdLabel.gameObject.DestroyTextTranslators();
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
        textTemplate.DestroyTextTranslators();
        textTemplate.enableAutoSizing = false;
        textTemplate.fontSize = 2f;
        textTemplate.text = "Text Here";
        textTemplate.gameObject.SetActive(false);

        Dictionary<string, CustomClip?> texts = [];

        List<(string text, CustomClip? clip, uint priority)> roleTexts = new();

        RoleListener.InvokeRoles<IRoleMeetingAction>(role =>
        {
            CustomClip? clip = null;
            string text = role.AddMeetingText(ref clip, out uint priority);
            if (!string.IsNullOrEmpty(text))
            {
                roleTexts.Add((text, clip, priority));
            }
        });

        roleTexts.Sort((a, b) => b.priority.CompareTo(a.priority));

        foreach (var (sortedText, sortedClip, _) in roleTexts)
        {
            texts[sortedText] = sortedClip;
        }

        List<TextMeshPro> textPros = [];

        __instance.StartCoroutine(CoDisplayTextsQueue(texts, textPros, __instance, textTemplate));

        Logger.LogHeader("Meeting Has Started");
    }

    private static IEnumerator CoDisplayTextsQueue(Dictionary<string, CustomClip?> texts, List<TextMeshPro> textPros, MonoBehaviour instance, TextMeshPro textTemplate)
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
                if (!string.IsNullOrEmpty(clip.ClipName))
                {
                    CustomSoundsManager.Instance.Play(clip.ClipName, clip.Volume);
                }
                else if (clip.Clip != null)
                {
                    SoundManager.Instance.PlaySound(clip.Clip, false, clip.Volume);
                }
            }

            yield return instance.StartCoroutine(CoFadeText(textPro));

            textPros.Remove(textPro);
            UnityEngine.Object.Destroy(textPro.gameObject);
        }
    }

    private static IEnumerator CoFadeText(TextMeshPro text)
    {
        float displayDuration = 6f;
        float fadeDuration = 1.5f;
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
    [HarmonyPrefix]
    private static bool UpdatePrefix(MeetingHud __instance)
    {
        // Pause discussion State when kill animation is playing in meeting
        if (__instance.state is not MeetingHud.VoteStates.Proceeding)
        {
            if (HudManager.Instance.KillOverlay.flameParent.active)
            {
                return false;
            }
        }

        return true;
    }

    [HarmonyPatch(nameof(MeetingHud.Update))]
    [HarmonyPostfix]
    private static void UpdatePostfix(MeetingHud __instance)
    {
        if (__instance == null) return;

        if (__instance.playerStates == null || !GameState.IsInGame) return;

        foreach (var pva in __instance.playerStates)
        {
            if (pva == null) continue;

            var playerData = GameData.Instance?.GetPlayerById(pva.TargetPlayerId);
            if (playerData == null) continue;
            var extendedPlayerData = playerData.ExtendedData();
            if (extendedPlayerData == null) return;

            if (extendedPlayerData.DirtyName <= 0) continue;

            extendedPlayerData.DirtyName -= 1;

            if (pva.ColorBlindName != null && pva.ColorBlindName.isActiveAndEnabled)
            {
                pva.ColorBlindName.transform.localPosition = new Vector3(-0.91f, -0.19f, -0.05f);
                pva.ColorBlindName.outlineWidth = 0.2745f;
            }

            if (pva.NameText == null) continue;

            TextMeshPro? TopText = pva.NameText.transform.Find("TextTop")?.gameObject?.GetComponent<TextMeshPro>();
            TextMeshPro? InfoText = pva.NameText.transform.Find("TextInfo")?.gameObject?.GetComponent<TextMeshPro>();

            bool flag = Main.AllPlayerControls.Any(player => player.PlayerId == pva.TargetPlayerId);
            PlayerControl? player = Utils.PlayerFromPlayerId(pva.TargetPlayerId);

            if (!flag)
            {
                string DisconnectText;
                switch (extendedPlayerData.DisconnectReason)
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
                if (player == null || extendedPlayerData == null) continue;

                var sbTagTop = new StringBuilder();
                var sbTag = new StringBuilder();

                string hexColor = "";

                bool hideInfo = PlayerControl.LocalPlayer.CheckAnyRoles(role => role.HidePlayerInfoOther(player));

                bool canRevealRole = player.IsLocalPlayer() || !PlayerControl.LocalPlayer.IsAlive(true) || player.IsImpostorTeammate()
                    || PlayerControl.LocalPlayer.CheckAnyRoles(role => role.RevealPlayerRole(player));

                if (canRevealRole && !hideInfo && player.Role()?.ShowRoleAboveName == true)
                {
                    hexColor = player.Role()?.RoleColorHex ?? "#FFFFFF";
                    sbTag.Append($"{player.Role()?.RoleNameAndAbilityAmount}{player.FormatTasksToText()}---");
                }

                bool canRevealAddons = player.IsLocalPlayer() || !PlayerControl.LocalPlayer.IsAlive(true) || player.IsImpostorTeammate()
                    || PlayerControl.LocalPlayer.CheckAnyRoles(role => role.RevealPlayerAddons(player));

                if (canRevealAddons && !hideInfo)
                {
                    foreach (var addon in extendedPlayerData.RoleInfo.Addons)
                    {
                        if (!addon.ShowRoleAboveName) continue;
                        sbTagTop.Append($"<size=55%>{addon.RoleNameAndAbilityAmount}</size>+++");
                    }
                }

                bool canRevealDeath = (player.IsLocalPlayer() && !player.IsAlive() || !PlayerControl.LocalPlayer.IsAlive(true) && !PlayerControl.LocalPlayer.IsGhostRole() ||
                    PlayerControl.LocalPlayer.CheckAnyRoles(role => role.RevealPlayerDeath(player))) && !player.IsAlive(true);

                if (canRevealDeath && !hideInfo)
                {
                    var num = sbTag.Length - 3;
                    if (num > 0)
                    {
                        sbTag.Remove(num, 3);
                    }
                    sbTag.Append($"{player.FormatDeathReason()}---");
                }

                if (!string.IsNullOrEmpty(extendedPlayerData.NameColor))
                {
                    hexColor = extendedPlayerData.NameColor;
                }

                if (!string.IsNullOrEmpty(hexColor))
                {
                    var color = Colors.HexToColor(hexColor);
                    pva.NameText.color = new Color(color.r, color.g, color.b, pva.NameText.color.a);
                }
                else
                {
                    pva.NameText.color = new Color(1f, 1f, 1f, pva.NameText.color.a);
                }

                sbTagTop = Utils.FormatStringBuilder(sbTagTop);
                sbTag = Utils.FormatStringBuilder(sbTag);

                pva.NameText.text = Utils.FormatPlayerName(player);
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

    [HarmonyPatch(nameof(MeetingHud.SetMasksEnabled))]
    [HarmonyPostfix]
    private static void SetMasksEnabled_Postfix(/*MeetingHud __instance*/)
    {
        PlayerVoteAreaButton.UpdateAllButtonStates();
        Utils.DirtyAllNames();
    }

    internal static bool DoNotCalculate = false;
    [HarmonyPatch(nameof(MeetingHud.CalculateVotes))]
    [HarmonyPrefix]
    private static bool CalculateVotes_Prefix(MeetingHud __instance, ref Il2CppSystem.Collections.Generic.Dictionary<byte, int> __result)
    {
        if (DoNotCalculate)
        {
            __result = new();
            DoNotCalculate = false;
            return false;
        }

        var playerStates = __instance.playerStates.ToList();
        Dictionary<byte, int> dictionary = [];

        for (int i = 0; i < playerStates.Count; i++)
        {
            PlayerVoteArea playerVoteArea = playerStates[i];

            int roleNum = 0;

            Utils.PlayerFromPlayerId(playerVoteArea.TargetPlayerId).InvokeRoles<IRoleMeetingAction>(role => roleNum += role.AddVotes(__instance, playerVoteArea));

            if (playerVoteArea.VotedFor != 252 && playerVoteArea.VotedFor != 255 && playerVoteArea.VotedFor != 254)
            {
                if (dictionary.TryGetValue(playerVoteArea.VotedFor, out int num))
                {
                    dictionary[playerVoteArea.VotedFor] = num + roleNum + 1;
                }
                else
                {
                    dictionary[playerVoteArea.VotedFor] = roleNum + 1;
                }
            }
        }

        RoleListener.InvokeRoles<IRoleMeetingAction>(role => role.AddAdditionalVotes(__instance, ref dictionary));

        __instance.playerStates = playerStates.ToArray();

        __result = new Il2CppSystem.Collections.Generic.Dictionary<byte, int>();
        foreach (var kvp in dictionary)
        {
            __result[kvp.Key] = kvp.Value;
        }

        return false;
    }

    // if VoterId is 255 then hide vote color
    [HarmonyPatch(nameof(MeetingHud.PopulateResults))]
    [HarmonyPrefix]
    private static bool PopulateResults_Prefix(MeetingHud __instance, [HarmonyArgument(0)] Il2CppArrayBase<MeetingHud.VoterState> states)
    {
        __instance.TitleText.text = Translator.GetString(StringNames.MeetingVotingResults);
        int num = 0;
        var statesOrder = states.OrderBy(pv => pv.VoterId < 200 ? 0 : 1);
        for (int i = 0; i < __instance.playerStates.Length; i++)
        {
            PlayerVoteArea playerVoteArea = __instance.playerStates[i];
            playerVoteArea.ClearForResults();
            int num2 = 0;
            foreach (MeetingHud.VoterState voterState in statesOrder)
            {
                NetworkedPlayerInfo? playerById = Utils.PlayerDataFromPlayerId(voterState.VoterId);
                bool flag = voterState.VoterId == 255;
                if (playerById == null && !flag)
                {
                    Logger.Error(string.Format("Couldn't find player info for voter: {0}", voterState.VoterId));
                }
                else if (i == 0 && voterState.SkippedVote)
                {
                    if (flag)
                    {
                        BloopAAnonymousVoteIcon(num, __instance.SkippedVoting.transform);
                        num++;
                        continue;
                    }
                    __instance.BloopAVoteIcon(playerById, num, __instance.SkippedVoting.transform);
                    num++;
                }
                else if (voterState.VotedForId == playerVoteArea.TargetPlayerId)
                {
                    if (flag)
                    {
                        BloopAAnonymousVoteIcon(num2, playerVoteArea.transform);
                        num2++;
                        continue;
                    }
                    __instance.BloopAVoteIcon(playerById, num2, playerVoteArea.transform);
                    num2++;
                }
            }
        }

        return false;
    }

    private static void BloopAAnonymousVoteIcon(int index, Transform parent)
    {
        var meetingHud = MeetingHud.Instance;
        SpriteRenderer spriteRenderer = UnityEngine.Object.Instantiate(meetingHud.PlayerVotePrefab);
        PlayerMaterial.SetColors(Palette.DisabledGrey, spriteRenderer);
        spriteRenderer.color = new Color(1f, 1f, 1f, 0.5f);
        spriteRenderer.transform.SetParent(parent);
        spriteRenderer.transform.localScale = Vector3.zero;
        PlayerVoteArea component = parent.GetComponent<PlayerVoteArea>();
        if (component != null)
        {
            spriteRenderer.material.SetInt(PlayerMaterial.MaskLayer, component.MaskLayer);
        }
        meetingHud.StartCoroutine(Effects.Bloop(index * 0.3f, spriteRenderer.transform, 1f, 0.5f));
        parent.GetComponent<VoteSpreader>().AddVote(spriteRenderer);
    }

    [HarmonyPatch(nameof(MeetingHud.CheckForEndVoting))]
    [HarmonyPrefix]
    private static void CheckForEndVoting_Prefix(MeetingHud __instance)
    {
        if (__instance.playerStates.All(ps => ps.AmDead || ps.DidVote))
        {
            var calculatedVotes = new Dictionary<byte, int>();
            foreach (var kvp in __instance.CalculateVotes())
            {
                calculatedVotes[kvp.Key] = kvp.Value;
            }
            RoleListener.InvokeRoles<IRoleMeetingAction>(role => role.EndVoting(__instance, ref calculatedVotes));
            KeyValuePair<byte, int> max = calculatedVotes.MaxPair(out bool tie);
            NetworkedPlayerInfo? exiled = GameData.Instance.AllPlayers.ToArray().FirstOrDefault((v) => !tie && v.PlayerId == max.Key);
            List<MeetingHud.VoterState> list = [];
            for (int i = 0; i < __instance.playerStates.Length; i++)
            {
                PlayerVoteArea playerVoteArea = __instance.playerStates[i];

                Utils.PlayerFromPlayerId(playerVoteArea.TargetPlayerId).InvokeRoles<IRoleMeetingAction>(role => role.AddVisualVotes(__instance, playerVoteArea, ref list));

                list.Add(new MeetingHud.VoterState
                {
                    VoterId = playerVoteArea.TargetPlayerId,
                    VotedForId = playerVoteArea.VotedFor
                });
            }
            __instance.RpcVotingComplete(list.ToArray(), exiled, tie);
        }
    }

    [HarmonyPatch(nameof(MeetingHud.Close))]
    [HarmonyPostfix]
    private static void Close_Postfix()
    {
        Logger.LogHeader("Meeting Has Ended");
    }
}
