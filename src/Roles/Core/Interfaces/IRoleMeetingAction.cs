using TheBetterRoles.Managers;

namespace TheBetterRoles.Roles.Core.Interfaces;

internal interface IRoleMeetingAction : IRoleAction
{
    /// <summary>
    /// Called before a meeting is called
    /// </summary>
    void MeetingBegin(MeetingHud meetingHud) { }

    /// <summary>
    /// Called when a meeting is called. Only really used to create meeting ability buttons.
    /// </summary>
    void MeetingStart(MeetingHud meetingHud) { }

    /// <summary>
    /// Returns text that will appear on the meeting hud on start.
    /// Priority is a output parameter representing the priority of this text entry, where higher values indicate a higher display priority.
    /// </summary>
    string AddMeetingText(ref CustomClip? clip, out uint priority)
    {
        priority = uint.MinValue;
        return string.Empty;
    }

    /// <summary>
    /// Called on meeting end, add additional votes to votedFor.
    /// This code is only ran by the host!
    /// </summary>
    int AddVotes(MeetingHud meetingHud, PlayerVoteArea pva) => 0;

    /// <summary>
    /// Called on meeting end, add visual votes on players vote area.
    /// </summary>
    void AddVisualVotes(MeetingHud meetingHud, PlayerVoteArea votedFor, ref List<MeetingHud.VoterState> states) { }

    /// <summary>
    /// Called on meeting end, add additional votes for calculation of exiled.
    /// This code is only ran by the host!
    /// </summary>
    void AddAdditionalVotes(MeetingHud meetingHud, ref Dictionary<byte, int> votes) { }

    /// <summary>
    /// Called after a meeting has ended, converting PlayerVoteArea info to VoterState.
    /// This code is only ran by the host!
    /// </summary>
    void EndVoting(MeetingHud meetingHud, ref Dictionary<byte, int> calculatedVotes) { }

    /// <summary>
    /// Called after an exile has concluded, handling the logic for the player who was exiled.
    /// </summary>
    void ExileEnd(PlayerControl? exiled, NetworkedPlayerInfo? exiledData) { }
}