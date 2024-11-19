using BepInEx.Unity.IL2CPP.Utils;
using Hazel;
using System.Collections;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles.Roles;

public class JanitorRole : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 3;
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Janitor;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Impostor;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Support;
    public override TBROptionTab? SettingsTab => BetterTabs.ImpostorRoles;

    public TBROptionItem? CleanCooldown;
    public TBROptionItem? KillCooldownClean;
    public TBROptionItem? SetKillCooldown;

    public override TBROptionItem[]? OptionItems
    {
        get
        {
            return
            [
                CleanCooldown = new TBROptionFloatItem().Create(GetOptionUID(true), SettingsTab, Translator.GetString("Role.Janitor.Option.CleanCooldown"), [0f, 180f, 2.5f], 25f, "", "s", RoleOptionItem),
                KillCooldownClean = new TBROptionCheckboxItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Janitor.Option.SetKillCooldownOnClean"), true, RoleOptionItem),
                SetKillCooldown = new TBROptionFloatItem().Create(GetOptionUID(), SettingsTab, Translator.GetString("Role.Janitor.Option.SetKillCooldown"), [0f, 180f, 2.5f], 35f, "", "s", KillCooldownClean),
            ];
        }
    }

    private DeadBody? Cleaning;
    bool IsVisible { get; set; } = true;
    public DeadBodyAbilityButton? CleanButton = new();
    public override void OnSetUpRole()
    {
        CleanButton = AddButton(new DeadBodyAbilityButton().Create(5, Translator.GetString("Role.Janitor.Ability.1"), CleanCooldown.GetFloat(), 0, 0, null, this, true, 0));
    }

    public override void OnAbility(int id, MessageReader? reader, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body)
    {
        switch (id)
        {
            case 5:
                CoroutineManager.Instance.StartCoroutine(CoFadeBodyOut(body));
                if (KillCooldownClean.GetBool())
                {
                    KillButton?.SetCooldown(SetKillCooldown.GetFloat());
                }
                break;
        }
    }

    public override bool CheckAbility(int id, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body)
    {
        switch (id)
        {
            case 5:
                {
                    if (body == null || body == Cleaning)
                    {
                        return false;
                    }
                }
                break;
        }

        return true;
    }

    private IEnumerator CoFadeBodyOut(DeadBody body)
    {
        Cleaning = body;
        bool fading = true;
        float fadeDuration = 1f;
        float fadeSpeed = 1f / fadeDuration;

        while (fading)
        {
            fading = false;

            foreach (var renderer in body.bodyRenderers)
            {
                Color currentColor = renderer.color;

                float newAlpha = Mathf.Max(currentColor.a - fadeSpeed * Time.deltaTime, 0f);
                renderer.color = new Color(currentColor.r, currentColor.g, currentColor.b, newAlpha);

                if (newAlpha > 0f)
                {
                    fading = true;
                }
            }

            yield return null;
        }

        Cleaning = null;
        body.DestroyObj();
    }
}
