using HarmonyLib;
using Hazel;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Modules;
using TheBetterRoles.Monos;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Interfaces;
using TMPro;

namespace TheBetterRoles.Roles.Impostors;

internal sealed class SwooperRole : ImpostorRoleTBR, IRoleAbilityAction, IRoleMurderAction, IRoleDeathAction
{
    internal sealed override int RoleId => 6;
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Swooper;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Impostor;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Killing;
    internal sealed override OptionTab? SettingsTab => TBRTabs.ImpostorRoles;
    internal sealed override bool DefaultVentOption => false;

    internal OptionItem? InvisibilityCooldown;
    internal OptionItem? InvisibilityDuration;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                InvisibilityCooldown = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Swooper.Option.InvisibilityCooldown", (0f, 180f, 2.5f), 25f, ("", "s"), RoleOptions.RoleOptionItem),
                InvisibilityDuration = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Swooper.Option.InvisibilityDuration", (0f, 180f, 2.5f), 10f, ("", "s"), RoleOptions.RoleOptionItem, canBeInfinite: true),
            ];
        }
    }

    private bool isVisible = true;
    internal BaseAbilityButton? InvisibilityButton = new();
    internal sealed override void OnSetUpRole()
    {

        if (_player.IsLocalPlayer())
        {
            InvisibilityButton = RoleButtons.AddButton(new BaseAbilityButton().Create(5, Translator.GetString("Role.Swooper.Ability.1"), InvisibilityCooldown.GetFloat(), InvisibilityDuration.GetFloat(), 0, LoadAbilitySprite("Swoop", 135), this, true));
            InvisibilityButton.CanCancelDuration = true;
        }
    }

    void IRoleAbilityAction.OnAbility(int id)
    {
        switch (id)
        {
            case 5:
                isVisible = false;
                SetInvisibility(true);
                SendRoleSync();
                InvisibilityButton?.SetDuration();
                break;
        }
    }

    void IRoleAbilityAction.AbilityDurationEnd(int id, bool isTimeOut)
    {
        switch (id)
        {
            case 5:
                ((IRoleAbilityAction)this).OnResetAbilityState(isTimeOut);
                break;
        }
    }

    void IRoleDeathAction.OnDeath(PlayerControl player, DeathReasons reason)
    {
        InvisibilityButton?.SetCooldown();
        ((IRoleAbilityAction)this).OnResetAbilityState(false);
    }

    void IRoleMurderAction.Murder(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility)
    {
        if (target.IsLocalPlayer())
        {
            _ = new LateTask(() =>
            {
                SetInvisibility(!isVisible);
            }, 1f, shouldLog: false);
        }
    }

    void IRoleAbilityAction.OnResetAbilityState(bool IsTimeOut)
    {
        isVisible = true;
        SetInvisibility(false);
    }

    private void SetInvisibility(bool isActive)
    {
        _player.ExtendedPC().InteractableTarget = !isActive;
        if (_player.IsImpostorTeammate() || !localPlayer.IsAlive())
        {
            SetTrueVisibility(true);
            _player.invisibilityAlpha = isActive ? 0.5f : 1f;
            SetNameTextAlpha(isActive ? 0.5f : 1f);
            _player.cosmetics.SetPhantomRoleAlpha(_player.invisibilityAlpha);
        }
        else
        {
            SetTrueVisibility(!isActive);
        }
    }

    private void SetTrueVisibility(bool @bool)
    {
        _player.shouldAppearInvisible = !@bool;
        _player.Visible = @bool && !_player.inVent;
    }

    private void SetNameTextAlpha(float alpha)
    {
        foreach (var text in _player.cosmetics.nameText.gameObject.transform.parent.GetComponentsInChildren<TextMeshPro>(true))
        {
            text.color = text.color.ToAlpha(alpha);
        }
        _player.cosmetics.colorBlindText.color = _player.cosmetics.colorBlindText.color.ToAlpha(alpha);
    }

    internal override void OnReceiveRoleSync(int syncId, MessageReader reader, PlayerControl sender)
    {
        isVisible = false;
        SetInvisibility(true);
    }

    [HarmonyPatch(typeof(PlayerControl))]
    class PlayerControlSwooperPatch
    {
        [HarmonyPatch(nameof(PlayerControl.SetHatAndVisorAlpha))]
        [HarmonyPrefix]
        internal static bool SetHatAndVisorAlpha_Prefix(PlayerControl __instance)
        {
            if (__instance.Is(RoleClassTypes.Swooper) && __instance.IsAlive()) return false;

            return true;
        }
    }
}