using AmongUs.Data;
using HarmonyLib;
using TheBetterRoles.Helpers;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Network.Configs;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Patches.Cosmetic.Skins;

[HarmonyPatch]
internal static class SkinsTabPatch
{
    private static TextMeshPro textTemplate;

    [HarmonyPatch(typeof(SkinsTab), nameof(SkinsTab.OnEnable))]
    [HarmonyPrefix]
    private static bool OnEnablePrefix(SkinsTab __instance)
    {
        __instance.scroller.Inner.transform.DestroyChildren();
        __instance.ColorChips = new();

        var unlockedSkins = HatManager.Instance.GetUnlockedSkins().Where(skin => skin.TryGetConfig().CheckPermission());
        var packages = CreateSkinPackages(unlockedSkins);

        var yOffset = __instance.YStart;
        textTemplate = __instance.transform.Find("Text").GetComponent<TextMeshPro>();
        textTemplate.text = Translator.GetString(StringNames.SkinLabel);
        textTemplate.DestroyTextTranslators();

        var orderedKeys = packages.Keys.OrderBy(PackagePriority);
        foreach (var key in orderedKeys)
        {
            yOffset = CreateHatPackage(packages[key], key, yOffset, __instance);
        }

        __instance.scroller.ContentYBounds.max = -(yOffset + 4.1f);
        return false;
    }

    private static Dictionary<string, List<Tuple<SkinData, CustomSkinData>>> CreateSkinPackages(IEnumerable<SkinData> skins)
    {
        var packages = new Dictionary<string, List<Tuple<SkinData, CustomSkinData>>>();

        foreach (var skin in skins)
        {
            var ext = CustomHatManager.CustomSkinsCache.FirstOrDefault(data => data.Name == skin.name);
            var packageKey = ext?.Package ?? CustomHatManager.InnerslothSkinPackageName;

            if (!packages.ContainsKey(packageKey))
            {
                packages[packageKey] = [];
            }
            packages[packageKey].Add(new Tuple<SkinData, CustomSkinData>(skin, ext));
        }

        return packages;
    }

    private static int PackagePriority(string package)
    {
        return package switch
        {
            CustomHatManager.InnerslothSkinPackageName => 1000,
            CustomHatManager.DeveloperSkinPackageName => 0,
            _ => 500
        };
    }

    private static float CreateHatPackage(List<Tuple<SkinData, CustomSkinData>> skins, string packageName, float yStart, SkinsTab skinsTab)
    {
        var offset = yStart;
        var isDefaultPackage = packageName == CustomHatManager.InnerslothSkinPackageName;

        skins = isDefaultPackage ? skins : [.. skins.OrderBy(h => h.Item1.name)];
        offset = AddPackageTitle(packageName, offset, skinsTab);

        for (var i = 0; i < skins.Count; i++)
        {
            var (skin, ext) = skins[i];
            var (xPos, yPos) = CalculatePosition(i, offset, skinsTab, isDefaultPackage);

            var colorChip = InstantiateColorChip(skinsTab, skin, xPos, yPos);
            SetChipAttributes(colorChip, skin, ext, skinsTab);
        }

        return offset - (skins.Count - 1) / skinsTab.NumPerRow * (isDefaultPackage ? 1f : 1.5f) * skinsTab.YOffset - 1.75f;
    }

    private static float AddPackageTitle(string packageName, float yOffset, SkinsTab skinsTab)
    {
        if (textTemplate != null)
        {
            var title = UnityEngine.Object.Instantiate(textTemplate, skinsTab.scroller.Inner);
            title.transform.localPosition = new Vector3(2.25f, yOffset, -1f);
            title.transform.localScale = Vector3.one * 1.5f;
            title.fontSize *= 0.5f;
            title.enableAutoSizing = false;

            skinsTab.StartCoroutine(Effects.Lerp(0.1f, new Action<float>(p => title.SetText(packageName))));
            yOffset -= 0.8f * skinsTab.YOffset;
        }

        return yOffset;
    }

    private static (float xPos, float yPos) CalculatePosition(int index, float yOffset, SkinsTab skinsTab, bool isDefaultPackage)
    {
        var xPos = skinsTab.XRange.Lerp(index % skinsTab.NumPerRow / (skinsTab.NumPerRow - 1f));
        var yPos = yOffset - index / skinsTab.NumPerRow * (isDefaultPackage ? 1f : 1.5f) * skinsTab.YOffset;
        return (xPos, yPos);
    }

    private static ColorChip InstantiateColorChip(SkinsTab skinsTab, SkinData skin, float xPos, float yPos)
    {
        var colorChip = UnityEngine.Object.Instantiate(skinsTab.ColorTabPrefab, skinsTab.scroller.Inner);
        colorChip.transform.localPosition = new Vector3(xPos, yPos, -1f);
        colorChip.Tag = skin;
        colorChip.Deselect();
        colorChip.ProductId = skin.ProductId;
        skinsTab.ColorChips.Add(colorChip);
        return colorChip;
    }

    private static void SetChipAttributes(ColorChip colorChip, SkinData skin, CustomSkinData ext, SkinsTab skinsTab)
    {
        if (ActiveInputManager.currentControlType == ActiveInputManager.InputType.Keyboard)
        {
            colorChip.Button.OnMouseOver.AddListener((Action)(() => skinsTab.SelectSkin(skin)));
            colorChip.Button.OnMouseOut.AddListener((Action)(() => skinsTab.SelectSkin(HatManager.Instance.GetSkinById(DataManager.Player.Customization.Skin))));
            colorChip.Button.OnClick.AddListener((Action)(() => skinsTab.ClickEquip()));
        }
        else
        {
            colorChip.Button.OnClick.AddListener((Action)(() => skinsTab.SelectSkin(skin)));
        }

        colorChip.Button.ClickMask = skinsTab.scroller.Hitbox;
        colorChip.Inner.SetMaskType(PlayerMaterial.MaskType.SimpleUI);
        skinsTab.UpdateMaterials(colorChip.Inner.FrontLayer, skin);

        if (ext != null)
        {
            AdjustChipVisuals(colorChip, ext, skin, skinsTab);
        }

        var plus = ext != null ? new Vector2(0f, 0.4f) : Vector2.zero;
        skin.SetPreview(colorChip.Inner.FrontLayer, skinsTab.HasLocalPlayer() ? PlayerControl.LocalPlayer.Data.DefaultOutfit.ColorId : ((int)DataManager.Player.Customization.Color));
        colorChip.Inner.transform.localPosition = skin.ChipOffset + plus;
    }

    private static void AdjustChipVisuals(ColorChip colorChip, CustomSkinData ext, SkinData skin, SkinsTab skinsTab)
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
            float plus = 0.26f;
            var transform1 = foreground.Find("Shade");
            transform1.localScale = new Vector3(transform1.localScale.x, transform1.localScale.y + plus, transform1.localScale.z);
            var transform2 = foreground.Find("CheckMark").transform;
            transform2.position = new Vector3(transform2.position.x, transform2.position.y + plus, transform2.position.z);
        }

        if (textTemplate != null)
        {
            var description = UnityEngine.Object.Instantiate(textTemplate, colorChip.transform);
            description.transform.localPosition = new Vector3(0f, -0.65f, -5f);
            description.alignment = TextAlignmentOptions.Center;
            description.transform.localScale = Vector3.one * 0.65f;

            skinsTab.StartCoroutine(Effects.Lerp(0.1f, new Action<float>(p => { description.SetText($"{skin.name}\nby {ext.Author}"); })));
        }
    }
}
