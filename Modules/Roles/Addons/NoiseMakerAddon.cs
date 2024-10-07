
using BepInEx.Unity.IL2CPP.Utils;
using System.Collections;
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles;

public class BaitAddon : CustomAddonBehavior
{
    // Role Info
    public override string RoleColor => "#920086";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.NoiseMaker;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.None;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.HelpfulAddon;
    public override BetterOptionTab? SettingsTab => BetterTabs.Addons;
    public BetterOptionItem? ArrowDuration;
    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
                ArrowDuration = new BetterOptionFloatItem().Create(GenerateOptionId(true), SettingsTab, Translator.GetString("Role.NoiseMaker.Option.ArrowDuration"), [5f, 15f, 2.5f], 10f, "", "s", RoleOptionItem),
            ];
        }
    }

    public override void OnMurder(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility)
    {
        if (target == _player)
        {
            _player.StartCoroutine(PlayNoiseNotification(ArrowDuration.GetFloat()));
        }
    }

    private IEnumerator PlayNoiseNotification(float duration = 15f, float flashInterval = 0.5f)
    {
        var arrow = new ArrowLocator().Create(_player.transform.position, Utils.LoadSprite("TheBetterRoles.Resources.Images.Icons.NoiseMaker-Arrow.png", 250f), minDistance: 0.25f);
        var spriteRenderer = arrow.SpriteRenderer;
        spriteRenderer.color = Color.white;

        int flashCount = Mathf.FloorToInt(duration / flashInterval);

        for (int i = 0; i < flashCount; i++)
        {
            Color startColor = (i % 2 == 0) ? new Color(1f, 0f, 0f) : new Color(0.5f, 0f, 0f);
            Color endColor = (i % 2 == 0) ? new Color(0.5f, 0f, 0f) : new Color(1f, 0f, 0f);

            float elapsedTime = 0f;

            while (elapsedTime < flashInterval)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / flashInterval;

                spriteRenderer.color = Color.Lerp(startColor, endColor, t);

                yield return null;
            }
        }

        float fadeDuration = 1f;
        float fadeElapsedTime = 0f;
        Color initialColor = spriteRenderer.color;

        while (fadeElapsedTime < fadeDuration)
        {
            fadeElapsedTime += Time.deltaTime;
            float t = fadeElapsedTime / fadeDuration;

            spriteRenderer.color = Color.Lerp(initialColor, new Color(initialColor.r, initialColor.g, initialColor.b, 0f), t);

            yield return null;
        }

        arrow.Remove();
    }
}
