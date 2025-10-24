using BepInEx.Unity.IL2CPP.Utils;
using Hazel;
using System.Collections;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Monos;
using TheBetterRoles.Network;
using TheBetterRoles.Network.RPCs;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Interfaces;
using UnityEngine;

namespace TheBetterRoles.Roles.Addons;

internal sealed class LoversAddon : AddonClass, IRoleGameplayAction, IRoleDisconnectAction, IRoleMurderAction, IRoleMeetingAction, IRoleGuessAction
{
    internal sealed override int RoleId => 45;
    internal sealed override string RoleColorHex => "#F846D0";
    internal sealed override RoleClass Role => this;
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Lovers;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.None;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.GeneralAddon;
    internal sealed override OptionTab? SettingsTab => TBRTabs.Addons;
    internal sealed override int AmountSize => 2;
    internal sealed override bool CountToPlayerAmount => !_player.Is(RoleClassTeam.Crewmate) || LoverPC?.Is(RoleClassTeam.Crewmate) == false;
    internal sealed override bool ShowRoleAboveName => !PlayerControl.LocalPlayer.IsAlive();
    internal sealed override bool ShowRoleInOutro => true;

    internal OptionItem? RevealLoversRoles;
    internal OptionItem? DieOnOtherLoversDeath;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                RevealLoversRoles = OptionCheckboxItem.Create(GetOptionUID(), SettingsTab, "Role.Lovers.Option.RevealLoversRoles", true, RoleOptions.RoleOptionItem),
                DieOnOtherLoversDeath = OptionCheckboxItem.Create(GetOptionUID(), SettingsTab, "Role.Lovers.Option.DieOnOtherLoversDeath", false, RoleOptions.RoleOptionItem),
            ];
        }
    }

    internal bool isSet;
    private bool superior;
    internal PlayerControl? LoverPC;
    internal NetworkedPlayerInfo? LoverData;

    internal sealed override void SetUpRoleAsHost()
    {
        if (!GameState.IsHost) return;
        CoroutineManager.Instance.StartCoroutine(CoSetPartners());
    }

    internal IEnumerator CoSetPartners()
    {
        yield return new WaitForSeconds(1f);

        if (isSet) yield break;

        if (!GameManager.Instance.GameHasStarted)
        {
            yield return new WaitForSeconds(1f);
        }

        PlayerControl[] players = Main.AllAlivePlayerControls.Where(pc => pc != _player && !pc.Has(RoleClassTypes.Lovers)
        && CanBeAssignedWithTeam(pc.Role()?.RoleTeam ?? RoleClassTeam.Crewmate) && !_player.HasTarget(pc) && !pc.IsTargetOf(_player)).ToArray();
        PlayerControl? target = null;
        if (players.Length > 0)
        {
            target = players.Random();
        }

        if (target != null)
        {
            SetPartner(target);
            SendRoleSync(1, target);
        }
        else
        {
            _player.SendRpcSetCustomRole(RoleClassTypes.Lovers, true);
        }
    }

    private void SetPartner(PlayerControl partner)
    {
        if (isSet) return;
        isSet = true;
        LoverPC = partner;
        LoverData = partner.Data;
        _player.AddTarget(partner, this);
        partner.ExtendedPC().InteractableTargetQueue.Add(false);
        if (!partner.Has(RoleClassTypes.Lovers))
        {
            superior = true;
            var role = CustomRoleManager.AddAddon(partner, RoleClassTypes.Lovers);
            if (role is LoversAddon addon)
            {
                addon.SetPartner(_player);
            }
        }
        _player.DirtyName();
        partner.DirtyName();
    }

    internal sealed override void OnDeinitialize()
    {
        if (LoverPC != null)
        {
            _player.RemoveTarget(LoverPC, this);
            LoverPC.ExtendedPC().InteractableTargetQueue.Add(true);
            LoverPC.SendRpcSetCustomRole(RoleClassTypes.Lovers, true);
        }
    }

    void IRoleMurderAction.MurderOther(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility)
    {
        if (LoverPC == null) return;
        if (killer != _player && target == LoverPC && _player.IsAlive() && DieOnOtherLoversDeath.GetBool())
        {
            LoverPC.SendRpcMurder(_player, true, true, MultiMurderFlags.spawnBody | MultiMurderFlags.showAnimation);
        }
    }

    void GuessEvent(PlayerControl guesser, PlayerControl target, RoleClassTypes role)
    {
        if (LoverPC == null) return;
        if (target == _player || guesser == _player && target.Role()?.RoleType != role)
        {
            LoverPC.CustomExiled();
        }
    }

    void IRoleMeetingAction.ExileEnd(PlayerControl? exiled, NetworkedPlayerInfo? exiledData)
    {
        if (LoverPC == null) return;
        if (exiledData == _data && DieOnOtherLoversDeath.GetBool())
        {
            LoverPC.CustomExiled();
        }
    }

    void IRoleGameplayAction.OnWin()
    {
        if (!LoverData.HasWon())
        {
            LoverData.AddSubWinner();
        }
    }

    void IRoleDisconnectAction.OnDisconnect(PlayerControl target, DisconnectReasons reason)
    {
        if (target == LoverPC)
        {
            Deinitialize();
        }
    }

    internal sealed override string SetNameMark(PlayerControl target)
    {
        if (!superior)
        {
            return string.Empty;
        }
        if (target != LoverPC && target != _player)
        {
            return string.Empty;
        }
        if (!LoverPC.IsLocalPlayer() && !_player.IsLocalPlayer() && PlayerControl.LocalPlayer.IsAlive())
        {
            return string.Empty;
        }

        return "♥".ToColor(RoleColorHex);
    }

    internal sealed override bool RevealPlayerRole(PlayerControl target)
    {
        return target == LoverPC && RevealLoversRoles.GetBool();
    }

    internal sealed override bool RevealPlayerAddons(PlayerControl target)
    {
        return target == LoverPC && RevealLoversRoles.GetBool();
    }

    internal sealed override void FormatRoleInfo(ref string info, bool isLongInfo)
    {
        if (!isLongInfo)
        {
            if (LoverData != null)
            {
                info = string.Format(info, LoverData.GetPlayerNameAndColor());
            }
            else
            {
                info = string.Format(info, "<#FFFFFF>???</color>");
            }
        }
    }

    internal sealed override void OnReceiveRoleSync(int syncId, MessageReader reader, PlayerControl sender)
    {
        switch (syncId)
        {
            case 1:
                {
                    var partner = reader.ReadPlayer();
                    if (partner != null)
                    {
                        SetPartner(partner);
                    }
                }
                break;
        }
    }
}
