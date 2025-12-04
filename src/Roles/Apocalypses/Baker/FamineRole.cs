using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Core.RoleBase;
using TheBetterRoles.Roles.Interfaces;

namespace TheBetterRoles.Roles.Apocalypses;

internal sealed class FamineRole : RoleClass, IRoleAbilityAction<PlayerControl>, IRoleMeetingAction
{
    internal sealed override int RoleId => 52;
    internal sealed override string RoleColorHex => "#83461C";
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Famine;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Apocalypse;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Evil;
    internal sealed override OptionTab? SettingsTab => TBRTabs.ApocalypseRoles;
    internal sealed override bool CanBeAssigned => false;
    internal sealed override bool CanVent => CustomRoleManager.GetRolePrefab<BakerRole>().CanVent;
    internal sealed override bool HasImpostorVision => CustomRoleManager.GetRolePrefab<BakerRole>().HasImpostorVision;

    internal OptionItem? FamineStarveCooldown => CustomRoleManager.GetRolePrefab<BakerRole>().FamineStarveCooldown;
    internal OptionItem? AbilityDistance => CustomRoleManager.GetRolePrefab<BakerRole>().AbilityDistance;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
            ];
        }
    }

    internal bool WasTransformed = false;

    internal List<NetworkedPlayerInfo> StarvePlayers = [];
    internal PlayerAbilityButton? StarveButton;
    internal sealed override void OnSetUpRole()
    {
        if (_player.IsLocalPlayer())
        {
            StarveButton = RoleButtons.AddButton(new PlayerAbilityButton().Create(5, Translator.GetString("Role.Famine.Ability.1"), FamineStarveCooldown.GetFloat(), 0, 0, null, this, true, AbilityDistance.GetStringValue()));
            StarveButton.TargetCondition = (target) =>
            {
                return !StarvePlayers.Contains(target.Data);
            };
        }
    }

    void IRoleAbilityAction<PlayerControl>.OnAbility(int id, PlayerControl target)
    {
        switch (id)
        {
            case 5:
                {
                    StarvePlayer(target);
                    Networked.SendRoleSync(target);
                }
                break;
        }
    }

    private void StarvePlayer(PlayerControl target)
    {
        StarvePlayers.Add(target.Data);
    }

    void IRoleMeetingAction.MeetingBegin(MeetingHud meetingHud)
    {
        foreach (var data in StarvePlayers)
        {
            var target = Utils.PlayerFromPlayerId(data.PlayerId);
            if (target == null || target.IsLocalPlayer() || !target.IsAlive()) continue;
            target.CustomExiled();
            target.SetDeathReason(DeathReasons.Starved, RoleColor);
        }
        StarvePlayers.Clear();
    }

    void IRoleMeetingAction.ExileEnd(PlayerControl? exiled, NetworkedPlayerInfo? exiledData)
    {
        foreach (var data in StarvePlayers)
        {
            var target = Utils.PlayerFromPlayerId(data.PlayerId);
            if (target == null || target.IsLocalPlayer() || !target.IsAlive()) continue;
            target.CustomExiled();
            target.SetDeathReason(DeathReasons.Starved, RoleColor);
        }
        StarvePlayers.Clear();
    }

    string IRoleMeetingAction.AddMeetingText(ref CustomClip? clip, out uint priority)
    {
        priority = 200;
        if (WasTransformed)
        {
            clip = new CustomClip() { ClipName = "Transform", Volume = 2f };
            WasTransformed = false;
            return $"<{CustomRoleManager.GetRolePrefab<BakerRole>().RoleColorHex}>{Translator.GetString("Role.Baker.TransformMsg")}</color>";
        }
        return string.Empty;
    }

    internal sealed override string SetNameMark(PlayerControl target)
    {
        if (_player.IsLocalPlayer() && target.IsAlive() && StarvePlayers.Contains(target.Data))
        {
            return "◯".ToColor(RoleColorHex);
        }

        return string.Empty;
    }

    internal override void OnReceiveRoleSync(RoleNetworked.Data data)
    {
        StarvePlayer(data.MessageReader.ReadFast<PlayerControl>());
    }
}
