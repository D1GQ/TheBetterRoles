
using BepInEx.Unity.IL2CPP.Utils;
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles;

public class InvestigatorRole : CustomRoleBehavior
{
    // Role Info
    public override string RoleColor => "#00FFEE";
    public override CustomRoleBehavior Role => this;
    public override CustomRoles RoleType => CustomRoles.Investigator;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Crewmate;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Information;
    public override BetterOptionTab? SettingsTab => BetterTabs.CrewmateRoles;

    public BetterOptionItem? FootprintInterval;
    public BetterOptionItem? FootprintDuration;
    public BetterOptionItem? AnonymousFootprint;
    public override BetterOptionItem[]? OptionItems
    {
        get
        {
            return
            [
                FootprintInterval = new BetterOptionFloatItem().Create(GetOptionUID(true), SettingsTab,
                Translator.GetString("Role.Investigator.Option.FootprintInterval"), [0.20f, 1f, 0.05f], 0.40f, "", "s", RoleOptionItem),

                FootprintDuration = new BetterOptionFloatItem().Create(GetOptionUID(), SettingsTab,
                Translator.GetString("Role.Investigator.Option.FootprintDuration"), [1f, 10f, 0.5f], 3.5f, "", "s", RoleOptionItem),

                AnonymousFootprint = new BetterOptionCheckboxItem().Create(GetOptionUID(), SettingsTab,
                Translator.GetString("Role.Investigator.Option.AnonymousFootprint"), false, RoleOptionItem),
            ];
        }
    }
    public override void OnSetUpRole()
    {
        if (GameObject.Find("Footprints") == null)
        {
            Footprints = new GameObject("Footprints");
        }
        else
        {
            Footprints = GameObject.Find("Footprints");
        }
    }
    private GameObject? Footprints;
    private Dictionary<byte, float> Timer = [];
    public override void FixedUpdate()
    {
        if (_player.IsLocalPlayer())
        {
            foreach (var player in Main.AllPlayerControls)
            {
                if (!player.Visible || player.inMovingPlat || player.invisibilityAlpha <= 0f || player.IsLocalPlayer()) continue;

                if (player.MyPhysics.Animations.IsPlayingRunAnimation())
                {
                    if (Timer.ContainsKey(player.PlayerId))
                    {
                        Timer[player.PlayerId] += Time.deltaTime;
                        if (Timer[player.PlayerId] > FootprintInterval.GetFloat())
                        {
                            Timer[player.PlayerId] = 0f;
                            CreateFootprint(player);
                        }
                    }
                    else
                    {
                        Timer[player.PlayerId] = 0f;
                    }
                }
            }
        }
    }

    private void CreateFootprint(PlayerControl player)
    {
        GameObject footprint = new GameObject("Footprint");
        footprint.transform.SetParent(Footprints.transform);
        footprint.transform.position = player.transform.position + new Vector3(0f, 0f, 0.005f) - new Vector3(0f, 0.25f, 0f);
        footprint.transform.LookAt2d(player.GetTruePosition() + player.MyPhysics.Velocity * 1000f);
        footprint.transform.rotation *= Quaternion.Euler(0f, 0f, 90f);
        var spriteRenderer = footprint.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = LoadAbilitySprite("Footprint", 135);
        spriteRenderer.color = !AnonymousFootprint.GetBool() ? Palette.PlayerColors[player.Data.DefaultOutfit.ColorId] : Color.gray;
        var color = spriteRenderer.color;
        spriteRenderer.color = new Color(color.r, color.g, color.b, 0.75f);

        _player.BetterData().StartCoroutine(FadeFootprintOut(footprint, spriteRenderer));
    }

    private System.Collections.IEnumerator FadeFootprintOut(GameObject footprint, SpriteRenderer spriteRenderer)
    {
        yield return new WaitForSeconds(FootprintDuration.GetFloat());

        float fadeDuration = 1f;
        float fadeSpeed = 1f / fadeDuration;
        Color originalColor = spriteRenderer.color;

        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            float alpha = Mathf.Lerp(1f, 0f, t * fadeSpeed);
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);

        UnityEngine.Object.Destroy(footprint);
    }
}
