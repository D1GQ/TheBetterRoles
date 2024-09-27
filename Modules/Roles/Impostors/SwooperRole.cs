
using TheBetterRoles.Patches;
using TMPro;
using UnityEngine;

namespace TheBetterRoles;

public class SwooperRole : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 500;
    public override string RoleColor => Utils.GetCustomRoleTeamColor(RoleTeam);
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Swooper;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Impostor;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Killing;
    public override bool CanKill => true;
    public override bool CanSabotage => true;
    public override bool CanVent => AllowVenting.GetBool();
    public override BetterOptionTab? SettingsTab => BetterTabs.ImpostorRoles;

    public BetterOptionItem? InvisibilityCooldown;
    public BetterOptionItem? InvisibilityDuration;
    public BetterOptionItem? AllowVenting;

    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
                InvisibilityCooldown = new BetterOptionFloatItem().Create(RoleId + 10, SettingsTab, Translator.GetString("Role.Swooper.Option.InvisibilityCooldow"), [0f, 180f, 2.5f], 25f, "", "s", RoleOptionItem),
                InvisibilityDuration = new BetterOptionFloatItem().Create(RoleId + 15, SettingsTab, Translator.GetString("Role.Swooper.Option.InvisibilityDuration"), [0f, 180f, 2.5f], 15f, "", "s", RoleOptionItem),
                AllowVenting = new BetterOptionCheckboxItem().Create(RoleId + 20, SettingsTab, Translator.GetString("Role.Ability.CanVent"), false, RoleOptionItem)
            ];
        }
    }

    bool IsVisible { get; set; } = true;
    public AbilityButton? InvisibilityButton;
    public override void SetUpRole()
    {
        base.SetUpRole();
        OptionItems.Initialize();

        KillButton.TargetCondition = (PlayerControl target) =>
        {
            return !target.IsImpostorTeammate();
        };

        InvisibilityButton = AddButton(new AbilityButton().Create(5, Translator.GetString("Role.Swooper.Ability.1"), InvisibilityCooldown.GetFloat(), InvisibilityDuration.GetFloat(), 0, Utils.LoadSprite("TheBetterRoles.Resources.Images.Ability.Swoop.png", 135), Role, true)) as AbilityButton;
        InvisibilityButton.CanCancelDuration = true;
    }
    public override void OnAbilityUse(int id, PlayerControl? target, Vent? vent)
    {
        switch (id)
        {
            case 5:
                IsVisible = false;
                InvisibilityButton.SetDuration();
                break;
        }

        base.OnAbilityUse(id, target, vent);
    }

    public override void OnAbilityDurationEnd(int id)
    {
        switch (id)
        {
            case 5:
                IsVisible = true;
                break;
        }
    }

    public override void Update()
    {
        float alpha = 1f;

        if (!IsVisible)
        {
            if (_player.IsLocalPlayer())
            {
                alpha = 0.5f;
            }
            else
            {
                alpha = 0f;
            }
        }

        if (!_player.IsLocalPlayer())
        {
            _player.cosmetics.hat.gameObject.SetActive(IsVisible);
            _player.cosmetics.visor.gameObject.SetActive(IsVisible);
        }

        _player.cosmetics.SetPhantomRoleAlpha(alpha);

        foreach (var text in _player.cosmetics.nameText.gameObject.transform.parent.GetComponentsInChildren<TextMeshPro>())
        {
            text.color = new Color(1f, 1f, 1f, alpha);
        }
    }
}
