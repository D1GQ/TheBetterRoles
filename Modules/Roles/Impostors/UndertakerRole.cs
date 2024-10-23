using Hazel;
using PowerTools;
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles;

public class UndertakerRole : CustomRoleBehavior
{
    // Role Info
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Undertaker;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Impostor;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Support;
    public override BetterOptionTab? SettingsTab => BetterTabs.ImpostorRoles;
    public override bool VentReliantRole => true;
    public override bool CanMoveInVents => !IsDragging;

    public BetterOptionItem? DragSlowdown;

    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
                DragSlowdown = new BetterOptionFloatItem().Create(GetOptionUID(true), SettingsTab, Translator.GetString("Role.Undertaker.Option.DragSlowdown"), [0.1f, 1f, 0.1f], 0.5f, "", "x", RoleOptionItem),
            ];
        }
    }

    private bool IsDragging = false;
    private DeadBody? Dragging;
    private Rigidbody2D? rigidbody;
    private bool hasSpeed = false;
    private CircleCollider2D? boxCollider;
    public DeadBodyButton? DragButton = new();
    public AbilityButton? DropButton = new();
    public override void OnSetUpRole()
    {
        DragButton = AddButton(new DeadBodyButton().Create(5, Translator.GetString("Role.Undertaker.Ability.1"), 0, 0, 0, null, this, true, 0f));
        DragButton.VisibleCondition = () => Dragging == null;
        DragButton.DeadBodyCondition = (DeadBody body) => body.GetComponentInChildren<SpriteAnim>().FrameTime >= 32;

        DropButton = AddButton(new AbilityButton().Create(6, Translator.GetString("Role.Undertaker.Ability.2"), 0, 0, 0, null, this, true));
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
        IsDragging = false;
        Dragging = null;
        if (rigidbody != null)
        {
            rigidbody.velocity = Vector2.zero;
            UnityEngine.Object.Destroy(rigidbody);
        }
        UnityEngine.Object.Destroy(boxCollider);
        ResetSpeed();
    }

    public override void Update()
    {
        if (Dragging == null && IsDragging)
        {
            ResetSpeed();
            return;
        }

        if (rigidbody == null || boxCollider == null)
        {
            return;
        }

        bool SnapToPlayer = _player.inMovingPlat || _player.MyPhysics.Animations.IsPlayingAnyLadderAnimation();
        boxCollider.enabled = !SnapToPlayer;

        // Hide body in vent
        if (_player.inVent && !_player.MyPhysics.Animations.IsPlayingEnterVentAnimation() && rigidbody.velocity.magnitude < 0.1f && !SnapToPlayer)
        {
            if (_player.IsLocalPlayer())
            {
                HideBody();
                SendRoleSync(1);
            }
            return;
        }

        Vector2 truePosition = _player.GetTruePosition();
        Vector2 objectPosition = Dragging.transform.position;

        float offsetX = _player.MyPhysics.FlipX ? +0.4f : -0.15f;
        if (_player.IsInVent() || SnapToPlayer) offsetX = 0f;
        Vector2 offset = new(offsetX, !SnapToPlayer ? +0.18f : 0.05f);

        Vector2 targetPosition = truePosition + offset;
        Vector2 difference = targetPosition - objectPosition;

        float followSpeed = 3f * _player.MyPhysics.SpeedMod;
        float smoothFactor = 1f;
        float snapThreshold = 1.25f + (_player.MyPhysics.SpeedMod * 0.5f);

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
        else if (!SnapToPlayer)
        {
            rigidbody.velocity = Vector2.Lerp(rigidbody.velocity, difference * followSpeed, smoothFactor);
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
}
