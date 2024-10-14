
using BepInEx.Unity.IL2CPP.Utils;
using Hazel;
using System.Collections;
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles;

public class AltruistRole : CustomRoleBehavior
{
    // Role Info
    public override string RoleColor => "#BA0400";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Altruist;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Crewmate;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Support;
    public override BetterOptionTab? SettingsTab => BetterTabs.CrewmateRoles;

    public BetterOptionItem? ReviveCooldown;
    public BetterOptionItem? ReviveDuration;
    public BetterOptionItem? KillOnRevive;

    public DeadBodyButton? ReviveButton;
    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
                ReviveCooldown = new BetterOptionFloatItem().Create(GenerateOptionId(true), SettingsTab, Translator.GetString("Role.Altruist.Option.ReviveCooldown"), [0f, 180f, 2.5f], 25f, "", "s", RoleOptionItem),
                ReviveDuration = new BetterOptionFloatItem().Create(GenerateOptionId(), SettingsTab, Translator.GetString("Role.Altruist.Option.ReviveDuration"), [0f, 180f, 2.5f], 10f, "", "s", RoleOptionItem),
                KillOnRevive = new BetterOptionCheckboxItem().Create(GenerateOptionId(), SettingsTab, Translator.GetString("Role.Altruist.Option.KillOnRevive"), true, RoleOptionItem),
            ];
        }
    }
    public override void OnSetUpRole()
    {
        ReviveButton = AddButton(new DeadBodyButton().Create(5, Translator.GetString("Role.Altruist.Ability.1"), 0, ReviveDuration.GetFloat(), 0, null, this, true, 1));
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
                    if (body != null)
                    {
                        ReviveButton.SetDuration();
                        CheckRevive(body);
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
            OnResetAbilityState();
        }
    }

    public override void OnResetAbilityState(bool isTimeOut = false)
    {
        if (!isTimeOut)
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
            if (_player.IsLocalPlayer())
            {
                Utils.FlashScreen(RoleColor, 0f, 0.25f, 0.25f, true);
                _player.MyPhysics.Animations.PlayScanner(false, false, _player.MyPhysics.FlipX);
                _player.MyPhysics.Animations.scannersImages.ToList().ForEach(sr => sr.color = Color.white);
                SetMovement(_player, true);
            }
            _player.StopCoroutine(reviveCoroutine);
            reviveCoroutine = null;
            isReviving = false;
        }
    }

    private void CheckRevive(DeadBody? body)
    {
        PlayerControl? target = Utils.PlayerFromPlayerId(body.ParentId);

        if (target != null && body != null)
        {
            isReviving = true;
            reviveCoroutine = _player.StartCoroutine(StartRevive(target, body));
        }
    }

    private IEnumerator StartRevive(PlayerControl target, DeadBody body)
    {
        if (_player.IsLocalPlayer())
        {
            Utils.FlashScreen(RoleColor, 0.25f, 0.25f, ReviveDuration.GetFloat());
            _player.MyPhysics.Animations.PlayScanner(true, false, _player.MyPhysics.FlipX);
            _player.MyPhysics.Animations.scannersImages.ToList().ForEach(sr => sr.color = Utils.HexToColor32(RoleColor));
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
    }

    private void Revive(PlayerControl target, DeadBody body)
    {
        isReviving = false;
        target.NetTransform.SnapTo(body.transform.position);
        if (_player.IsLocalPlayer() && KillOnRevive.GetBool())
        {
            _player.MurderSync(_player, true, false, true, false, false);
        }
        target.Revive();
        UnityEngine.Object.Destroy(body.gameObject);
    }

    private void SetMovement(PlayerControl source, bool canMove)
    {
        source.moveable = canMove;
        source.MyPhysics.ResetMoveState(false);
        source.NetTransform.enabled = canMove;
        source.MyPhysics.enabled = canMove;
        source.NetTransform.Halt();
    }
}