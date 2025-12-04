using Hazel;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Modules;
using TheBetterRoles.Network;
using TheBetterRoles.Network.RPCs;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Core.RoleBase;
using TheBetterRoles.Roles.Interfaces;

namespace TheBetterRoles.Roles.Neutrals;

internal sealed class ExecutionerRole : RoleClass, IRoleMeetingAction, IRoleDeathAction, IRoleDisconnectAction, IRoleGameplayAction
{
    internal sealed override int RoleId => 41;
    internal sealed override string RoleColorHex => "#919191";
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Executioner;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Neutral;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Evil;
    internal sealed override OptionTab? SettingsTab => TBRTabs.NeutralRoles;
    internal sealed override bool MeetingReliantRole => true;
    internal sealed override bool DefaultCanCallMeetingOption => true;

    private static Dictionary<int, RoleClassTypes> OptionToRole => new()
    {
        { 0, RoleClassTypes.Crewmate },
        { 1, RoleClassTypes.Opportunist },
        { 2, RoleClassTypes.Jester }
    };

    internal OptionItem? MercyTargeting;
    internal OptionItem? RoleOnTargetDead;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                MercyTargeting = OptionCheckboxItem.Create(GetOptionUID(), SettingsTab, "Role.Executioner.Option.MercyTargeting", false, RoleOptions.RoleOptionItem),
                RoleOnTargetDead = OptionStringItem.Create(GetOptionUID(), SettingsTab, "Role.Executioner.Option.RoleOnTargetDead",
                [Utils.GetCustomRoleNameAndColor(RoleClassTypes.Crewmate), Utils.GetCustomRoleNameAndColor(RoleClassTypes.Opportunist), Utils.GetCustomRoleNameAndColor(RoleClassTypes.Jester)], -1, RoleOptions.RoleOptionItem),
            ];
        }
    }

    private PlayerControl? targetPlayer;
    private NetworkedPlayerInfo? targetData;
    private bool HasVotedOutTarget = false;

    internal sealed override void SetUpRoleAsHost()
    {
        TrySetTarget();
    }

    private void TrySetTarget()
    {
        if (!GameState.IsHost) return;

        var player = Main.AllAlivePlayerControls.Where(pc => pc != _player && !_player.HasTarget(pc)).Random();
        if (player != null)
        {
            SetTarget(player);
            MarkDirty();
        }
        else
        {
            SyncSetRoleOnTargetDeath();
        }
    }

    internal sealed override void OnDeinitialize()
    {
        if (targetPlayer != null)
        {
            _player.RemoveTarget(targetPlayer, this);
            if (_player.IsLocalPlayer())
            {
                targetPlayer.ExtendedData().NameColor = string.Empty;
            }
        }
    }

    private void SetTarget(PlayerControl target)
    {
        UnsetTarget();

        targetPlayer = target;
        targetData = target.Data;
        _player.AddTarget(targetPlayer, this);
        if (_player.IsLocalPlayer())
        {
            targetPlayer.ExtendedData().NameColor = "#202020";
        }
    }

    internal void UnsetTarget()
    {
        if (targetPlayer != null)
        {
            _player.RemoveTarget(targetPlayer, this);
            if (_player.IsLocalPlayer())
            {
                targetPlayer.ExtendedData().NameColor = string.Empty;
            }
        }
    }

    void IRoleMeetingAction.ExileEnd(PlayerControl? exiled, NetworkedPlayerInfo? exiledData)
    {
        if (exiledData == targetData && _player.IsAlive())
        {
            HasVotedOutTarget = true;
            CheckWinCondition();
        }
    }

    void IRoleDisconnectAction.OnDisconnect(PlayerControl player, DisconnectReasons reason)
    {
        if (!GameState.IsHost) return;

        if (player == targetPlayer && !HasVotedOutTarget)
        {
            if (MercyTargeting.GetBool())
            {
                TrySetTarget();
            }
            else
            {
                SyncSetRoleOnTargetDeath();
            }
        }
    }

    void IRoleDeathAction.OnDeathOther(PlayerControl player, DeathReasons reason)
    {
        if (!GameState.IsHost) return;

        if (player == targetPlayer && !HasVotedOutTarget)
        {
            if (MercyTargeting.GetBool())
            {
                TrySetTarget();
            }
            else
            {
                SyncSetRoleOnTargetDeath();
            }
        }
    }

    private void SyncSetRoleOnTargetDeath()
    {
        if (_player.IsLocalPlayer())
        {
            SetRoleOnTargetDeath();
        }
        else
        {
            Networked.SendRoleSync(1);
        }
    }

    private void SetRoleOnTargetDeath()
    {
        if (!_player.IsLocalPlayer()) return;

        var role = _player.SendRpcSetCustomRole(OptionToRole[RoleOnTargetDead.GetStringValue()]);
        if (role != null)
        {
            _player.ShieldBreakAnimation(role.RoleColorHex);
        }
    }

    internal sealed override void FormatRoleInfo(ref string info, bool isLongInfo)
    {
        if (!isLongInfo)
        {
            if (targetPlayer != null)
            {
                info = string.Format(info, targetPlayer.GetPlayerNameAndColor());
            }
            else
            {
                info = string.Format(info, "<#FFFFFF>???</color>");
            }
        }
    }

    internal sealed override string SetNameMark(PlayerControl target)
    {
        if (!_player.IsLocalPlayer() && localPlayer.IsAlive()) return string.Empty;
        if (!GameState.IsMeeting) return string.Empty;
        if (target != targetPlayer) return string.Empty;

        return $"<{RoleColorHex}>☜</color>";
    }

    bool IRoleGameplayAction.WinCondition() => HasVotedOutTarget;

    internal sealed override void OnReceiveRoleSync(RoleNetworked.Data data)
    {
        switch (data.SyncId)
        {
            case 1:
                SetRoleOnTargetDeath();
                break;
        }
    }

    public override void Serialize(MessageWriter writer)
    {
        writer.WritePlayer(targetPlayer);
        ClearDirtyBits();
    }

    public override void Deserialize(MessageReader reader)
    {
        if (targetPlayer != null) return;

        var player = reader.ReadPlayer();
        if (player != null)
        {
            SetTarget(player);
        }
    }
}
