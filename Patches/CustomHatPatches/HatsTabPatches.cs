using AmongUs.Data;
using HarmonyLib;
using TheBetterRoles.Items;
using TheBetterRoles.Managers;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Patches;

[HarmonyPatch]
public static class HatsTabPatches
{
    private static TextMeshPro textTemplate;

    [HarmonyPatch(typeof(HatsTab), nameof(HatsTab.OnEnable))]
    [HarmonyPrefix]
    private static bool OnEnablePrefix(HatsTab __instance)
    {
        __instance.scroller.Inner.transform.DestroyChildren();
        __instance.ColorChips = new Il2CppSystem.Collections.Generic.List<ColorChip>();

        var unlockedHats = DestroyableSingleton<HatManager>.Instance.GetUnlockedHats();
        var packages = CreateHatPackages(unlockedHats);

        var yOffset = __instance.YStart;
        textTemplate = GameObject.Find("HatsGroup").transform.Find("Text").GetComponent<TextMeshPro>();

        var orderedKeys = packages.Keys.OrderBy(PackagePriority);
        foreach (var key in orderedKeys)
        {
            yOffset = CreateHatPackage(packages[key], key, yOffset, __instance);
        }

        __instance.scroller.ContentYBounds.max = -(yOffset + 4.1f);
        return false;
    }

    private static Dictionary<string, List<Tuple<HatData, CustomHatData>>> CreateHatPackages(IEnumerable<HatData> hats)
    {
        var packages = new Dictionary<string, List<Tuple<HatData, CustomHatData>>>();

        foreach (var hat in hats)
        {
            var ext = CustomHatManager.CustomHatsCache.FirstOrDefault(data => data.Name == hat.name);
            var packageKey = ext?.Package ?? CustomHatManager.InnerslothPackageName;

            if (!packages.ContainsKey(packageKey))
            {
                packages[packageKey] = new List<Tuple<HatData, CustomHatData>>();
            }
            packages[packageKey].Add(new Tuple<HatData, CustomHatData>(hat, ext));
        }

        return packages;
    }

    private static int PackagePriority(string package)
    {
        return package switch
        {
            CustomHatManager.InnerslothPackageName => 1000,
            CustomHatManager.DeveloperPackageName => 0,
            _ => 500
        };
    }

    private static float CreateHatPackage(List<Tuple<HatData, CustomHatData>> hats, string packageName, float yStart, HatsTab hatsTab)
    {
        var offset = yStart;
        var isDefaultPackage = packageName == CustomHatManager.InnerslothPackageName;

        hats = isDefaultPackage ? hats : hats.OrderBy(h => h.Item1.name).ToList();
        offset = AddPackageTitle(packageName, offset, hatsTab);

        for (var i = 0; i < hats.Count; i++)
        {
            var (hat, ext) = hats[i];
            var (xPos, yPos) = CalculatePosition(i, offset, hatsTab, isDefaultPackage);

            var colorChip = InstantiateColorChip(hatsTab, hat, xPos, yPos);
            SetChipAttributes(colorChip, hat, ext, hatsTab);
        }

        return offset - (hats.Count - 1) / hatsTab.NumPerRow * (isDefaultPackage ? 1f : 1.5f) * hatsTab.YOffset - 1.75f;
    }

    private static float AddPackageTitle(string packageName, float yOffset, HatsTab hatsTab)
    {
        if (textTemplate != null)
        {
            var title = UnityEngine.Object.Instantiate(textTemplate, hatsTab.scroller.Inner);
            title.transform.localPosition = new Vector3(2.25f, yOffset, -1f);
            title.transform.localScale = Vector3.one * 1.5f;
            title.fontSize *= 0.5f;
            title.enableAutoSizing = false;

            hatsTab.StartCoroutine(Effects.Lerp(0.1f, new Action<float>(p => title.SetText(packageName))));
            yOffset -= 0.8f * hatsTab.YOffset;
        }

        return yOffset;
    }

    private static (float xPos, float yPos) CalculatePosition(int index, float yOffset, HatsTab hatsTab, bool isDefaultPackage)
    {
        var xPos = hatsTab.XRange.Lerp(index % hatsTab.NumPerRow / (hatsTab.NumPerRow - 1f));
        var yPos = yOffset - index / hatsTab.NumPerRow * (isDefaultPackage ? 1f : 1.5f) * hatsTab.YOffset;
        return (xPos, yPos);
    }

    private static ColorChip InstantiateColorChip(HatsTab hatsTab, HatData hat, float xPos, float yPos)
    {
        var colorChip = UnityEngine.Object.Instantiate(hatsTab.ColorTabPrefab, hatsTab.scroller.Inner);
        colorChip.transform.localPosition = new Vector3(xPos, yPos, -1f);
        colorChip.Tag = hat;
        colorChip.SelectionHighlight.gameObject.SetActive(false);
        hatsTab.ColorChips.Add(colorChip);
        return colorChip;
    }

    private static void SetChipAttributes(ColorChip colorChip, HatData hat, CustomHatData ext, HatsTab hatsTab)
    {
        if (ActiveInputManager.currentControlType == ActiveInputManager.InputType.Keyboard)
        {
            colorChip.Button.OnMouseOver.AddListener((Action)(() => hatsTab.SelectHat(hat)));
            colorChip.Button.OnMouseOut.AddListener((Action)(() => hatsTab.SelectHat(DestroyableSingleton<HatManager>.Instance.GetHatById(DataManager.Player.Customization.Hat))));
            colorChip.Button.OnClick.AddListener((Action)(() => hatsTab.ClickEquip()));
        }
        else
        {
            colorChip.Button.OnClick.AddListener((Action)(() => hatsTab.SelectHat(hat)));
        }

        colorChip.Button.ClickMask = hatsTab.scroller.Hitbox;
        colorChip.Inner.SetMaskType(PlayerMaterial.MaskType.SimpleUI);
        hatsTab.UpdateMaterials(colorChip.Inner.FrontLayer, hat);

        if (ext != null)
        {
            AdjustChipVisuals(colorChip, ext, hat, hatsTab);
        }

        colorChip.Inner.SetHat(hat, hatsTab.HasLocalPlayer() ? PlayerControl.LocalPlayer.Data.DefaultOutfit.ColorId : DataManager.Player.Customization.Color);
        colorChip.Inner.transform.localPosition = hat.ChipOffset;
    }

    private static void AdjustChipVisuals(ColorChip colorChip, CustomHatData ext, HatData hat, HatsTab hatsTab)
    {
        var background = colorChip.transform.Find("Background");
        var foreground = colorChip.transform.Find("ForeGround");

        if (background != null)
        {
            background.localPosition = Vector3.down * 0.243f;
            background.localScale = new Vector3(background.localScale.x, 0.8f, background.localScale.y);
        }
        if (foreground != null)
        {
            foreground.localPosition = Vector3.down * 0.243f;
        }

        if (textTemplate != null)
        {
            var description = UnityEngine.Object.Instantiate(textTemplate, colorChip.transform);
            description.transform.localPosition = new Vector3(0f, -0.65f, -1f);
            description.alignment = TextAlignmentOptions.Center;
            description.transform.localScale = Vector3.one * 0.65f;

            hatsTab.StartCoroutine(Effects.Lerp(0.1f, new Action<float>(p => { description.SetText($"{hat.name}\nby {ext.Author}"); })));
        }
    }
}
