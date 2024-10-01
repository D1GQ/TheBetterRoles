
using System.Numerics;
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles;

public class GiantAddon : CustomAddonBehavior
{
    // Role Info
    public override string RoleColor => "#745354";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Giant;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.None;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.HarmfulAddon;
    public override BetterOptionTab? SettingsTab => BetterTabs.Addons;

    public CircleCollider2D? CircleCollider;
    public UnityEngine.Vector3 Size;
    public UnityEngine.Vector2 Offset;
    public float Radius;
    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
            ];
        }
    }

    public override bool CanMoveInVent => false;
    public AbilityButton? MeetingButton = new();
    public override void SetUpRole()
    {
        OptionItems.Initialize();

        Size = _player.transform.localScale;
        CircleCollider = _player.GetComponent<CircleCollider2D>();
        Offset = CircleCollider.offset;
        Radius = CircleCollider.radius;

        _player.transform.localScale = new UnityEngine.Vector3(1f, 1f, 1f);
        CircleCollider.offset = new UnityEngine.Vector2(0f, -0.2f);
        CircleCollider.radius = 0.35f;
        _player.MyPhysics.Speed = PlayerSpeed * 0.6f;
    }

    public override void OnDeinitialize()
    {
        _player.transform.localScale = Size;
        CircleCollider.offset = Offset;
        CircleCollider.radius = Radius;
        _player.MyPhysics.Speed = PlayerSpeed / 0.6f;
    }
}
