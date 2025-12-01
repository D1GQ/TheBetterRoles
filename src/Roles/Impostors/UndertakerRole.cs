using Hazel;
using PowerTools;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Interfaces;
using UnityEngine;

namespace TheBetterRoles.Roles.Impostors;

internal sealed class UndertakerRole : ImpostorRoleTBR, IRoleUpdateAction, IRoleAbilityAction<DeadBody>
{
    internal sealed override int RoleId => 7;
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Undertaker;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Impostor;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Support;
    internal sealed override OptionTab? SettingsTab => TBRTabs.ImpostorRoles;
    internal sealed override bool VentReliantRole => true;
    internal sealed override bool CanMoveInVents => !IsDragging;

    internal OptionItem? DragSlowdown;
    internal OptionItem? CanHideBodyInVent;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                DragSlowdown = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Undertaker.Option.DragSlowdown", (0.1f, 1f, 0.1f), 0.5f, ("", "x"), RoleOptions.RoleOptionItem),
                CanHideBodyInVent = OptionCheckboxItem.Create(GetOptionUID(), SettingsTab, "Role.Undertaker.Option.CanHideBodyInVent", false, RoleOptions.RoleOptionItem),
            ];
        }
    }

    private bool IsDragging = false;
    private DeadBody? Dragging;
    private Rigidbody2D? rigidbody;
    private bool hasSpeed = false;
    private CircleCollider2D? boxCollider;
    internal DeadBodyAbilityButton? DragButton = new();
    internal BaseAbilityButton? DropButton = new();
    internal sealed override void OnSetUpRole()
    {
        if (_player.IsLocalPlayer())
        {
            RoleButtons.KillButton?.AddTargetCondition((PlayerControl target) => { return !IsDragging; });
            RoleButtons.VentButton?.AddVentCondition((Vent vent) => { return !IsDragging || CanHideBodyInVent.GetBool(); });

            DragButton = RoleButtons.AddButton(new DeadBodyAbilityButton().Create(5, Translator.GetString("Role.Undertaker.Ability.1"), 0, 0, 0, null, this, true, 0f));
            DragButton.VisibleCondition = () => Dragging == null;
            DragButton.DeadBodyCondition = (DeadBody body) => body.GetComponentInChildren<SpriteAnim>().FrameTime >= 32 && body.GetComponent<Rigidbody2D>() == null;

            DropButton = RoleButtons.AddButton(new BaseAbilityButton().Create(6, Translator.GetString("Role.Undertaker.Ability.2"), 0, 0, 0, null, this, true));
            DropButton.VisibleCondition = () => Dragging != null;
        }
    }

    void IRoleAbilityAction<DeadBody>.OnAbility(int id, DeadBody target)
    {
        switch (id)
        {
            case 5:
                {
                    DragBody(target);
                    SendRoleSync(0, target);
                }
                break;
            case 6:
                {
                    ((IRoleAbilityAction)this).OnResetAbilityState(false);
                    SendRoleSync(1);
                }
                break;
        }
    }

    private void DragBody(DeadBody deadBody)
    {
        IsDragging = true;
        Dragging = deadBody;
        boxCollider = deadBody.gameObject.AddComponent<CircleCollider2D>();
        boxCollider.radius = 0.3f;
        boxCollider.offset = new Vector2(-0.18f, -0.13f);
        rigidbody = deadBody.gameObject.AddComponent<Rigidbody2D>();
        rigidbody.gravityScale = 0f;
        rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
        rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
        SetSpeed();
    }

    private void SetSpeed()
    {
        if (!hasSpeed)
        {
            hasSpeed = true;
            _player.MyPhysics.Speed = _PlayerSpeed * DragSlowdown.GetFloat();
        }
    }

    private void ResetSpeed()
    {
        if (hasSpeed)
        {
            hasSpeed = false;
            _player.MyPhysics.Speed = _PlayerSpeed / DragSlowdown.GetFloat();
        }
    }

    void IRoleAbilityAction.OnResetAbilityState(bool isTimeOut)
    {
        if (rigidbody != null)
        {
            rigidbody.velocity = Vector2.zero;
            UnityEngine.Object.Destroy(rigidbody);
        }
        if (boxCollider != null) UnityEngine.Object.Destroy(boxCollider);
        IsDragging = false;
        Dragging = null;
        ResetSpeed();
    }

    void IRoleUpdateAction.Update()
    {
        if (Dragging == null && IsDragging)
        {
            ResetSpeed();
            return;
        }
        if (Dragging == null || rigidbody == null || boxCollider == null || _player == null || _player.MyPhysics == null || _player.MyPhysics.Animations == null)
        {
            if (IsDragging)
            {
                ((IRoleAbilityAction)this).OnResetAbilityState(false);
                SendRoleSync(2);
            }
            return;
        }

        bool snapToPlayer = _player.inMovingPlat || _player.MyPhysics.Animations.IsPlayingAnyLadderAnimation();
        boxCollider.enabled = !snapToPlayer;

        Vent? vent = Main.AllEnabledVents.FirstOrDefault(v => v.Id == _player.GetPlayerVentId());
        if (_player.inVent && !_player.MyPhysics.Animations.IsPlayingEnterVentAnimation() && rigidbody.velocity.magnitude < 0.1f
            && !snapToPlayer && Vector2.Distance(vent.transform.position, rigidbody.position) < 0.5f)
        {
            if (_player.IsLocalPlayer())
            {
                HideBody();
                SendRoleSync(3);
            }
            return;
        }

        Vector2 truePosition = _player.GetTruePosition();
        Vector2 offset = new(
            _player.MyPhysics.FlipX ? +0.34f : -0.155f,
            !snapToPlayer ? +0.18f : 0.05f
        );
        Vector2 targetPosition = truePosition + (_player.IsInVent() || snapToPlayer ? Vector2.zero : offset);

        Vector2 difference = targetPosition - rigidbody.position;
        float snapThreshold = 1.5f;

        if (difference.magnitude > snapThreshold)
        {
            if (_player.IsLocalPlayer())
            {
                bool isRigidbodyOnRight = rigidbody.position.x > _player.GetTruePosition().x;
                ((IRoleAbilityAction)this).OnResetAbilityState(false);
                SendRoleSync(2, isRigidbodyOnRight);
            }
            else
            {
                rigidbody.position = targetPosition;
                rigidbody.velocity = Vector2.zero;
            }
        }
        else if (!snapToPlayer)
        {
            bool isRigidbodyOnRight = rigidbody.position.x > _player.GetTruePosition().x;
            Dragging.bodyRenderers.ToList().ForEach(body => body.flipX = isRigidbodyOnRight);
            float followSmoothTime = 0.2f;
            Vector2 desiredVelocity = difference / followSmoothTime;
            rigidbody.velocity = Vector2.Lerp(rigidbody.velocity, desiredVelocity, Time.deltaTime * 10f);
        }
        else
        {
            rigidbody.position = targetPosition;
            rigidbody.velocity = Vector2.zero;
        }
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

    private void HideBody()
    {
        Dragging.transform.position = new Vector2(1000f, 1000f);
        rigidbody.velocity = Vector2.zero;
        UnityEngine.Object.Destroy(rigidbody);
        UnityEngine.Object.Destroy(boxCollider);
        ResetSpeed();
        Dragging = null;
        IsDragging = false;

        Vent? vent = Main.AllEnabledVents.FirstOrDefault(v => v.Id == _player.GetPlayerVentId());
        vent?.myAnim?.Play(vent.EnterVentAnim, 1);

        if (_player.IsLocalPlayer())
        {
            vent?.SetButtons(RoleListener.CheckAllRoles(role => role.CanMoveInVents, player: _player));
        }
    }

    internal sealed override void OnReceiveRoleSync(int syncId, MessageReader reader, PlayerControl sender)
    {
        switch (syncId)
        {
            case 0:
                {
                    DragBody(reader.ReadFast<DeadBody>());
                }
                break;
            case 1:
                {
                    ((IRoleAbilityAction)this).OnResetAbilityState(false);
                }
                break;
            case 2:
                {
                    var isRigidbodyOnRight = reader.ReadBoolean();
                    Vector2 pos = reader.ReadVector2();
                    Dragging.bodyRenderers.ToList().ForEach(body => body.flipX = isRigidbodyOnRight);
                    Dragging.transform.position = pos;
                    ((IRoleAbilityAction)this).OnResetAbilityState(false);
                }
                break;
            case 3:
                {
                    HideBody();
                }
                break;
        }
    }
}
