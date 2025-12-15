using BepInEx.Unity.IL2CPP.Utils;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Patches.UI.GameSettings;
using TheBetterRoles.Roles.Core.Interfaces;
using UnityEngine;

namespace TheBetterRoles.Roles.Crewmates;

internal sealed class InvestigatorRole : CrewmateRoleTBR, IRoleUpdateAction
{
    internal sealed override int RoleId => 9;
    internal sealed override string RoleColorHex => "#00FFEE";
    internal sealed override RoleClassTypes RoleType => RoleClassTypes.Investigator;
    internal sealed override RoleClassTeam RoleTeam => RoleClassTeam.Crewmate;
    internal sealed override RoleClassCategory RoleCategory => RoleClassCategory.Information;
    internal sealed override OptionTab? SettingsTab => TBRTabs.CrewmateRoles;

    internal OptionItem? FootprintInterval;
    internal OptionItem? FootprintDuration;
    internal OptionItem? AnonymousFootprint;
    internal sealed override OptionItem[]? OptionItems
    {
        get
        {
            return
            [
                FootprintInterval = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Investigator.Option.FootprintInterval", (0.20f, 1f, 0.05f), 0.40f, ("", "s"), RoleOptions.RoleOptionItem),
                FootprintDuration = OptionFloatItem.Create(GetOptionUID(), SettingsTab, "Role.Investigator.Option.FootprintDuration", (1f, 10f, 0.5f), 3.5f, ("", "s"), RoleOptions.RoleOptionItem),
                AnonymousFootprint = OptionCheckboxItem.Create(GetOptionUID(), SettingsTab, "Role.Investigator.Option.AnonymousFootprint", false, RoleOptions.RoleOptionItem),
            ];
        }
    }

    private GameObject? footprints;
    private readonly Dictionary<byte, float> timer = [];
    private readonly List<GameObject> bodyPrints = [];
    private readonly List<GameObject> markedBodies = [];
    internal sealed override void OnSetUpRole()
    {
        if (GameObject.Find("Footprints") == null)
        {
            footprints = new GameObject("Footprints");
        }
        else
        {
            footprints = GameObject.Find("Footprints");
        }
    }

    internal sealed override void OnDeinitialize()
    {
        foreach (var body in bodyPrints)
        {
            if (body)
            {
                body.DestroyObj();
            }
        }
    }

    void IRoleUpdateAction.FixedUpdate()
    {
        if (_player.IsLocalPlayer())
        {
            foreach (var player in Main.AllPlayerControls)
            {
                if (player == null) continue;
                if (!player.Visible || player.inMovingPlat || player.invisibilityAlpha <= 0f || player.IsLocalPlayer()) continue;

                if (player.MyPhysics.Animations.IsPlayingRunAnimation())
                {
                    if (timer.ContainsKey(player.PlayerId))
                    {
                        timer[player.PlayerId] += Time.deltaTime;
                        if (timer[player.PlayerId] > FootprintInterval.GetFloat())
                        {
                            timer[player.PlayerId] = 0f;
                            CreateFootprint(player);
                        }
                    }
                    else
                    {
                        timer[player.PlayerId] = 0f;
                    }
                }
            }

            foreach (var body in Main.AllDeadBodys)
            {
                if (body)
                {
                    if (!markedBodies.Contains(body.gameObject))
                    {
                        markedBodies.Add(body.gameObject);
                        CreateBodyprint(body);
                    }
                }
            }
        }
    }

    private void CreateFootprint(PlayerControl player)
    {
        GameObject footprint = new("Footprint");
        footprint.transform.SetParent(footprints.transform);
        footprint.transform.position = player.transform.position + new Vector3(0f, 0f, 0.005f) - new Vector3(0f, 0.25f, 0f);
        footprint.transform.LookAt2d(player.GetTruePosition() + player.MyPhysics.Velocity * 1000f);
        footprint.transform.rotation *= Quaternion.Euler(0f, 0f, 90f);
        var spriteRenderer = footprint.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = LoadAbilitySprite("Footprint", 135);
        spriteRenderer.color = !AnonymousFootprint.GetBool() ? Palette.PlayerColors[player.cosmetics.bodyMatProperties.ColorId] : Palette.DisabledGrey;
        var color = spriteRenderer.color;
        spriteRenderer.color = new Color(color.r, color.g, color.b, 0.75f);

        CoroutineManager.Scene.StartCoroutine(CoFadeFootprintOut(footprint, spriteRenderer));
    }

    private void CreateBodyprint(DeadBody body)
    {
        GameObject bodyprint = UnityEngine.Object.Instantiate(body.transform.Find("Sprite").GetComponent<SpriteRenderer>().gameObject);
        bodyprint.transform.SetParent(footprints.transform, true);
        var sprite = bodyprint.GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            sprite.color = Palette.PlayerColors[Utils.PlayerDataFromPlayerId(body.ParentId).DefaultOutfit.ColorId] - new Color(0f, 0f, 0f, 0.5f);
        }
        bodyprint.transform.localPosition = body.transform.Find("Sprite").position;
        bodyprint.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
        bodyprint.transform.position += new Vector3(0f, 0f, 0.0005f);
        bodyPrints.Add(bodyprint);
    }

    private System.Collections.IEnumerator CoFadeFootprintOut(GameObject footprint, SpriteRenderer spriteRenderer)
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

        footprint.DestroyObj();
    }
}
