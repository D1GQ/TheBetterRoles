using Hazel;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Interfaces;
using UnityEngine;

namespace TheBetterRoles.Roles.Crewmates;

internal sealed class MayorRole : CrewmateRoleTBR, IRoleMeetingAction
{
    internal sealed override int RoleId => 10;
    internal sealed override string RoleColorHex => "#004f1e";
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Mayor;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Crewmate;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Support;
    internal sealed override OptionTab? SettingsTab => TBRTabs.CrewmateRoles;
    internal sealed override bool DefaultCanCallMeetingOption => true;
    internal sealed override bool MeetingReliantRole => true;

    internal OptionItem? CollectVoteOnSkip;
    internal OptionItem? CollectVoteWhenVotedOn;
    internal OptionItem? AdditionalVoteIcon;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                CollectVoteOnSkip = OptionCheckboxItem.Create(GetOptionUID(), SettingsTab, "Role.Mayor.Option.CollectVoteOnSkip", true, RoleOptions.RoleOptionItem),
                CollectVoteWhenVotedOn = OptionCheckboxItem.Create(GetOptionUID(), SettingsTab, "Role.Mayor.Option.CollectVoteWhenVotedOn", true, RoleOptions.RoleOptionItem),
                AdditionalVoteIcon = OptionStringItem.Create(GetOptionUID(), SettingsTab, "Role.Mayor.Option.AdditionalVoteIcon",
                ["Role.Mayor.AdditionalVoteIcon.Normal", "Role.Mayor.AdditionalVoteIcon.Anonymous", "Role.Mayor.AdditionalVoteIcon.Hide"],
                1, RoleOptions.RoleOptionItem),
            ];
        }
    }


    private int additionalVotes = 1;
    private List<byte> voted = [];
    private PlayerVoteAreaButton? voteButton;

    void IRoleMeetingAction.MeetingStart(MeetingHud meetingHud)
    {
        voted.Clear();
        if (_player.IsLocalPlayer())
        {
            voteButton = PlayerVoteAreaButton.Create("Vote", this, LoadAbilitySprite("MayorVote", 100));
            voteButton.ShowCondition = (pva, targetData) => { return !targetData.IsDead && !targetData.Disconnected && additionalVotes > 0; };
            voteButton.ClickAction = OnVoteButton;
        }
    }

    private AudioClip? lastSound;
    private void OnVoteButton(PassiveButton? button, PlayerVoteArea? pva, NetworkedPlayerInfo? targetData)
    {
        if (MeetingHud.Instance.state is MeetingHud.VoteStates.NotVoted)
        {
            additionalVotes--;
            voted.Add(pva.TargetPlayerId);
            if (lastSound != null)
            {
                SoundManager.instance.StopSound(lastSound);
            }
            lastSound = MeetingHud.Instance.VoteLockinSound;
            SoundManager.instance.PlaySound(lastSound, false);
        }

        voteButton.UpdateButtonStates();

        MarkDirty();
    }

    void IRoleMeetingAction.EndVoting(MeetingHud meetingHud, ref Dictionary<byte, int> calculatedVotes)
    {
        foreach (var pva in meetingHud.playerStates)
        {
            if (CollectVoteOnSkip.GetBool())
            {
                if (pva.TargetPlayerId == _player.PlayerId && pva.VotedFor == 253)
                {
                    additionalVotes++;
                }
            }

            if (CollectVoteWhenVotedOn.GetBool())
            {
                if (pva.TargetPlayerId != _player.PlayerId && pva.VotedFor == _player.PlayerId)
                {
                    additionalVotes++;
                }
            }
        }
    }

    void IRoleMeetingAction.AddAdditionalVotes(MeetingHud meetingHud, ref Dictionary<byte, int> votes)
    {
        foreach (var vote in voted)
        {
            if (!votes.ContainsKey(vote))
            {
                votes[vote] = 1;
                continue;
            }
            votes[vote]++;
        }
    }

    void IRoleMeetingAction.AddVisualVotes(MeetingHud meetingHud, PlayerVoteArea votedFor, ref List<MeetingHud.VoterState> states)
    {
        if (AdditionalVoteIcon.GetStringValue() == 2) return;

        foreach (var vote in voted)
        {
            states.Add(new MeetingHud.VoterState()
            {
                VoterId = AdditionalVoteIcon.GetStringValue() != 1 ? _player.PlayerId : (byte)255,
                VotedForId = vote,
            });
        }
    }

    internal sealed override void SetAbilityAmountText(ref int maxAmount, ref int currentAmount)
    {
        currentAmount = additionalVotes;
    }

    public override void Serialize(MessageWriter writer)
    {
        writer.WritePacked(additionalVotes);
        writer.WriteFast(voted);
        ClearDirtyBits();
    }

    public override void Deserialize(MessageReader reader)
    {
        additionalVotes = reader.ReadPackedInt32();
        voted = reader.ReadFast<List<byte>>();
    }
}
