using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles.Roles;

public class GlowAddon : CustomAddonBehavior
{
    // Role Info
    public override int RoleId => 35;
    public override string RoleColor => "#ffff3e";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Glow;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.None;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.GeneralAddon;
    public override TBROptionTab? SettingsTab => BetterTabs.Addons;

    public TBROptionItem? DeadBodyGlow;
    public override TBROptionItem[]? OptionItems
    {
        get
        {
            return
            [
                DeadBodyGlow = new TBROptionCheckboxItem().Create(GetOptionUID(true), SettingsTab, Translator.GetString("Role.Glow.Option.DeadBodyGlow"), false, RoleOptionItem),
            ];
        }
    }

    public override void OnSetUpRole()
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

    public override void OnDeadBodyDrop(PlayerControl killer, DeadBody myBody)
    {
        if (DeadBodyGlow.GetBool())
        {
            foreach (var item in myBody.gameObject.GetComponentsInChildren<SpriteRenderer>(true))
            {
                item.sortingOrder = 1;
            }
        }
    }

    public override void OnDeinitialize()
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
