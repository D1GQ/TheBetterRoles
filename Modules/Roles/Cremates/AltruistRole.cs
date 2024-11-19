using BepInEx.Unity.IL2CPP.Utils;
using Hazel;
using System.Collections;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using TheBetterRoles.RPCs;
using UnityEngine;

namespace TheBetterRoles.Roles;

public class AltruistRole : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 8;
    public override string RoleColor => "#BA0400";
    public override CustomRoleBehavior Role => this;
    public override CustomRoleType RoleType => CustomRoleType.Altruist;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Crewmate;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Support;
    public override TBROptionTab? SettingsTab => BetterTabs.CrewmateRoles;

    public TBROptionItem? ReviveCooldown;
    public TBROptionItem? ReviveDuration;
    public TBROptionItem? KillOnRevive;

    public DeadBodyAbilityButton? ReviveButton;
    public override TBROptionItem[]? OptionItems
    {
        get
        {
            return
            [
                ReviveCooldown = new TBROptionFloatItem().Create(GetOptionUID(true), SettingsTab, Translator.GetString("Role.Altruist.Option.ReviveCooldown"), [0f, 180f, 2.5f], 15f, "", "s", RoleOptionItem),
                ReviveDuration = new TBROptionFloatItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Altruist.Option.ReviveDuration"), [0f, 180f, 2.5f], 10f, "", "s", RoleOptionItem),
                KillOnRevive = new TBROptionCheckboxItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Altruist.Option.KillOnRevive"), true, RoleOptionItem),
            ];
        }
    }
    public override void OnSetUpRole()
    {
        ReviveButton = AddButton(new DeadBodyAbilityButton().Create(5, Translator.GetString("Role.Altruist.Ability.1"), ReviveCooldown.GetFloat(), ReviveDuration.GetFloat(), 0, null, this, true, 0f));
        ReviveButton.CanCancelDuration = true;
        ReviveButton.DeadBodyCondition = (DeadBody body) =>
        {
            return body.enabled;
        };
    }

    public override void OnDeinitialize()
    {
        CancelRevive();
    }

    public override void OnAbility(int id, MessageReader? reader, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body)
    {
        switch (id)
        {
            case 5:
                {
                    if (body != null && _player.IsLocalPlayer())
                    {
                        if (CheckRevive(body) == true)
                        {
                            ReviveButton?.SetDuration();
                        }
                        else
                        {
                            if (_player.IsLocalPlayer())
                            {
                                _player.ShieldBreakAnimation(RoleColor);
                            }
                        }
                    }
                }
                break;
        }
    }

    public override void OnAbilityDurationEnd(int id, bool isTimeOut)
    {
        switch (id)
        {
            case 5:
                {
                    OnResetAbilityState(isTimeOut);
                }
                break;
        }
    }

    public override void OnMurderOther(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility)
    {
        if (target == _player)
        {
            OnResetAbilityState(false);
        }
    }

    public override void OnResetAbilityState(bool isTimeOut)
    {
        if (!isTimeOut && _player.IsLocalPlayer())
        {
            CancelRevive();
        }
    }

    private bool isReviving;
    private Coroutine? reviveCoroutine;

    private void CancelRevive()
    {
        if (isReviving)
        {
            Utils.FlashScreen(RoleColor, 0f, 0.25f, 0.25f, true);
            _player.MyPhysics.Animations.PlayScanner(false, false, _player.MyPhysics.FlipX);
            _player.MyPhysics.Animations.scannersImages.ToList().ForEach(sr => sr.color = Color.white);
            SetMovement(_player, true);
            CoroutineManager.Instance.StopCoroutine(reviveCoroutine);
            reviveCoroutine = null;
            isReviving = false;
        }
    }

    private bool CheckRevive(DeadBody? body)
    {
        PlayerControl? target = Utils.PlayerFromPlayerId(body.ParentId);

        if (body != null && !CustomRoleManager.RoleChecksAny(target, role => role.IsGhostRole))
        {
            isReviving = true;
            reviveCoroutine = CoroutineManager.Instance.StartCoroutine(CoStartRevive(target, body));
            return true;
        }

        return false;
    }

    private IEnumerator CoStartRevive(PlayerControl target, DeadBody body)
    {
        if (_player.IsLocalPlayer())
        {
            Utils.FlashScreen(RoleColor, 0.25f, 0.25f, ReviveDuration.GetFloat());
            _player.MyPhysics.Animations.PlayScanner(true, false, _player.MyPhysics.FlipX);
            _player.MyPhysics.Animations.scannersImages.ToList().ForEach(sr => sr.color = RoleColor32);
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

        Revive(target, body);
        SendRoleSync(0, [target, body]);
    }

    private void Revive(PlayerControl target, DeadBody body)
    {
        if (target == null) return;

        isReviving = false;
        if (_player.IsLocalPlayer())
        {
            target.SendSnapTo(body.TruePosition);
            target.SendRpcRevive();
            if (KillOnRevive.GetBool())
            {
                _player.SendRpcMurder(_player, true, MultiMurderFlags.spawnBody | MultiMurderFlags.playSound);
            }
        }
        target.CustomRevive();
        body.DestroyObj();
        SendRoleSync(0, [body]);
    }

    private void SetMovement(PlayerControl player, bool canMove)
    {
        player.moveable = canMove;
        player.MyPhysics.ResetMoveState(false);
        player.NetTransform.enabled = canMove;
        player.MyPhysics.enabled = canMove;
        player.NetTransform.Halt();
    }

    public override void OnSendRoleSync(int syncId, MessageWriter writer, object[]? additionalParams)
    {
        switch (syncId)
        {
            case 0:
                {
                    writer.WriteDeadBodyId((DeadBody)additionalParams[0]);
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
                    var body = reader.ReadDeadBodyId();
                    body?.DestroyObj();
                }
                break;
        }
    }
}