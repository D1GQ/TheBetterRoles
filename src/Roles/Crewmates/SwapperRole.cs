using BepInEx.Unity.IL2CPP.Utils;
using Hazel;
using System.Collections;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Interfaces;
using UnityEngine;

namespace TheBetterRoles.Roles.Crewmates;

internal sealed class SwapperRole : CrewmateRoleTBR, IRoleUpdateAction, IRoleMeetingAction
{
    internal sealed override int RoleId => 14;
    internal sealed override string RoleColorHex => "#52c345";
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Swapper;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Crewmate;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Support;
    internal sealed override OptionTab? SettingsTab => TBRTabs.CrewmateRoles;
    internal sealed override bool MeetingReliantRole => true;

    internal OptionItem? AmountOfSwaps;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                AmountOfSwaps = OptionIntItem.Create(GetOptionUID(), SettingsTab, "Role.Swapper.Option.AmountOfSwaps", (1, 100, 1), 3, ("", ""), RoleOptions.RoleOptionItem)
            ];
        }
    }

    private NetworkedPlayerInfo? firstTargetData;
    private NetworkedPlayerInfo? secondTargetData;
    private bool hasSwapped;
    private bool isSwapping;
    private int swaps = 0;
    private PlayerVoteAreaButton? swapperButton;
    internal sealed override void OnSetUpRole()
    {
        swaps = AmountOfSwaps.GetInt();
        isSwapping = false;
    }

    void IRoleMeetingAction.MeetingStart(MeetingHud meetingHud)
    {
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

        button.GetComponent<SpriteRenderer>().color = RoleColor;

        if (firstTargetData == null)
        {
            firstTargetData = targetData;
        }
        else if (secondTargetData == null)
        {
            secondTargetData = targetData;
        }

        hasSwapped = firstTargetData != null && secondTargetData != null;

        swapperButton?.UpdateButtonStates();

        MarkDirty();
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

        swapperButton?.UpdateButtonStates();

        MarkDirty();
    }

    void IRoleUpdateAction.FixedUpdate()
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

    private void UnsetTargets()
    {
        hasSwapped = false;
        firstTargetData = null;
        secondTargetData = null;

        if (swapperButton != null)
        {
            foreach (var button in swapperButton.Buttons)
            {
                if (button.Item1 == null) continue;
                button.Item1.GetComponent<SpriteRenderer>().color = Color.white;
            }
        }

        swapperButton?.UpdateButtonStates();
    }

    // Only ran by host!
    void IRoleMeetingAction.EndVoting(MeetingHud meetingHud, ref Dictionary<byte, int> calculatedVotes)
    {
        SendRoleSync(0);
        SwapVotes(meetingHud);
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
            CoroutineManager.Instance.StartCoroutine(CoSwapAnimation(pva1, pva2));
        }

        UnsetTargets();
    }

    private IEnumerator CoSwapAnimation(PlayerVoteArea pva1, PlayerVoteArea pva2)
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

    internal sealed override void SetAbilityAmountText(ref int maxAmount, ref int currentAmount)
    {
        maxAmount = AmountOfSwaps.GetInt();
        currentAmount = swaps;
    }

    internal sealed override void OnReceiveRoleSync(int syncId, MessageReader reader, PlayerControl sender)
    {
        switch (syncId)
        {
            case 0:
                {
                    SwapVotes(MeetingHud.Instance, true);
                }
                break;
        }
    }

    public override void Serialize(MessageWriter writer)
    {
        writer.WritePlayerData(firstTargetData);
        writer.WritePlayerData(secondTargetData);
        writer.Write(hasSwapped);
        ClearDirtyBits();
    }

    public override void Deserialize(MessageReader reader)
    {
        firstTargetData = reader.ReadPlayerData();
        secondTargetData = reader.ReadPlayerData();
        hasSwapped = reader.ReadBoolean();
    }
}
