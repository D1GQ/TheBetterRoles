
using Hazel;
using TheBetterRoles.Patches;
using TMPro;
using UnityEngine;

namespace TheBetterRoles;

public class SwooperRole : CustomRoleBehavior
{
    // Role Info
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

    private bool IsVisible { get; set; } = true;
    public AbilityButton? InvisibilityButton = new();
    public override void OnSetUpRole()
    {
        InvisibilityButton = AddButton(new AbilityButton().Create(5, Translator.GetString("Role.Swooper.Ability.1"), InvisibilityCooldown.GetFloat(), InvisibilityDuration.GetFloat(), 0, LoadAbilitySprite("Swoop", 135), this, true)) as AbilityButton;
        InvisibilityButton.CanCancelDuration = true;
    }

    public override void OnAbility(int id, MessageReader? reader, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body)
    {
        switch (id)
        {
            case 5:
                IsVisible = false;
                InvisibilityButton.SetDuration();
                break;
        }
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
        InteractableTarget = IsVisible;
        SetInvisibility(!IsVisible);
    }

    public override void OnResetAbilityState()
    {
        InteractableTarget = true;
        IsVisible = true;
        SetInvisibility(!IsVisible);
    }

    private void SetInvisibility(bool isActive)
    {
        if (!isActive)
        {
            _player.shouldAppearInvisible = false;
            _player.Visible = true;
        }
        if (_player.IsLocalPlayer() || localPlayer.IsImpostorTeammate() || !localPlayer.IsAlive())
        {
            _player.invisibilityAlpha = isActive ? 0.5f : 1f;
            SetNameTextAlpha(isActive ? 0.5f : 1f);
        }
        else
        {
            _player.invisibilityAlpha = isActive ? 0 : 1;
            SetNameTextAlpha(isActive ? 0f : 1f);
        }

        _player.cosmetics.SetPhantomRoleAlpha(_player.invisibilityAlpha);
        if (isActive && (PlayerControl.LocalPlayer.Data.IsDead || PlayerControl.LocalPlayer.Data.Role.IsImpostor))
        {
            return;
        }
        if (!_player.IsLocalPlayer())
        {
            _player.shouldAppearInvisible = isActive;
            if (_player.inVent)
            {
                _player.Visible = false;
                return;
            }
            _player.Visible = !isActive;
        }
    }

    private void SetNameTextAlpha(float alpha)
    {
        foreach (var text in _player.cosmetics.nameText.gameObject.transform.parent.GetComponentsInChildren<TextMeshPro>())
        {
            text.color = new Color(1f, 1f, 1f, alpha);
        }
    }
}
