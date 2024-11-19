using BepInEx.Unity.IL2CPP.Utils;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using UnityEngine;

namespace TheBetterRoles.Roles;

public class InvestigatorRole : CustomRoleBehavior
{
    // Role Info
    public override int RoleId => 9;
    public override string RoleColor => "#00FFEE";
    public override CustomRoleBehavior Role => this;
    public override CustomRoleType RoleType => CustomRoleType.Investigator;
    public override CustomRoleTeam RoleTeam => CustomRoleTeam.Crewmate;
    public override CustomRoleCategory RoleCategory => CustomRoleCategory.Information;
    public override TBROptionTab? SettingsTab => BetterTabs.CrewmateRoles;

    public TBROptionItem? FootprintInterval;
    public TBROptionItem? FootprintDuration;
    public TBROptionItem? AnonymousFootprint;
    public override TBROptionItem[]? OptionItems
    {
        get
        {
            return
            [
                FootprintInterval = new TBROptionFloatItem().Create(GetOptionUID(true), SettingsTab,
                Translator.GetString("Role.Investigator.Option.FootprintInterval"), [0.20f, 1f, 0.05f], 0.40f, "", "s", RoleOptionItem),

                FootprintDuration = new TBROptionFloatItem().Create(GetOptionUID(), SettingsTab,
                Translator.GetString("Role.Investigator.Option.FootprintDuration"), [1f, 10f, 0.5f], 3.5f, "", "s", RoleOptionItem),

                AnonymousFootprint = new TBROptionCheckboxItem().Create(GetOptionUID(), SettingsTab,
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

    public override void OnDeinitialize()
    {
        foreach (var body in bodyPrints)
        {
            if (body)
            {
                body.DestroyObj();
            }
        }
    }

    private GameObject? Footprints;
    private Dictionary<byte, float> Timer = [];

    private List<GameObject> bodyPrints = [];
    private List<GameObject> markedBodies = [];
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
        footprint.transform.SetParent(Footprints.transform);
        footprint.transform.position = player.transform.position + new Vector3(0f, 0f, 0.005f) - new Vector3(0f, 0.25f, 0f);
        footprint.transform.LookAt2d(player.GetTruePosition() + player.MyPhysics.Velocity * 1000f);
        footprint.transform.rotation *= Quaternion.Euler(0f, 0f, 90f);
        var spriteRenderer = footprint.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = LoadAbilitySprite("Footprint", 135);
        spriteRenderer.color = !AnonymousFootprint.GetBool() ? Palette.PlayerColors[player.cosmetics.bodyMatProperties.ColorId] : Palette.DisabledGrey;
        var color = spriteRenderer.color;
        spriteRenderer.color = new Color(color.r, color.g, color.b, 0.75f);

        CoroutineManager.Instance.StartCoroutine(CoFadeFootprintOut(footprint, spriteRenderer));
    }

    private void CreateBodyprint(DeadBody body)
    {
        GameObject bodyprint = UnityEngine.Object.Instantiate(body.transform.Find("Sprite").GetComponent<SpriteRenderer>().gameObject);
        bodyprint.transform.SetParent(Footprints.transform, true);
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
