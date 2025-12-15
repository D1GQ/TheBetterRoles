using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Modules;
using TheBetterRoles.Monos;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core.Interfaces;
using TheBetterRoles.Roles.Core.RoleBase;
using UnityEngine;

namespace TheBetterRoles.Roles.Impostors;

internal sealed class PenguinRole : ImpostorRoleTBR, IRoleAbilityAction<PlayerControl>, IRoleUpdateAction
{
    internal sealed override int RoleId => 49;
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Penguin;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Impostor;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Support;
    internal sealed override OptionTab? SettingsTab => TBRTabs.ImpostorRoles;
    internal sealed override bool DefaultVentOption => false;

    internal OptionItem? DragCooldown;
    internal OptionItem? DragDuration;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                DragCooldown = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Penguin.Option.DragCooldown", (0f, 180f, 2.5f), 20f, ("", "s"), RoleOptions.RoleOptionItem),
                DragDuration = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Penguin.Option.DragDuration", (0f, 180f, 2.5f), 10f, ("", "s"), RoleOptions.RoleOptionItem, canBeInfinite: true),
            ];
        }
    }

    private bool IsDragging = false;
    private PlayerControl Dragging;
    internal PlayerAbilityButton? DragButton = new();
    internal sealed override void OnSetUpRole()
    {
        if (_player.IsLocalPlayer())
        {
            DragButton = RoleButtons.AddButton(PlayerAbilityButton.Create(5, Translator.GetString("Role.Penguin.Ability.1"), DragCooldown.GetFloat(), DragDuration.GetFloat(), 0, null, this, true, 0f));
            DragButton.CanCancelDuration = true;
            DragButton.InteractCondition = () => { return !GameState.CamouflageCommsIsActive || DragButton.IsDuration; };
        }
    }

    void IRoleAbilityAction<PlayerControl>.OnAbility(int id, PlayerControl target)
    {
        switch (id)
        {
            case 5:
                {
                    DragTarget(target);
                    Networked.SendRoleSync(target);
                    DragButton?.SetDuration();
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

    private void DragTarget(PlayerControl target)
    {
        if (IsDragging) return;
        Dragging = target;
        IsDragging = true;
        Dragging.NetTransform.Halt();
        Dragging.NetTransform.enabled = false;
        Dragging.MyPhysics.enabled = false;
        if (Dragging.IsLocalPlayer())
        {
            Minigame.Instance?.Close();
            Dragging.moveable = false;
        }
        if (!_player.IsLocalPlayer())
        {
            Dragging.ExtendedPC().InteractableTargetQueue.Add(false);
        }
    }

    private void TryReleaseTarget()
    {
        if (IsDragging)
        {
            IsDragging = false;
            Dragging.NetTransform.enabled = true;
            Dragging.NetTransform.Halt();
            Dragging.MyPhysics.enabled = true;
            if (Dragging.IsLocalPlayer()) Dragging.moveable = true;
            Dragging = null;
            if (_player.IsLocalPlayer())
            {
                DragButton.SetCooldown(durationState: 0);
            }
            if (!_player.IsLocalPlayer())
            {
                Dragging.ExtendedPC().InteractableTargetQueue.Add(true);
            }
        }
    }

    void IRoleAbilityAction.OnResetAbilityState(bool isTimeOut)
    {
        TryReleaseTarget();
    }

    void IRoleUpdateAction.Update()
    {
        if (Dragging == null || !IsDragging)
        {
            return;
        }

        if (_player.IsInVent() || !_player.IsAlive() || !Dragging.IsAlive())
        {
            if (_player.IsLocalPlayer()) Dragging.ShieldBreakAnimation(RoleColorHex);
            TryReleaseTarget();
        }

        try
        {
            var rigidbody = Dragging.rigidbody2D;

            bool snapToPlayer = _player.inMovingPlat || _player.MyPhysics.Animations.IsPlayingAnyLadderAnimation();
            Vector2 truePosition = _player.GetCustomPosition();
            Vector2 offset = new(_player.MyPhysics.FlipX ? +0.34f : -0.155f, 0f);
            Vector2 targetPosition = truePosition + offset;

            Vector2 difference = targetPosition - rigidbody.position;
            float snapThreshold = 1.5f;

            if (snapToPlayer)
            {
                Dragging.NetTransform.SnapTo(targetPosition - new Vector2(0f, 0.2f));
            }
            else if (difference.magnitude > snapThreshold)
            {
                Dragging.NetTransform.SnapTo(_player.GetCustomPosition());
            }
            else
            {
                bool isRigidbodyOnRight = rigidbody.position.x > _player.GetCustomPosition().x;
                Dragging.MyPhysics.FlipX = isRigidbodyOnRight;
                float followSmoothTime = 0.2f;
                Vector2 desiredVelocity = difference / followSmoothTime;
                rigidbody.velocity = Vector2.Lerp(rigidbody.velocity, desiredVelocity, Time.deltaTime * 5f);
            }
        }
        catch { }
        ;
    }

    void IRoleUpdateAction.LateUpdate()
    {
        if (Dragging != null && IsDragging)
        {
            Vector3 position = Dragging.transform.position;
            position.z = position.y / 1000f;
            Dragging.transform.position = position;
        }
    }

    internal override void OnReceiveRoleSync(RoleNetworked.Data data)
    {
        DragTarget(data.MessageReader.ReadFast<PlayerControl>());
    }
}