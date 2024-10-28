
using Hazel;
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles;

public class MayorRole : CustomRoleBehavior
{
    // Role Info
    public override string RoleColor => "#004f1e";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Mayor;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Crewmate;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Support;
    public override BetterOptionTab? SettingsTab => BetterTabs.CrewmateRoles;
    public BetterOptionItem? CollectVoteOnSkip;
    public BetterOptionItem? CollectVoteWhenVotedOn;
    public BetterOptionItem? AdditionalVoteIcon;
    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
                CollectVoteOnSkip = new BetterOptionCheckboxItem().Create(GetOptionUID(true), SettingsTab, Translator.GetString("Role.Mayor.Option.CollectVoteOnSkip"), true, RoleOptionItem),
                CollectVoteWhenVotedOn = new BetterOptionCheckboxItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Mayor.Option.CollectVoteWhenVotedOn"), true, RoleOptionItem),
                AdditionalVoteIcon = new BetterOptionStringItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Mayor.Option.AdditionalVoteIcon"),
                [Translator.GetString("Role.Mayor.AdditionalVoteIcon.Normal"), Translator.GetString("Role.Mayor.AdditionalVoteIcon.Anonymous"), Translator.GetString("Role.Mayor.AdditionalVoteIcon.Hide")],
                1, RoleOptionItem),
            ];
        }
    }


    private PlayerMeetingButton? voteButton;
    private int additionalVotes = 1;
    private List<byte> voted = [];

    public override void OnMeetingStart(MeetingHud meetingHud)
    {
        voted.Clear();
        if (_player.IsLocalPlayer())
        {
            voteButton = new PlayerMeetingButton().Create("Vote", this, LoadAbilitySprite("Vote", 100));
            voteButton.ShowCondition = (pva, targetData) => { return !targetData.IsDead && !targetData.Disconnected && additionalVotes > 0; };
            voteButton.ClickAction = OnVoteButton;
        }
    }

    public override void OnEndVoting(MeetingHud meetingHud)
    {
        foreach (var pva in meetingHud.playerStates)
        {
            if (pva.TargetPlayerId == _player.PlayerId && pva.VotedFor == 253 && CollectVoteOnSkip.GetBool())
            {
                additionalVotes++;
            }
            else if (pva.TargetPlayerId != _player.PlayerId && pva.VotedFor == _player.PlayerId && CollectVoteWhenVotedOn.GetBool())
            {
                additionalVotes++;
            }
        }
    }

    private void OnVoteButton(PassiveButton? button, PlayerVoteArea? pva, NetworkedPlayerInfo? targetData)
    {
        if (MeetingHud.Instance.state is MeetingHud.VoteStates.NotVoted)
        {
            additionalVotes--;
            voted.Add(pva.TargetPlayerId);
            SoundManager.instance.PlaySound(MeetingHud.Instance.VoteLockinSound, false);
        }
        SendRoleSync(0);
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

    public override void SetAbilityAmountTextForMeeting(ref int maxAmount, ref int currentAmount)
    {
        currentAmount = additionalVotes;
    }

    public override void OnSendRoleSync(int syncId, MessageWriter writer, object[]? additionalParams)
    {
        switch (syncId)
        {
            case 0:
                {
                    writer.Write(additionalVotes);
                    writer.Write(voted.Count);
                    foreach (var vote in voted)
                    {
                        writer.Write(vote);
                    }
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
                    additionalVotes = reader.ReadInt32();
                    int count = reader.ReadInt32();
                    List<byte> bytes = [];
                    for (int i = 0; i < count; i++)
                    {
                        bytes.Add(reader.ReadByte());
                    }
                    voted = bytes;
                }
                break;
        }
    }
}
