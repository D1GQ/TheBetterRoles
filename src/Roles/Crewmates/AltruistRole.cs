using BepInEx.Unity.IL2CPP.Utils;
using System.Collections;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Modules;
using TheBetterRoles.Network.RPCs;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core.Interfaces;
using UnityEngine;

namespace TheBetterRoles.Roles.Crewmates;

internal sealed class AltruistRole : CrewmateRoleTBR, IRoleMurderAction, IRoleAbilityAction<DeadBody>
{
    internal sealed override int RoleId => 8;
    internal sealed override string RoleColorHex => "#BA0400";
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Altruist;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Crewmate;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Support;
    internal sealed override OptionTab? SettingsTab => TBRTabs.CrewmateRoles;

    internal OptionItem? ReviveCooldown;
    internal OptionItem? ReviveDuration;
    internal OptionItem? KillOnRevive;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                ReviveCooldown = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Altruist.Option.ReviveCooldown", (0f, 180f, 2.5f), 15f, ("", "s"), RoleOptions.RoleOptionItem),
                ReviveDuration = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Altruist.Option.ReviveDuration", (0f, 180f, 2.5f), 10f, ("", "s"), RoleOptions.RoleOptionItem),
                KillOnRevive = OptionCheckboxItem.Create(GetOptionUID(), SettingsTab, "Role.Altruist.Option.KillOnRevive", true, RoleOptions.RoleOptionItem),
            ];
        }
    }

    private bool isReviving;
    private Coroutine? reviveCoroutine;
    internal DeadBodyAbilityButton? ReviveButton;
    internal sealed override void OnSetUpRole()
    {
        if (_player.IsLocalPlayer())
        {
            ReviveButton = RoleButtons.AddButton(DeadBodyAbilityButton.Create(5, Translator.GetString("Role.Altruist.Ability.1"), ReviveCooldown.GetFloat(), ReviveDuration.GetFloat(), 0, null, this, true, 0f));
            ReviveButton.CanCancelDuration = true;
            ReviveButton.DeadBodyCondition = (body) =>
            {
                return body.enabled;
            };
        }
    }

    internal sealed override void OnDeinitialize()
    {
        CancelRevive();
    }

    void IRoleAbilityAction<DeadBody>.OnAbility(int id, DeadBody target)
    {
        switch (id)
        {
            case 5:
                {
                    if (target != null && _player.IsLocalPlayer())
                    {
                        if (CheckRevive(target) == true)
                        {
                            ReviveButton?.SetDuration();
                        }
                        else
                        {
                            if (_player.IsLocalPlayer())
                            {
                                _player.ShieldBreakAnimation(RoleColorHex);
                            }
                        }
                    }
                }
                break;
        }
    }

    void IRoleAbilityAction.AbilityDurationEnd(int id, bool isTimeOut)
    {
        switch (id)
        {
            case 5:
                {
                    ((IRoleAbilityAction)this).OnResetAbilityState(isTimeOut);
                }
                break;
        }
    }

    void IRoleMurderAction.MurderOther(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility)
    {
        if (target == _player)
        {
            ((IRoleAbilityAction)this).OnResetAbilityState(false);
        }
    }

    void IRoleAbilityAction.OnResetAbilityState(bool isTimeOut)
    {
        if (!isTimeOut && _player.IsLocalPlayer())
        {
            CancelRevive();
        }
    }

    private void CancelRevive()
    {
        if (isReviving)
        {
            ScreenFlash.Stop("altruist");
            _player.MyPhysics.Animations.PlayScanner(false, false, _player.MyPhysics.FlipX);
            _player.MyPhysics.Animations.scannersImages.ToList().ForEach(sr => sr.color = Color.white);
            SetMovement(_player, true);
            _roleMono.StopCoroutine(reviveCoroutine);
            reviveCoroutine = null;
            isReviving = false;
        }
    }

    private bool CheckRevive(DeadBody? body)
    {
        PlayerControl? target = Utils.PlayerFromPlayerId(body.ParentId);

        if (body != null && !target.IsGhostRole() && !target.IsAlive())
        {
            isReviving = true;
            reviveCoroutine = _roleMono.StartCoroutine(CoStartRevive(target, body));
            return true;
        }

        return false;
    }

    private IEnumerator CoStartRevive(PlayerControl player, DeadBody body)
    {
        if (_player.IsLocalPlayer())
        {
            Utils.FlashScreen("altruist", RoleColorHex, 0.25f, 0.25f, ReviveDuration.GetFloat());
            _player.MyPhysics.Animations.PlayScanner(true, false, _player.MyPhysics.FlipX);
            _player.MyPhysics.Animations.scannersImages.ToList().ForEach(sr => sr.color = RoleColor);
            SetMovement(_player, false);
        }

        float duration = ReviveDuration.GetFloat();
        float checkInterval = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (body == null)
            {
                CancelRevive();
                yield break;
            }

            yield return new WaitForSeconds(checkInterval);
            elapsed += checkInterval;
        }

        if (_player.IsLocalPlayer())
        {
            _player.MyPhysics.Animations.PlayScanner(false, false, _player.MyPhysics.FlipX);
            _player.MyPhysics.Animations.scannersImages.ToList().ForEach(sr => sr.color = Color.white);
            SetMovement(_player, true);
        }

        Revive(player, body);
    }

    private void Revive(PlayerControl target, DeadBody body)
    {
        if (target == null) return;

        isReviving = false;

        body.SendRpcRemoveBody();
        target.SendSnapTo(body.TruePosition);
        target.SendRpcRevive();
        if (KillOnRevive.GetBool())
        {
            _player.SendRpcMurder(_player, true, true, MultiMurderFlags.spawnBody | MultiMurderFlags.playSound);
        }
    }

    private void SetMovement(PlayerControl player, bool canMove)
    {
        player.moveable = canMove;
        player.MyPhysics.ResetMoveState(false);
        player.NetTransform.enabled = canMove;
        player.MyPhysics.enabled = canMove;
        player.NetTransform.Halt();
    }

    internal sealed override bool HidePlayerInfoOther(PlayerControl target)
    {
        if (_player.IsAlive() && !localPlayer.IsAlive() && !target.IsLocalPlayer() && localPlayer.DeadBody() != null)
        {
            return true;
        }

        return false;
    }
}