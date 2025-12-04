using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Network;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Core.RoleBase;
using TheBetterRoles.Roles.Interfaces;
using UnityEngine;

namespace TheBetterRoles.Roles.Apocalypses;

internal class PlaguebearerRole : RoleClass, IRoleMurderAction, IRoleAbilityAction<PlayerControl>, IRoleMeetingAction, IRoleInteractedAction, IRoleDisconnectAction
{
    internal sealed override int RoleId => 17;
    internal sealed override string RoleColorHex => "#97BD3D";
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Plaguebearer;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Apocalypse;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Benign;
    internal sealed override OptionTab? SettingsTab => TBRTabs.ApocalypseRoles;
    internal sealed override AudioClip? IntroSound => Prefab.GetCachedPrefab<ShapeshifterRole>().IntroSound;

    internal OptionItem? InfectCooldown;
    internal OptionItem? InfectDistance;
    internal OptionItem? PestilenceKillCooldown;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                InfectCooldown = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Plaguebearer.Option.InfectCooldown", (0f, 180f, 2.5f), 25, ("", "s"), RoleOptions.RoleOptionItem),
                InfectDistance = OptionStringItem.Create(GetOptionUID(), SettingsTab, "Role.Plaguebearer.Option.InfectDistance", ["Role.Option.Distance.1", "Role.Option.Distance.2", "Role.Option.Distance.3"], 1, RoleOptions.RoleOptionItem),
                PestilenceKillCooldown = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Plaguebearer.Option.PestilenceKillCooldown", (0f, 180f, 2.5f), 25, ("", "s"), RoleOptions.RoleOptionItem),
            ];
        }
    }

    private readonly List<NetworkedPlayerInfo> infected = [];
    internal PlayerAbilityButton? InfectButton;
    internal sealed override void OnSetUpRole()
    {
        if (_player.IsLocalPlayer())
        {
            InfectButton = RoleButtons.AddButton(new PlayerAbilityButton().Create(5, Translator.GetString("Role.Plaguebearer.Ability.1"), InfectCooldown.GetFloat(), 0, 0, null, this, true, InfectDistance.GetStringValue()));
            InfectButton.TargetCondition = (target) =>
            {
                return !infected.Contains(target.Data);
            };
        }
    }

    void IRoleAbilityAction<PlayerControl>.OnAbility(int id, PlayerControl target)
    {
        switch (id)
        {
            case 5:
                {
                    InfectPlayer(target);
                    Networked.SendRoleSync(0, target);
                }
                break;
        }
    }

    internal sealed override void OnDeinitialize()
    {
        if (_player.IsLocalPlayer())
        {
            foreach (var data in infected)
            {
                var player = data.Object;
                if (player != null)
                {
                    player.SetTrueVisorColor(Palette.VisorColor);
                    player.ExtendedData().NameColor = string.Empty;
                }
            }
        }
    }

    // Infact player on interactions
    void IRoleInteractedAction.PlayerInteractedOther(PlayerControl player, PlayerControl target)
    {
        if (target == _player && !infected.Contains(player.Data))
        {
            InfectPlayer(player);
        }
        else if (infected.Contains(player.Data) && !infected.Contains(target.Data))
        {
            InfectPlayer(target);
        }
    }

    void IRoleMeetingAction.ExileEnd(PlayerControl? exiled, NetworkedPlayerInfo? exiledData)
    {
        CheckPestillenceCondition();
    }

    void IRoleMurderAction.MurderOther(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility)
    {
        CheckPestillenceCondition();
    }

    void IRoleDisconnectAction.OnDisconnect(PlayerControl player, DisconnectReasons reason)
    {
        if (player == _player) return;

        CheckPestillenceCondition();
    }

    private void InfectPlayer(PlayerControl player)
    {
        infected.Add(player.Data);
        if (_player.IsLocalPlayer())
        {
            player.SetTrueVisorColor(RoleColor);
            player.ExtendedData().NameColor = RoleColorHex;
        }

        CheckPestillenceCondition();
    }

    private void CheckPestillenceCondition()
    {
        if (!_player.IsLocalPlayer()) return;

        if (Main.AllAlivePlayerControls.Where(pc => pc != _player).Select(pc => pc.Data).All(infected.Contains))
        {
            if (_player.IsLocalPlayer()) CustomSoundsManager.Instance.Play(Sounds.Transform);
            SetPestillence();
            Networked.SendRoleSync(1);
        }
    }

    private void SetPestillence()
    {
        var role = CustomRoleManager.SetCustomRole(_player, RoleClassTypes.Pestillence);
        if (role is PestillenceRole Pestillence)
        {
            Pestillence.WasTransformed = true;
        }
    }

    internal override void OnReceiveRoleSync(RoleNetworked.Data data)
    {
        switch (data.SyncId)
        {
            case 0:
                InfectPlayer(data.MessageReader.ReadFast<PlayerControl>());
                break;
            case 1:
                if (data.Sender == _player)
                {
                    SetPestillence();
                }
                break;
        }
    }
}