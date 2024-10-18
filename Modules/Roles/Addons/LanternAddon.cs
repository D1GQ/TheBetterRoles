
using Hazel;
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles;

public class LanternAddon : CustomAddonBehavior
{
    // Role Info
    public override string RoleColor => "#c7c71d";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Lantern;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.None;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.HarmfulAddon;
    public override BetterOptionTab? SettingsTab => BetterTabs.Addons;
    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
            ];
        }
    }

    public AbilityButton? LanternButton;
    private GameObject? Lantern;
    public override void OnSetUpRole()
    {
        LanternButton = AddButton(new AbilityButton().Create(5, Translator.GetString("Role.Lantern.Ability.1"), 3f, 0, 0, null, this, false));
        CreateLantern();
        LanternButton.SetCooldown();
    }

    public override void OnDeinitialize()
    {
        _player.lightSource.enabled = true;
        if (Lantern != null)
        {
            UnityEngine.Object.Destroy(Lantern);
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
                UnityEngine.Object.Destroy(Lantern);
            }
            Lantern = new GameObject("Lantern");
            Lantern.transform.position = _player.transform.position + new Vector3(0f, 0f, 0.005f) - new Vector3(0f, 0.05f, 0f);
            var spriteRenderer = Lantern.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = LoadAbilitySprite("Lantern", 325);
        }
    }
}
