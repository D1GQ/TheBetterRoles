
using BepInEx.Unity.IL2CPP.Utils;
using System.Collections;
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles;

public class NoiseMakerAddon : CustomAddonBehavior
{
    // Role Info
    public override string RoleColor => "#920086";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.NoiseMaker;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.None;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.HelpfulAddon;
    public override BetterOptionTab? SettingsTab => BetterTabs.Addons;
    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
            ];
        }
    }

    public override void OnMurder(PlayerControl killer, PlayerControl target, bool Suicide, bool IsAbility)
    {
        if (target == _player)
        {
            _player.StartCoroutine(PlayNoiseNotification());
        }
    }

    private IEnumerator PlayNoiseNotification(float duration = 15f, float flashInterval = 0.5f)
    {
        var arrow = new ArrowLocator().Create(_player.transform.position, minDistance: 0.25f);
        var spriteRenderer = arrow.SpriteRenderer;
        spriteRenderer.color = Color.white;

        // Calculate the number of flashes based on the duration and interval
        int flashCount = Mathf.FloorToInt(duration / flashInterval);

        // Flash for the specified duration
        for (int i = 0; i < flashCount; i++)
        {
            Color startColor = (i % 2 == 0) ? new Color(1f, 0f, 0f) : new Color(0.5f, 0f, 0f);
            Color endColor = (i % 2 == 0) ? new Color(0.5f, 0f, 0f) : new Color(1f, 0f, 0f);

            float elapsedTime = 0f;

            // Gradually fade between the colors during the flash interval
            while (elapsedTime < flashInterval)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / flashInterval;

                // Lerp from startColor to endColor over time
                spriteRenderer.color = Color.Lerp(startColor, endColor, t);

                // Wait for the next frame
                yield return null;
            }
        }

        // Destroy the arrow after the duration
        arrow.Remove();
    }
}
