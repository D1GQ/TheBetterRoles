
using Hazel;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles.Roles;

public class MayorRole : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 10;
    public override string RoleColor => "#004f1e";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Mayor;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Crewmate;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Support;
    public override TBROptionTab? SettingsTab => BetterTabs.CrewmateRoles;

    public TBROptionItem? CollectVoteOnSkip;
    public TBROptionItem? CollectVoteWhenVotedOn;
    public TBROptionItem? AdditionalVoteIcon;
    public override TBROptionItem[]? OptionItems
    {
        get
        {
            return
            [
                CollectVoteOnSkip = new TBROptionCheckboxItem().Create(GetOptionUID(true), SettingsTab, Translator.GetString("Role.Mayor.Option.CollectVoteOnSkip"), true, RoleOptionItem),
                CollectVoteWhenVotedOn = new TBROptionCheckboxItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Mayor.Option.CollectVoteWhenVotedOn"), true, RoleOptionItem),
                AdditionalVoteIcon = new TBROptionStringItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Mayor.Option.AdditionalVoteIcon"),
                [Translator.GetString("Role.Mayor.AdditionalVoteIcon.Normal"), Translator.GetString("Role.Mayor.AdditionalVoteIcon.Anonymous"), Translator.GetString("Role.Mayor.AdditionalVoteIcon.Hide")],
                1, RoleOptionItem),
            ];
        }
    }


    private PlayerVoteAreaButton? voteButton;
    private int additionalVotes = 1;
    private List<byte> voted = [];

    public override void OnMeetingStart(MeetingHud meetingHud)
    {
        voted.Clear();
        if (_player.IsLocalPlayer())
        {
            voteButton = new PlayerVoteAreaButton().Create("Vote", this, LoadAbilitySprite("Vote", 100));
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

        IsDirty = true;
    }

    public override void OnEndVoting(MeetingHud meetingHud)
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

    public override void AddAdditionalVotes(MeetingHud meetingHud, ref Dictionary<byte, int> votes)
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

    public override void AddVisualVotes(MeetingHud meetingHud, PlayerVoteArea votedFor, ref List<MeetingHud.VoterState> states)
    {
        if (AdditionalVoteIcon.GetValue() == 2) return;

        foreach (var vote in voted)
        {
            states.Add(new MeetingHud.VoterState()
            {
                VoterId = AdditionalVoteIcon.GetValue() != 1 ? _player.PlayerId : (byte)255,
                VotedForId = vote,
            });
        }
    }

    public override void SetAbilityAmountText(ref int maxAmount, ref int currentAmount)
    {
        currentAmount = additionalVotes;
    }

    public override void Serialize(MessageWriter writer)
    {
        writer.WritePacked(additionalVotes);
        writer.Write(voted.Count);
        foreach (var vote in voted)
        {
            writer.Write(vote);
        }
    }

    public override void Deserialize(MessageReader reader)
    {
        additionalVotes = reader.ReadPackedInt32();
        int count = reader.ReadInt32();
        List<byte> bytes = [];
        for (int i = 0; i < count; i++)
        {
            bytes.Add(reader.ReadByte());
        }
        voted = bytes;
    }
}
