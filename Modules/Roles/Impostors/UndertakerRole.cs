
using BepInEx.Unity.IL2CPP.Utils;
using Hazel;
using System.Collections;
using System.Drawing;
using TheBetterRoles.Patches;
using UnityEngine;
using static Il2CppSystem.Uri;

namespace TheBetterRoles;

public class UndertakerRole : CustomRoleBehavior
{
    // Role Info
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Undertaker;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Impostor;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Support;
    public override bool CanKill => true;
    public override bool CanSabotage => true;
    public override BetterOptionTab? SettingsTab => BetterTabs.ImpostorRoles;

    public BetterOptionItem? DragSlowdown;

    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
                DragSlowdown = new BetterOptionFloatItem().Create(GenerateOptionId(true), SettingsTab, Translator.GetString("Role.Undertaker.Option.DragSlowdown"), [0.1f, 1f, 0.1f], 0.5f, "", "x", RoleOptionItem),
            ];
        }
    }

    private DeadBody? Dragging;
    private Rigidbody2D? rigidbody;
    private bool hasSpeed = false;
    private CircleCollider2D? boxCollider;
    public DeadBodyButton? DragButton = new();
    public AbilityButton? DropButton = new();
    public override void OnSetUpRole()
    {
        DragButton = AddButton(new DeadBodyButton().Create(5, Translator.GetString("Role.Undertaker.Ability.1"), 0, 0, 0, null, this, true, 1f));
        DragButton.VisibleCondition = () => Dragging == null;

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
                        Dragging = body;
                        boxCollider = body.gameObject.AddComponent<CircleCollider2D>();
                        boxCollider.radius = 0.3f;
                        boxCollider.offset = new Vector2(-0.18f, -0.13f);
                        Dragging = body;
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
        if (Dragging != null)
        {
            Dragging = null;
            rigidbody.velocity = Vector2.zero;
            UnityEngine.Object.Destroy(rigidbody);
            UnityEngine.Object.Destroy(boxCollider);
            ResetSpeed();
        }
    }

    public override void Update()
    {
        if (Dragging == null || rigidbody == null)
        {
            return;
        }

        if (_player.inVent && !_player.MyPhysics.Animations.IsPlayingEnterVentAnimation())
        {
            Dragging.transform.position = new Vector2(1000f, 1000f);
            rigidbody.velocity = Vector2.zero;
            UnityEngine.Object.Destroy(rigidbody);
            UnityEngine.Object.Destroy(boxCollider);
            ResetSpeed();
            Dragging = null;
            return;
        }

        Vector2 truePosition = _player.GetTruePosition();
        Vector2 objectPosition = Dragging.transform.position;

        float offsetX = _player.MyPhysics.FlipX ? +0.4f : -0.15f;
        Vector2 offset = new(offsetX, +0.18f);

        Vector2 targetPosition = truePosition + offset;
        Vector2 difference = targetPosition - objectPosition;

        float followSpeed = 3f * _player.MyPhysics.SpeedMod;
        float smoothFactor = 0.2f;
        float snapThreshold = 1.25f + (_player.MyPhysics.SpeedMod * 0.5f);

        if (difference.magnitude > snapThreshold)
        {
            rigidbody.position = targetPosition;
            rigidbody.velocity = Vector2.zero;
        }
        else
        {
            rigidbody.velocity = Vector2.Lerp(rigidbody.velocity, difference * followSpeed, smoothFactor);
        }
    }
}
