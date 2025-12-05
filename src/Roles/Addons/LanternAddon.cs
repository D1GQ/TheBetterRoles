using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core;
using TheBetterRoles.Roles.Interfaces;
using UnityEngine;

namespace TheBetterRoles.Roles.Addons;

internal sealed class LanternAddon : AddonClass, IRoleUpdateAction, IRoleAbilityAction, IRoleGameplayAction, IRoleMeetingAction
{
    internal sealed override int RoleId => 26;
    internal sealed override string RoleColorHex => "#c7c71d";
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Lantern;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.None;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.HarmfulAddon;
    internal sealed override OptionTab? SettingsTab => TBRTabs.Addons;

    internal OptionItem? PlaceLanternOffCooldown;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                PlaceLanternOffCooldown = OptionCheckboxItem.Create(GetOptionUID(), SettingsTab, "Role.Lantern.Option.PlaceLanternOffCooldown", true, RoleOptions.RoleOptionItem),
            ];
        }
    }

    private GameObject? Lantern;
    internal BaseAbilityButton? LanternButton;
    internal sealed override void OnSetUpRole()
    {
        if (_player.IsLocalPlayer())
        {
            LanternButton = RoleButtons.AddButton(BaseAbilityButton.Create(5, Translator.GetString("Role.Lantern.Ability.1"), 3f, 0, 0, null, this, false));
            CreateLantern();
            LanternButton.SetCooldown();
        }
    }

    void IRoleGameplayAction.IntroCutsceneEnd()
    {
        CreateLantern();
    }

    internal sealed override void OnDeinitialize()
    {
        _player.lightSource.enabled = true;
        if (Lantern != null)
        {
            Lantern.DestroyObj();
        }
    }

    void IRoleUpdateAction.FixedUpdate()
    {
        if (_player.IsLocalPlayer() && PlaceLanternOffCooldown.GetBool())
        {
            if (!LanternButton.IsCooldown && (Lantern == null || Vector2.Distance(_player.GetCustomPosition(), Lantern.transform.position) > 1.5f))
            {
                CreateLantern();
                LanternButton.SetCooldown();
            }
        }
    }

    void IRoleAbilityAction.OnAbility(int id)
    {
        switch (id)
        {
            case 5:
                CreateLantern();
                break;
        }
    }

    void IRoleMeetingAction.ExileEnd(PlayerControl? exiled, NetworkedPlayerInfo? exiledData)
    {
        CreateLantern();
    }

    private void CreateLantern()
    {
        if (_player.IsLocalPlayer())
        {
            _player.lightSource.enabled = true;
            _player.lightSource.Update();
            _player.lightSource.enabled = false;
            _player.lightSource.gameObject.transform.position = _player.GetCustomPosition();
            if (Lantern != null)
            {
                Lantern.DestroyObj();
            }
            Lantern = new GameObject("Lantern");
            Lantern.transform.position = _player.transform.position + new Vector3(0f, 0f, 0.005f) - new Vector3(0f, 0.05f, 0f);
            var spriteRenderer = Lantern.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = LoadAbilitySprite("Lantern", 325);
        }
    }
}
