using BepInEx.Unity.IL2CPP.Utils;
using System.Collections;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core.Interfaces;
using TheBetterRoles.Roles.Core.RoleBase;
using UnityEngine;

namespace TheBetterRoles.Roles.Impostors;

internal sealed class JanitorRole : ImpostorRoleTBR, IRoleAbilityAction<DeadBody>
{
    internal sealed override int RoleId => 3;
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Janitor;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Impostor;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Support;
    internal sealed override OptionTab? SettingsTab => TBRTabs.ImpostorRoles;

    internal OptionItem? CleanCooldown;
    internal OptionItem? KillCooldownClean;
    internal OptionItem? SetKillCooldown;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                CleanCooldown = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Janitor.Option.CleanCooldown", (0f, 180f, 2.5f), 25f, ("", "s"), RoleOptions.RoleOptionItem),
                KillCooldownClean = OptionCheckboxItem.Create(GetOptionUID(), SettingsTab, "Role.Janitor.Option.SetKillCooldownOnClean", true, RoleOptions.RoleOptionItem),
                SetKillCooldown = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Janitor.Option.SetKillCooldown", (0f, 180f, 2.5f), 35f, ("", "s"), KillCooldownClean),
            ];
        }
    }

    private DeadBody? Cleaning;
    internal DeadBodyAbilityButton? CleanButton = new();
    internal sealed override void OnSetUpRole()
    {
        if (_player.IsLocalPlayer())
        {
            CleanButton = RoleButtons.AddButton(DeadBodyAbilityButton.Create(5, Translator.GetString("Role.Janitor.Ability.1"), CleanCooldown.GetFloat(), 0, 0, null, this, true, 0));
        }
    }

    void IRoleAbilityAction<DeadBody>.OnAbility(int id, DeadBody target)
    {
        switch (id)
        {
            case 5:
                {
                    if (target != null && target != Cleaning)
                    {
                        CoroutineManager.Scene.StartCoroutine(CoFadeBodyOut(target));
                        if (KillCooldownClean.GetBool())
                        {
                            RoleButtons.KillButton?.SetCooldown(SetKillCooldown.GetFloat());
                        }
                        Networked.SendRoleSync(target);
                    }
                }
                break;
        }
    }

    private IEnumerator CoFadeBodyOut(DeadBody body)
    {
        Cleaning = body;
        Cleaning.Reported = true;
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
        body.Remove();
    }

    internal override void OnReceiveRoleSync(RoleNetworked.Data data)
    {
        CoroutineManager.Scene.StartCoroutine(CoFadeBodyOut(data.MessageReader.ReadFast<DeadBody>()));
        if (KillCooldownClean.GetBool())
        {
            RoleButtons.KillButton?.SetCooldown(SetKillCooldown.GetFloat());
        }
    }
}