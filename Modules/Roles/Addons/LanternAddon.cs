using Hazel;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles.Roles;

public class LanternAddon : CustomAddonBehavior
{
    // Role Info
    public override int RoleId => 26;
    public override string RoleColor => "#c7c71d";
    public override CustomRoleBehavior Role => this;
    public override CustomRoleType RoleType => CustomRoleType.Lantern;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.None;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.HarmfulAddon;
    public override TBROptionTab? SettingsTab => BetterTabs.Addons;
    public TBROptionItem? PlaceLanternOffCooldown;
    public override TBROptionItem[]? OptionItems
    {
        get
        {
            return
            [
                PlaceLanternOffCooldown = new TBROptionCheckboxItem().Create(GetOptionUID(true), SettingsTab, Translator.GetString("Role.Lantern.Option.PlaceLanternOffCooldown"), true, RoleOptionItem),
            ];
        }
    }

    public BaseAbilityButton? LanternButton;
    private GameObject? Lantern;
    public override void OnSetUpRole()
    {
        LanternButton = AddButton(new BaseAbilityButton().Create(5, Translator.GetString("Role.Lantern.Ability.1"), 3f, 0, 0, null, this, false));
        CreateLantern();
        LanternButton.SetCooldown();
    }

    public override void OnIntroCutsceneEnd()
    {
        CreateLantern();
    }

    public override void OnDeinitialize()
    {
        _player.lightSource.enabled = true;
        if (Lantern != null)
        {
            Lantern.DestroyObj();
        }
    }

    public override void FixedUpdate()
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

    public override void OnAbility(int id, MessageReader? reader, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body)
    {
        switch (id)
        {
            case 5:
                CreateLantern();
                break;
        }
    }

    public override void OnExileEnd(PlayerControl? exiled, NetworkedPlayerInfo? exiledData)
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
