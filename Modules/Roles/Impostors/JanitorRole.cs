
using BepInEx.Unity.IL2CPP.Utils;
using System.Collections;
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles;

public class JanitorRole : CustomRoleBehavior
{
    // Role Info
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Janitor;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Impostor;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Support;
    public override bool CanKill => true;
    public override bool CanSabotage => true;
    public override bool CanVent => AllowVenting.GetBool();
    public override BetterOptionTab? SettingsTab => BetterTabs.ImpostorRoles;

    public BetterOptionItem? CleanCooldown;
    public BetterOptionItem? KillCooldownClean;
    public BetterOptionItem? SetKillCooldown;
    public BetterOptionItem? AllowVenting;

    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
                CleanCooldown = new BetterOptionFloatItem().Create(RoleId + 10, SettingsTab, Translator.GetString("Role.Janitor.Option.CleanCooldown"), [0f, 180f, 2.5f], 25f, "", "s", RoleOptionItem),
                KillCooldownClean = new BetterOptionCheckboxItem().Create(RoleId + 15, SettingsTab, Translator.GetString("Role.Janitor.Option.SetKillCooldownOnClean"), true, RoleOptionItem),
                SetKillCooldown = new BetterOptionFloatItem().Create(RoleId + 20, SettingsTab, Translator.GetString("Role.Janitor.Option.SetKillCooldown"), [0f, 180f, 2.5f], 35f, "", "s", KillCooldownClean),
                AllowVenting = new BetterOptionCheckboxItem().Create(RoleId + 25, SettingsTab, Translator.GetString("Role.Ability.CanVent"), false, RoleOptionItem),
            ];
        }
    }

    private DeadBody? Cleaning;
    bool IsVisible { get; set; } = true;
    public AbilityButton? CleanButton = new();
    public override void OnSetUpRole()
    {
        CleanButton = AddButton(new DeadBodyButton().Create(5, Translator.GetString("Role.Janitor.Ability.1"), CleanCooldown.GetFloat(), 0, null, this, true, 1f)) as AbilityButton;
    }

    public override void OnAbility(int id, CustomRoleBehavior role, PlayerControl? target, Vent? vent, DeadBody? body)
    {
        switch (id)
        {
            case 5:
                _player.StartCoroutine(FadeBodyOut(body));
                if (KillCooldownClean.GetBool())
                {
                    KillButton.SetCooldown(SetKillCooldown.GetFloat());
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

    private IEnumerator FadeBodyOut(DeadBody body)
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
        UnityEngine.Object.Destroy(body.gameObject);
    }
}
