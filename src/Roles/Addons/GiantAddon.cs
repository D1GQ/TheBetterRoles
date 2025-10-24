using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Interfaces;
using UnityEngine;

namespace TheBetterRoles.Roles.Addons;

internal sealed class GiantAddon : AddonClass, IRoleDisguiseAction, IRoleMurderAction
{
    internal sealed override int RoleId => 25;
    internal sealed override string RoleColorHex => "#745354";
    internal sealed override RoleClass Role => this;
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Giant;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.None;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.HarmfulAddon;
    internal sealed override OptionTab? SettingsTab => TBRTabs.Addons;
    internal sealed override bool CanMoveInVents => !IsBig;

    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
            ];
        }
    }

    internal CircleCollider2D? CircleCollider;
    internal Vector3 Size;
    internal Vector2 Offset;
    internal float Radius;
    private bool IsBig = false;

    internal sealed override void OnSetUpRole()
    {
        SetSize();
    }

    internal sealed override void OnDeinitialize()
    {
        ResetSize();
    }

    void IRoleDisguiseAction.Disguise(PlayerControl player)
    {
        ResetSize();
    }

    void IRoleDisguiseAction.Undisguise(PlayerControl player)
    {
        SetSize();
    }

    void IRoleMurderAction.DeadBodyDrop(PlayerControl killer, DeadBody myBody)
    {
        if (IsBig)
        {
            myBody.transform.localScale = myBody.transform.localScale * 1.25f;
        }
    }

    private void SetSize()
    {
        if (IsBig) return;
        IsBig = true;
        Size = _player.transform.localScale;
        _player.transform.localScale = new UnityEngine.Vector3(1f, 1f, 1f);
        _player.MyPhysics.Speed = _PlayerSpeed * 0.6f;
    }

    private void ResetSize()
    {
        if (!IsBig) return;
        IsBig = false;
        _player.transform.localScale = Size;
        _player.MyPhysics.Speed = _PlayerSpeed / 0.6f;
    }
}
