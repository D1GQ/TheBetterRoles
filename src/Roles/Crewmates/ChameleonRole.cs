using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Hazel;
using System.Collections;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Monos;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Interfaces;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Roles.Crewmates;

internal sealed class ChameleonRole : CrewmateRoleTBR, IRoleMurderAction, IRoleAbilityAction, IRoleDeathAction
{
    internal sealed override int RoleId => 38;
    internal sealed override string RoleColorHex => "#64AD1C";
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Chameleon;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Crewmate;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Information;
    internal sealed override OptionTab? SettingsTab => TBRTabs.CrewmateRoles;
    internal sealed override bool DefaultVentOption => false;

    internal OptionItem? InvisibilityCooldown;
    internal OptionItem? InvisibilityDuration;
    internal OptionItem? FadeDuration;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                InvisibilityCooldown = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Chameleon.Option.InvisibilityCooldow", (0f, 180f, 2.5f), 15f, ("", "s"), RoleOptions.RoleOptionItem),
                InvisibilityDuration = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Chameleon.Option.InvisibilityDuration", (0f, 180f, 2.5f), 10f, ("", "s"), RoleOptions.RoleOptionItem, canBeInfinite: true),
                FadeDuration = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Chameleon.Option.FadeDuration", (0f, 5f, 0.5f), 0.5f, ("", "s"), RoleOptions.RoleOptionItem),
            ];
        }
    }

    private bool isVisible = true;
    private Coroutine? fadeOutCoroutine;
    private Coroutine? fadeInCoroutine;
    internal BaseAbilityButton? InvisibilityButton = new();
    internal sealed override void OnSetUpRole()
    {
        if (_player.IsLocalPlayer())
        {
            InvisibilityButton = RoleButtons.AddButton(new BaseAbilityButton().Create(5, Translator.GetString("Role.Chameleon.Ability.1"), InvisibilityCooldown.GetFloat(), InvisibilityDuration.GetFloat(), 0, null, this, true));
            InvisibilityButton.DurationName = Translator.GetString("Role.Chameleon.Ability.2");
            InvisibilityButton.CanCancelDuration = true;
        }
    }

    internal sealed override void OnDeinitialize()
    {
        if (fadeInCoroutine != null) CoroutineManager.Instance.StopCoroutine(fadeInCoroutine);
        _player.invisibilityAlpha = 1f;
        _player.cosmetics.SetPhantomRoleAlpha(_player.invisibilityAlpha);
        SetNameTextAlpha(_player.invisibilityAlpha);
        isVisible = true;
        _player.ExtendedPC().InteractableTarget = true;
    }

    void IRoleAbilityAction.OnAbility(int id)
    {
        switch (id)
        {
            case 5:
                FadeOut();
                SendRoleSync(0);
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
        if (fadeOutCoroutine != null) CoroutineManager.Instance.StopCoroutine(fadeOutCoroutine);
        fadeOutCoroutine = null;
        if (fadeInCoroutine != null) CoroutineManager.Instance.StopCoroutine(fadeOutCoroutine);
        fadeInCoroutine = null;
        InvisibilityButton?.SetCooldown();
        _player.invisibilityAlpha = 1f;
        _player.cosmetics.SetPhantomRoleAlpha(_player.invisibilityAlpha);
        SetNameTextAlpha(_player.invisibilityAlpha);
        isVisible = true;
        _player.ExtendedPC().InteractableTarget = true;

    }

    void IRoleMurderAction.MurderOther(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility)
    {
        if (target.IsLocalPlayer() && target != localPlayer && !isVisible)
        {
            _ = new LateTask(() =>
            {
                if (!isVisible)
                {
                    _player.invisibilityAlpha = 0.5f;
                    _player.cosmetics.SetPhantomRoleAlpha(_player.invisibilityAlpha);
                    SetNameTextAlpha(_player.invisibilityAlpha);
                }
            }, 1f, shouldLog: false);
        }
    }

    void IRoleAbilityAction.OnResetAbilityState(bool IsTimeOut)
    {
        FadeIn();
        SendRoleSync(1);
    }

    private void FadeOut()
    {
        if (fadeInCoroutine != null) CoroutineManager.Instance.StopCoroutine(fadeOutCoroutine);
        fadeInCoroutine = null;
        fadeOutCoroutine = CoroutineManager.Instance.StartCoroutine(CoFadeOut());
    }

    private IEnumerator CoFadeOut()
    {
        float duration = FadeDuration.GetFloat();
        float targetAlpha = localPlayer.IsAlive() ? 0.045f : 0.5f;
        float elapsed = 0f;

        float alpha;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            alpha = Mathf.Lerp(1f, targetAlpha, elapsed / duration);

            _player.invisibilityAlpha = alpha;
            _player.cosmetics.SetPhantomRoleAlpha(_player.invisibilityAlpha);
            SetNameTextAlpha(_player.invisibilityAlpha);
            yield return null;
        }

        _player.invisibilityAlpha = targetAlpha;
        _player.cosmetics.SetPhantomRoleAlpha(_player.invisibilityAlpha);
        SetNameTextAlpha(localPlayer.IsAlive() ? 0f : 0.5f);

        isVisible = false;
        _player.ExtendedPC().InteractableTarget = false;
        fadeOutCoroutine = null;
    }

    private void FadeIn()
    {
        if (fadeOutCoroutine != null) CoroutineManager.Instance.StopCoroutine(fadeOutCoroutine);
        fadeOutCoroutine = null;
        fadeInCoroutine = CoroutineManager.Instance.StartCoroutine(CoFadeIn());
    }

    private IEnumerator CoFadeIn()
    {
        float totalFadeDuration = FadeDuration.GetFloat();
        float currentAlpha = _player.invisibilityAlpha;
        float targetAlpha = 1f;

        float duration = totalFadeDuration * Mathf.Abs(targetAlpha - currentAlpha);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float alpha = Mathf.Lerp(currentAlpha, targetAlpha, elapsed / duration);

            _player.invisibilityAlpha = alpha;
            _player.cosmetics.SetPhantomRoleAlpha(_player.invisibilityAlpha);
            SetNameTextAlpha(_player.invisibilityAlpha);
            yield return null;
        }

        _player.invisibilityAlpha = targetAlpha;
        _player.cosmetics.SetPhantomRoleAlpha(_player.invisibilityAlpha);
        SetNameTextAlpha(_player.invisibilityAlpha);

        isVisible = true;
        _player.ExtendedPC().InteractableTarget = true;
        fadeInCoroutine = null;
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
        switch (syncId)
        {
            case 0:
                FadeOut();
                break;
            case 1:
                FadeIn();
                break;
        }
    }

    [HarmonyPatch(typeof(PlayerControl))]
    class PlayerControlSwooperPatch
    {
        [HarmonyPatch(nameof(PlayerControl.SetHatAndVisorAlpha))]
        [HarmonyPrefix]
        internal static bool SetHatAndVisorAlpha_Prefix(PlayerControl __instance)
        {
            if (__instance.Is(RoleClassTypes.Chameleon) && __instance.IsAlive()) return false;

            return true;
        }
    }
}