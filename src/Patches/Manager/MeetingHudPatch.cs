using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System.Collections;
using TheBetterRoles.Data;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Monos;
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

    internal static void UpdateHostIcon()
    {
        if (MeetingHud.Instance == null) return;

        PlayerMaterial.SetColors(GameData.Instance.GetHost().Color, MeetingHud.Instance.HostIcon);
        MeetingHud.Instance.ProceedButton.gameObject.GetComponentInChildren<TextMeshPro>().text = Translator.GetString("HostInMeeting", [GameData.Instance.GetHost().PlayerName]);
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
        // Add host icon to meeting hud
        __instance.ProceedButton.gameObject.transform.localPosition = new(-2.5f, 2.2f, 0);
        __instance.ProceedButton.gameObject.GetComponent<SpriteRenderer>().enabled = false;
        __instance.ProceedButton.GetComponent<PassiveButton>().enabled = false;
        __instance.HostIcon.enabled = true;
        __instance.HostIcon.gameObject.SetActive(true);
        __instance.ProceedButton.gameObject.SetActive(true);
        MeetingHud.Instance.ProceedButton.DestroyTextTranslators();
        UpdateHostIcon();

        PlayerVoteAreaButton.AllButtons.Clear();

        var role = PlayerControl.LocalPlayer.Role();
        if (TBRGameSettings.ImpostorsCanGuess.GetBool() && role.IsImpostor ||
            TBRGameSettings.CrewmatesCanGuess.GetBool() && role.IsCrewmate ||
            TBRGameSettings.BenignNeutralsCanGuess.GetBool() && role.IsNeutral && !role.IsKillingRole ||
            TBRGameSettings.KillingNeutralsCanGuess.GetBool() && role.IsNeutral && !role.IsKillingRole ||
            role.GuessReliantRole)
        {
            var Guess = PlayerVoteAreaButton.Create("Guess", sprite: Utils.LoadSprite($"TheBetterRoles.Resources.Images.Icons.TargetIcon.png", 100));
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
            var target = Utils.PlayerFromPlayerId(pva.TargetPlayerId);
            pva.gameObject.AddComponent<MeetingInfoDisplay>().Init(target, pva);
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
            NetworkedPlayerInfo? exiled = GameData.Instance.AllPlayers.FirstOrDefaultIL2CPP((v) => !tie && v.PlayerId == max.Key);
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
