using BepInEx.Unity.IL2CPP.Utils;
using Hazel;
using System.Collections;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Interfaces;
using UnityEngine;

namespace TheBetterRoles.Roles.Addons;

internal sealed class TiebreakerAddon : AddonClass, IRoleMeetingAction
{
    internal sealed override int RoleId => 46;
    internal sealed override string RoleColorHex => "#56A450";
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Tiebreaker;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.None;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.HelpfulAddon;
    internal sealed override OptionTab? SettingsTab => TBRTabs.Addons;

    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
            ];
        }
    }

    void IRoleMeetingAction.EndVoting(MeetingHud meetingHud, ref Dictionary<byte, int> calculatedVotes)
    {
        var myVote = meetingHud.playerStates.FirstOrDefault(pva => pva.TargetPlayerId == _player.PlayerId).VotedFor;
        var myVotePva = meetingHud.playerStates.FirstOrDefault(pva => pva.TargetPlayerId == myVote);
        calculatedVotes.MaxPair(out bool tie);
        if (tie)
        {
            calculatedVotes[myVote] = 100;
            CoroutineManager.Instance.StartCoroutine(CoTiebreakAnimation(myVotePva));
            SendRoleSync(0, myVote);
        }
    }

    private static IEnumerator CoTiebreakAnimation(PlayerVoteArea pva, float magnitude = 0.01f)
    {
        if (pva == null) yield break;

        Vector3 originalPosition = pva.transform.position;

        try
        {
            while (GameState.IsMeeting && pva != null)
            {
                float offsetX = UnityEngine.Random.Range(-magnitude, magnitude);
                float offsetY = UnityEngine.Random.Range(-magnitude, magnitude);

                pva.transform.position = originalPosition + new Vector3(offsetX, offsetY, 0);

                yield return null;
            }
        }
        finally
        {
            if (pva != null)
            {
                pva.transform.position = originalPosition;
            }
        }
    }

    internal sealed override void OnReceiveRoleSync(int syncId, MessageReader reader, PlayerControl sender)
    {
        switch (syncId)
        {
            case 0:
                {
                    var myVote = reader.ReadByte();
                    var myVotePva = MeetingHud.Instance.playerStates.FirstOrDefault(pva => pva.TargetPlayerId == myVote);
                    CoroutineManager.Instance.StartCoroutine(CoTiebreakAnimation(myVotePva));
                }
                break;
        }
    }
}
