using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles;

public class GiantAddon : CustomAddonBehavior
{
    // Role Info
    public override int RoleId => 25;
    public override string RoleColor => "#745354";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Giant;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.None;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.HarmfulAddon;
    public override BetterOptionTab? SettingsTab => BetterTabs.Addons;

    public CircleCollider2D? CircleCollider;
    public Vector3 Size;
    public Vector2 Offset;
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

    public override bool CanMoveInVents => !IsBig;
    public AbilityButton? MeetingButton = new();
    private bool IsBig = false;
    public override void OnSetUpRole()
    {
        SetSize();
    }

    public override void OnDeinitialize()
    {
        ResetSize();
    }

    public override void OnDisguise(PlayerControl player)
    {
        ResetSize();
    }

    public override void OnUndisguise(PlayerControl player)
    {
        SetSize();
    }

    private void SetSize()
    {
        if (IsBig) return;
        IsBig = true;
        Size = _player.transform.localScale;
        CircleCollider = _player.GetComponent<CircleCollider2D>();
        Offset = CircleCollider.offset;
        Radius = CircleCollider.radius;

        _player.transform.localScale = new UnityEngine.Vector3(1f, 1f, 1f);
        CircleCollider.offset = new UnityEngine.Vector2(0f, -0.2f);
        CircleCollider.radius = 0.35f;
        _player.MyPhysics.Speed = PlayerSpeed * 0.6f;
    }

    private void ResetSize()
    {
        if (!IsBig) return;
        IsBig = false;
        _player.transform.localScale = Size;
        CircleCollider.offset = Offset;
        CircleCollider.radius = Radius;
        _player.MyPhysics.Speed = PlayerSpeed / 0.6f;
    }
}
