
using BepInEx.Unity.IL2CPP.Utils;
using Hazel;
using System.Collections;
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles;

public class SwapperRole : CustomRoleBehavior
{
    // Role Info
    public override string RoleColor => "#52c345";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Swapper;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Crewmate;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Support;
    public override BetterOptionTab? SettingsTab => BetterTabs.CrewmateRoles;
    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
            ];
        }
    }

    private PlayerMeetingButton? swapperButton;
    private NetworkedPlayerInfo? firstTargetData;
    private NetworkedPlayerInfo? secondTargetData;
    private bool hasSwapped;
    private bool isSwapping;

    public override void OnMeetingStart(MeetingHud meetingHud)
    {
        isSwapping = false;
        if (_player.IsLocalPlayer())
        {
            swapperButton = new PlayerMeetingButton().Create("Swap", this, LoadAbilitySprite("Swap", 80));
            swapperButton.ShowCondition = (pva, targetData) => { return (!targetData.IsDead && !targetData.Disconnected && !hasSwapped || targetData == firstTargetData || targetData == secondTargetData) && !isSwapping; };
            swapperButton.ClickAction = OnSwap;
        }
    }

    private void OnSwap(PassiveButton? button, PlayerVoteArea? pva, NetworkedPlayerInfo? targetData)
    {
        if (MeetingHud.Instance.state != MeetingHud.VoteStates.NotVoted) return;

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

        foreach (var button in swapperButton.Buttons.Keys)
        {
            if (button == null) continue;
            button.GetComponent<SpriteRenderer>().color = Color.white;
        }
    }

    public override void CheckForEndVoting(MeetingHud meetingHud)
    {
        if (hasSwapped && firstTargetData != null && secondTargetData != null)
        {
            isSwapping = true;
            foreach (var pva in meetingHud.playerStates.ToArray())
            {
                if (pva.VotedFor == firstTargetData.PlayerId)
                {
                    if (pva.AmDead) continue;
                    pva.VotedFor = secondTargetData.PlayerId;
                }
                else if (pva.VotedFor == secondTargetData.PlayerId)
                {
                    if (pva.AmDead) continue;
                    pva.VotedFor = firstTargetData.PlayerId;
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
                    writer.Write(firstTargetData != null ? firstTargetData.PlayerId : (byte)255);
                    writer.Write(secondTargetData != null ? secondTargetData.PlayerId : (byte)255);
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
                    firstTargetData = Utils.PlayerDataFromPlayerId(reader.ReadByte());
                    secondTargetData = Utils.PlayerDataFromPlayerId(reader.ReadByte());
                    hasSwapped = reader.ReadBoolean();
                }
                break;
        }
    }
}
