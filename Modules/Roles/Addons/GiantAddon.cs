using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles.Roles;

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

    public override void OnMurder(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility)
    {
        if (target == _player && IsBig)
        {
            _ = new LateTask(SetDeadBodySize, 0.005f, shouldLog: false);
        }
    }

    private void SetDeadBodySize()
    {
        var body = Main.AllDeadBodys.FirstOrDefault(b => b.ParentId == _player.PlayerId);
        if (body != null)
        {
            body.transform.localScale = body.transform.localScale * 1.25f;
        }
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
