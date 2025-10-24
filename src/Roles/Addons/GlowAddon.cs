using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Interfaces;
using UnityEngine;

namespace TheBetterRoles.Roles.Addons;

internal sealed class GlowAddon : AddonClass, IRoleMurderAction
{
    internal sealed override int RoleId => 35;
    internal sealed override string RoleColorHex => "#ffff3e";
    internal sealed override RoleClass Role => this;
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Glow;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.None;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.GeneralAddon;
    internal sealed override OptionTab? SettingsTab => TBRTabs.Addons;

    internal OptionItem? DeadBodyGlow;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                DeadBodyGlow = OptionCheckboxItem.Create(GetOptionUID(), SettingsTab, "Role.Glow.Option.DeadBodyGlow", false, RoleOptions.RoleOptionItem),
            ];
        }
    }

    internal sealed override void OnSetUpRole()
    {
        foreach (var item in _player.cosmetics.gameObject.GetComponentsInChildren<SpriteRenderer>(true))
        {
            item.sortingOrder = 1;
        }
        foreach (var item in _player.MyPhysics.gameObject.GetComponentsInChildren<SpriteRenderer>(true))
        {
            item.sortingOrder = 1;
        }
    }

    void IRoleMurderAction.DeadBodyDrop(PlayerControl killer, DeadBody myBody)
    {
        if (DeadBodyGlow.GetBool())
        {
            foreach (var item in myBody.gameObject.GetComponentsInChildren<SpriteRenderer>(true))
            {
                item.sortingOrder = 1;
            }
        }
    }

    internal sealed override void OnDeinitialize()
    {
        foreach (var item in _player.cosmetics.gameObject.GetComponentsInChildren<SpriteRenderer>(true))
        {
            item.sortingOrder = 0;
        }
        foreach (var item in _player.MyPhysics.gameObject.GetComponentsInChildren<SpriteRenderer>(true))
        {
            item.sortingOrder = 0;
        }
    }
}
