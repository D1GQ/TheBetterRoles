using BepInEx.Unity.IL2CPP.Utils;
using Hazel;
using System.Collections;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles.Roles;

public class SwapperRole : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 14;
    public override string RoleColor => "#52c345";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Swapper;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Crewmate;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Support;
    public override BetterOptionTab? SettingsTab => BetterTabs.CrewmateRoles;

    public BetterOptionItem? AmountOfSwaps;
    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
                AmountOfSwaps = new BetterOptionIntItem().Create(GetOptionUID(true), SettingsTab, Translator.GetString("Role.Swapper.Option.AmountOfSwaps"), [1, 100, 1], 3, "", "", RoleOptionItem)
            ];
        }
    }

    private PlayerVoteAreaButton? swapperButton;
    private NetworkedPlayerInfo? firstTargetData;
    private NetworkedPlayerInfo? secondTargetData;
    private bool hasSwapped;
    private bool isSwapping;
    private int swaps = 0;

    public override void OnMeetingStart(MeetingHud meetingHud)
    {
        swaps = AmountOfSwaps.GetInt();
        isSwapping = false;
        if (_player.IsLocalPlayer())
        {
            swapperButton = new PlayerVoteAreaButton().Create("Swap", this, LoadAbilitySprite("Swap", 80));
            swapperButton.ShowCondition = (pva, targetData) => { return (!targetData.IsDead && !targetData.Disconnected && !hasSwapped || targetData == firstTargetData || targetData == secondTargetData) && !isSwapping && swaps > 0; };
            swapperButton.ClickAction = OnSwap;
        }
    }

    private void OnSwap(PassiveButton? button, PlayerVoteArea? pva, NetworkedPlayerInfo? targetData)
    {
        if (MeetingHud.Instance.state != MeetingHud.VoteStates.NotVoted || swaps <= 0) return;

        if (firstTargetData == targetData || secondTargetData == targetData)
        {
            UnsetTarget(targetData, button);
            return;
        }

        button.GetComponent<SpriteRenderer>().color = RoleColor32;

        if (firstTargetData == null)
        {
            firstTargetData = targetData;
        }
        else if (secondTargetData == null)
        {
            secondTargetData = targetData;
        }

        hasSwapped = firstTargetData != null && secondTargetData != null;

        SendRoleSync(0);
    }

    public override void FixedUpdate()
    {
        if (hasSwapped)
        {
            if (firstTargetData.IsDead || firstTargetData.Disconnected
                || secondTargetData.IsDead || secondTargetData.Disconnected)
            {
                UnsetTargets();
            }
        }
    }

    private void UnsetTarget(NetworkedPlayerInfo? target, PassiveButton? button)
    {
        if (firstTargetData == target)
        {
            firstTargetData = null;
        }
        else if (secondTargetData == target)
        {
            secondTargetData = null;
        }

        button.GetComponent<SpriteRenderer>().color = Color.white;
        hasSwapped = false;

        SendRoleSync(0);
    }

    private void UnsetTargets()
    {
        hasSwapped = false;
        firstTargetData = null;
        secondTargetData = null;

        if (swapperButton != null)
        {
            foreach (var button in swapperButton.Buttons.Keys)
            {
                if (button == null) continue;
                button.GetComponent<SpriteRenderer>().color = Color.white;
            }
        }
    }

    // Only ran by host!
    public override void OnEndVoting(MeetingHud meetingHud)
    {
        SwapVotes(meetingHud);
        SendRoleSync(1);
    }

    private void SwapVotes(MeetingHud meetingHud, bool onlyAnimate = false)
    {
        if (hasSwapped && firstTargetData != null && secondTargetData != null)
        {
            PlayerVoteArea? myPva = meetingHud.playerStates.FirstOrDefault(p => p.TargetPlayerId == PlayerControl.LocalPlayer.PlayerId);

            if (!onlyAnimate)
            {
                foreach (var pva in meetingHud.playerStates.ToArray())
                {
                    if (pva.VotedFor == firstTargetData.PlayerId)
                    {
                        if (pva.AmDead) continue;
                        pva.UnsetVote();
                        pva.SetVote(secondTargetData.PlayerId);
                    }
                    else if (pva.VotedFor == secondTargetData.PlayerId)
                    {
                        if (pva.AmDead) continue;
                        pva.UnsetVote();
                        pva.SetVote(firstTargetData.PlayerId);
                    }
                }
            }

            PlayerVoteArea? pva1 = meetingHud.playerStates.FirstOrDefault(p => p.TargetPlayerId == firstTargetData.PlayerId);
            PlayerVoteArea? pva2 = meetingHud.playerStates.FirstOrDefault(p => p.TargetPlayerId == secondTargetData.PlayerId);
            _player.BetterData().StartCoroutine(SwapAnimation(pva1, pva2));
        }

        UnsetTargets();
    }

    private IEnumerator SwapAnimation(PlayerVoteArea pva1, PlayerVoteArea pva2)
    {
        swaps--;
        isSwapping = true;

        PlayerVoteArea? myPva = MeetingHud.Instance.playerStates.FirstOrDefault(p => p.TargetPlayerId == PlayerControl.LocalPlayer.PlayerId);

        if (myPva.VotedFor == pva1.TargetPlayerId)
        {
            pva1.ThumbsDown.enabled = true;
            PlayerMaterial.SetColors(new Color(0.4f, 0.4f, 0.4f, 1f), pva2.ThumbsDown);
            pva2.ThumbsDown.color = new Color(1f, 1f, 1f, 0.5f);
        }
        else if (myPva.VotedFor == pva2.TargetPlayerId)
        {
            pva2.ThumbsDown.enabled = true;
            PlayerMaterial.SetColors(new Color(0.4f, 0.4f, 0.4f, 1f), pva1.ThumbsDown);
            pva1.ThumbsDown.color = new Color(1f, 1f, 1f, 0.5f);
        }

        float animationDuration = 2.5f;

        Vector3 startPosition1 = pva1.transform.position;
        Vector3 startPosition2 = pva2.transform.position;

        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            float t = elapsedTime / animationDuration;

            pva1.transform.position = Vector3.Lerp(startPosition1, startPosition2, t);
            pva2.transform.position = Vector3.Lerp(startPosition2, startPosition1, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        pva1.transform.position = startPosition2;
        pva2.transform.position = startPosition1;
    }


    public override void OnSendRoleSync(int syncId, MessageWriter writer, object[]? additionalParams)
    {
        switch (syncId)
        {
            case 0:
                {
                    writer.WritePlayerDataId(firstTargetData);
                    writer.WritePlayerDataId(secondTargetData);
                    writer.Write(hasSwapped);
                }
                break;
        }
    }

    public override void OnReceiveRoleSync(int syncId, MessageReader reader, PlayerControl sender)
    {
        switch (syncId)
        {
            case 0:
                {
                    Logger.InGame("TEST");
                    firstTargetData = reader.ReadPlayerDataId();
                    secondTargetData = reader.ReadPlayerDataId();
                    hasSwapped = reader.ReadBoolean();
                }
                break;
            case 1:
                {
                    SwapVotes(MeetingHud.Instance, true);
                }
                break;
        }
    }

    public override void SetAbilityAmountTextForMeeting(ref int maxAmount, ref int currentAmount)
    {
        maxAmount = AmountOfSwaps.GetInt();
        currentAmount = swaps;
    }
}
