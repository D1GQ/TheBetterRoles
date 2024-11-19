
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Hazel;
using System.Collections;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Roles;

public class ChameleonRole : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 38;
    public override string RoleColor => "#64AD1C";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Chameleon;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Crewmate;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Information;
    public override TBROptionTab? SettingsTab => BetterTabs.CrewmateRoles;
    public override bool DefaultVentOption => false;

    public TBROptionItem? InvisibilityCooldown;
    public TBROptionItem? InvisibilityDuration;
    public TBROptionItem? FadeDuration;

    public override TBROptionItem[]? OptionItems
    {
        get
        {
            return
            [
                InvisibilityCooldown = new TBROptionFloatItem().Create(GetOptionUID(true), SettingsTab, Translator.GetString("Role.Chameleon.Option.InvisibilityCooldow"), [0f, 180f, 2.5f], 15f, "", "s", RoleOptionItem),
                InvisibilityDuration = new TBROptionFloatItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Chameleon.Option.InvisibilityDuration"), [0f, 180f, 2.5f], 10f, "", "s", RoleOptionItem, canBeInfinite: true),
                FadeDuration = new TBROptionFloatItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Chameleon.Option.FadeDuration"), [0f, 5f, 0.5f], 0.5f, "", "s", RoleOptionItem),
            ];
        }
    }

    private bool isVisible = true;
    public BaseAbilityButton? InvisibilityButton = new();
    public override void OnSetUpRole()
    {
        InvisibilityButton = AddButton(new BaseAbilityButton().Create(5, Translator.GetString("Role.Chameleon.Ability.1"), InvisibilityCooldown.GetFloat(), InvisibilityDuration.GetFloat(), 0, null, this, true));
        InvisibilityButton.DurationName = Translator.GetString("Role.Chameleon.Ability.2");
        InvisibilityButton.CanCancelDuration = true;
    }

    public override void OnDeinitialize()
    {
        if (fadeInCoroutine != null) CoroutineManager.Instance.StopCoroutine(fadeInCoroutine);
        _player.invisibilityAlpha = 1f;
        _player.cosmetics.SetPhantomRoleAlpha(_player.invisibilityAlpha);
        SetNameTextAlpha(_player.invisibilityAlpha);
        isVisible = true;
        InteractableTarget = true;
    }

    public override void OnAbility(int id, MessageReader? reader, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body)
    {
        switch (id)
        {
            case 5:
                InvisibilityButton?.SetDuration();
                fadeOutCoroutine = CoroutineManager.Instance.StartCoroutine(CoFadeOut());
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

    public override void OnDeath(PlayerControl player, DeathReasons reason)
    {
        InvisibilityButton?.SetCooldown();
        _player.invisibilityAlpha = 1f;
        _player.cosmetics.SetPhantomRoleAlpha(_player.invisibilityAlpha);
        SetNameTextAlpha(_player.invisibilityAlpha);
        isVisible = true;
        InteractableTarget = true;

    }

    public override void OnMurderOther(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility)
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

    public override void OnResetAbilityState(bool IsTimeOut)
    {
        if (fadeOutCoroutine != null) CoroutineManager.Instance.StopCoroutine(fadeOutCoroutine);
        fadeOutCoroutine = null;
        fadeInCoroutine = CoroutineManager.Instance.StartCoroutine(CoFadeIn());
    }

    private Coroutine? fadeOutCoroutine;
    private Coroutine? fadeInCoroutine;

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
        InteractableTarget = false;
        fadeOutCoroutine = null;
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
        InteractableTarget = true;
        fadeInCoroutine = null;
    }

    private void SetNameTextAlpha(float alpha)
    {
        foreach (var text in _player.cosmetics.nameText.gameObject.transform.parent.GetComponentsInChildren<TextMeshPro>(true))
        {
            text.color = new Color(1f, 1f, 1f, alpha);
        }
        _player.cosmetics.colorBlindText.color = new Color(1f, 1f, 1f, alpha);
    }

    [HarmonyPatch(typeof(PlayerControl))]
    class PlayerControlSwooperPatch
    {
        [HarmonyPatch(nameof(PlayerControl.SetHatAndVisorAlpha))]
        [HarmonyPrefix]
        public static bool SetHatAndVisorAlpha_Prefix(PlayerControl __instance)
        {
            if (__instance.Is(CustomRoles.Swooper) && __instance.IsAlive()) return false;

            return true;
        }
    }
}