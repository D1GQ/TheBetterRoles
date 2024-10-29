
using Hazel;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Roles;

public class SwooperRole : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 6;
    public override string RoleColor => Utils.GetCustomRoleTeamColor(RoleTeam);
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Swooper;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Impostor;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Killing;
    public override bool DefaultVentOption => false;
    public override BetterOptionTab? SettingsTab => BetterTabs.ImpostorRoles;

    public BetterOptionItem? InvisibilityCooldown;
    public BetterOptionItem? InvisibilityDuration;

    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
                InvisibilityCooldown = new BetterOptionFloatItem().Create(GetOptionUID(true), SettingsTab, Translator.GetString("Role.Swooper.Option.InvisibilityCooldow"), [0f, 180f, 2.5f], 25f, "", "s", RoleOptionItem),
                InvisibilityDuration = new BetterOptionFloatItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Swooper.Option.InvisibilityDuration"), [0f, 180f, 2.5f], 15f, "", "s", RoleOptionItem),
            ];
        }
    }

    private bool isVisible = true;
    public BaseAbilityButton? InvisibilityButton = new();
    public override void OnSetUpRole()
    {
        InvisibilityButton = AddButton(new BaseAbilityButton().Create(5, Translator.GetString("Role.Swooper.Ability.1"), InvisibilityCooldown.GetFloat(), InvisibilityDuration.GetFloat(), 0, LoadAbilitySprite("Swoop", 135), this, true));
        InvisibilityButton.CanCancelDuration = true;
    }

    public override void OnAbility(int id, MessageReader? reader, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body)
    {
        switch (id)
        {
            case 5:
                isVisible = false;
                SetInvisibility(true, false);
                InvisibilityButton.SetDuration();
                break;
        }
    }

    public override void OnAbilityDurationEnd(int id, bool isTimeOut)
    {
        switch (id)
        {
            case 5:
                OnResetAbilityState(isTimeOut);
                break;
        }
    }

    public override void FixedUpdate()
    {
        SetInvisibility(!isVisible, true);
    }

    public override void OnResetAbilityState(bool IsTimeOut)
    {
        isVisible = true;
        InteractableTarget = true;
        SetInvisibility(false, false);
    }

    private void SetInvisibility(bool isActive, bool isUpdate)
    {
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

        if (!_player.IsLocalPlayer() && !isUpdate)
        {
            SetTrueVisibility(!isActive);
        }
    }

    private void SetTrueVisibility(bool @bool)
    {
        _player.Visible = @bool && !_player.inVent;
        _player.shouldAppearInvisible = !@bool;
    }

    private void SetNameTextAlpha(float alpha)
    {
        foreach (var text in _player.cosmetics.nameText.gameObject.transform.parent.GetComponentsInChildren<TextMeshPro>())
        {
            text.color = new Color(1f, 1f, 1f, alpha);
        }
        _player.cosmetics.colorBlindText.color = new Color(1f, 1f, 1f, alpha);
    }
}
