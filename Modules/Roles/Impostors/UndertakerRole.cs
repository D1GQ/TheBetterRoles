using Hazel;
using PowerTools;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles.Roles;

public class UndertakerRole : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 7;
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Undertaker;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Impostor;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Support;
    public override TBROptionTab? SettingsTab => BetterTabs.ImpostorRoles;
    public override bool VentReliantRole => true;
    public override bool CanMoveInVents => !IsDragging;

    public TBROptionItem? DragSlowdown;
    public TBROptionItem? CanHideBodyInVent;

    public override TBROptionItem[]? OptionItems
    {
        get
        {
            return
            [
                DragSlowdown = new TBROptionFloatItem().Create(GetOptionUID(true), SettingsTab, Translator.GetString("Role.Undertaker.Option.DragSlowdown"), [0.1f, 1f, 0.1f], 0.5f, "", "x", RoleOptionItem),
                CanHideBodyInVent = new TBROptionCheckboxItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Undertaker.Option.CanHideBodyInVent"), false, RoleOptionItem),
            ];
        }
    }

    private bool IsDragging = false;
    private DeadBody? Dragging;
    private Rigidbody2D? rigidbody;
    private bool hasSpeed = false;
    private CircleCollider2D? boxCollider;
    public DeadBodyAbilityButton? DragButton = new();
    public BaseAbilityButton? DropButton = new();
    public override void OnSetUpRole()
    {
        KillButton?.AddTargetCondition((PlayerControl target) => { return !IsDragging; });
        VentButton?.AddVentCondition((Vent vent) => { return !IsDragging || CanHideBodyInVent.GetBool(); });

        DragButton = AddButton(new DeadBodyAbilityButton().Create(5, Translator.GetString("Role.Undertaker.Ability.1"), 0, 0, 0, null, this, true, 0f));
        DragButton.VisibleCondition = () => Dragging == null;
        DragButton.DeadBodyCondition = (DeadBody body) => body.GetComponentInChildren<SpriteAnim>().FrameTime >= 32 && body.GetComponent<Rigidbody2D>() == null;

        DropButton = AddButton(new BaseAbilityButton().Create(6, Translator.GetString("Role.Undertaker.Ability.2"), 0, 0, 0, null, this, true));
        DropButton.VisibleCondition = () => Dragging != null;
    }

    public override void OnAbility(int id, MessageReader? reader, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body)
    {
        switch (id)
        {
            case 5:
                {
                    if (body != null)
                    {
                        IsDragging = true;
                        Dragging = body;
                        boxCollider = body.gameObject.AddComponent<CircleCollider2D>();
                        boxCollider.radius = 0.3f;
                        boxCollider.offset = new Vector2(-0.18f, -0.13f);
                        rigidbody = body.gameObject.AddComponent<Rigidbody2D>();
                        rigidbody.gravityScale = 0f;
                        rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
                        rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
                        SetSpeed();
                    }

                }
                break;
            case 6:
                {
                    OnResetAbilityState(false);
                }
                break;
        }
    }

    private void SetSpeed()
    {
        if (!hasSpeed)
        {
            hasSpeed = true;
            _player.MyPhysics.Speed = PlayerSpeed * DragSlowdown.GetFloat();
        }
    }

    private void ResetSpeed()
    {
        if (hasSpeed)
        {
            hasSpeed = false;
            _player.MyPhysics.Speed = PlayerSpeed / DragSlowdown.GetFloat();
        }
    }

    public override void OnResetAbilityState(bool isTimeOut)
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

    public override void Update()
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
                OnResetAbilityState(false);
                SendRoleSync(0);
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
                SendRoleSync(1);
            }
            return;
        }

        Vector2 truePosition = _player.GetTruePosition();
        Vector2 offset = new(
            _player.MyPhysics.FlipX ? +0.4f : -0.15f,
            !snapToPlayer ? +0.18f : 0.05f
        );
        Vector2 targetPosition = truePosition + (_player.IsInVent() || snapToPlayer ? Vector2.zero : offset);

        Vector2 difference = targetPosition - rigidbody.position;
        float snapThreshold = 1.5f;

        if (difference.magnitude > snapThreshold)
        {
            if (_player.IsLocalPlayer())
            {
                OnResetAbilityState(false);
                SendRoleSync(0);
            }
            else
            {
                rigidbody.position = targetPosition;
                rigidbody.velocity = Vector2.zero;
            }
        }
        else if (!snapToPlayer)
        {
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
            vent?.SetButtons(CustomRoleManager.RoleChecks(_player, role => role.CanMoveInVents));
        }
    }

    public override void OnSendRoleSync(int syncId, MessageWriter writer, object[]? additionalParams)
    {
        switch (syncId)
        {
            case 0:
                {
                    NetHelpers.WriteVector2(rigidbody.position, writer);
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
                    Vector2 pos = NetHelpers.ReadVector2(reader);
                    Dragging.transform.position = pos;
                    OnResetAbilityState(false);
                }
                break;
            case 1:
                {
                    HideBody();
                }
                break;
        }
    }
}
