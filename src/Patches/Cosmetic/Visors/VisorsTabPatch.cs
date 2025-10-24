using AmongUs.Data;
using HarmonyLib;
using TheBetterRoles.Helpers;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Network.Configs;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Patches.Cosmetic.Visors;

[HarmonyPatch]
internal static class VisorsTabPatch
{
    private static TextMeshPro textTemplate;

    [HarmonyPatch(typeof(VisorsTab), nameof(VisorsTab.OnEnable))]
    [HarmonyPrefix]
    private static bool OnEnablePrefix(VisorsTab __instance)
    {
        __instance.scroller.Inner.transform.DestroyChildren();
        __instance.ColorChips = new();

        var unlockedVisors = HatManager.Instance.GetUnlockedVisors().Where(visor => visor.TryGetConfig().CheckPermission());
        var packages = CreateVisorPackages(unlockedVisors);

        var yOffset = __instance.YStart;
        textTemplate = __instance.transform.Find("Text").GetComponent<TextMeshPro>();
        textTemplate.text = Translator.GetString(StringNames.Visors);
        textTemplate.DestroyTextTranslators();

        var orderedKeys = packages.Keys.OrderBy(PackagePriority);
        foreach (var key in orderedKeys)
        {
            yOffset = CreateHatPackage(packages[key], key, yOffset, __instance);
        }

        __instance.scroller.ContentYBounds.max = -(yOffset + 4.1f);
        return false;
    }

    private static Dictionary<string, List<Tuple<VisorData, CustomVisorData>>> CreateVisorPackages(IEnumerable<VisorData> visors)
    {
        var packages = new Dictionary<string, List<Tuple<VisorData, CustomVisorData>>>();

        foreach (var visor in visors)
        {
            var ext = CustomHatManager.CustomVisorsCache.FirstOrDefault(data => data.Name == visor.name);
            var packageKey = ext?.Package ?? CustomHatManager.InnerslothVisorPackageName;

            if (!packages.ContainsKey(packageKey))
            {
                packages[packageKey] = [];
            }
            packages[packageKey].Add(new Tuple<VisorData, CustomVisorData>(visor, ext));
        }

        return packages;
    }

    private static int PackagePriority(string package)
    {
        return package switch
        {
            CustomHatManager.InnerslothVisorPackageName => 1000,
            CustomHatManager.DeveloperVisorPackageName => 0,
            _ => 500
        };
    }

    private static float CreateHatPackage(List<Tuple<VisorData, CustomVisorData>> visors, string packageName, float yStart, VisorsTab visorsTab)
    {
        var offset = yStart;
        var isDefaultPackage = packageName == CustomHatManager.InnerslothVisorPackageName;

        visors = isDefaultPackage ? visors : [.. visors.OrderBy(h => h.Item1.name)];
        offset = AddPackageTitle(packageName, offset, visorsTab);

        for (var i = 0; i < visors.Count; i++)
        {
            var (visor, ext) = visors[i];
            var (xPos, yPos) = CalculatePosition(i, offset, visorsTab, isDefaultPackage);

            var colorChip = InstantiateColorChip(visorsTab, visor, xPos, yPos);
            SetChipAttributes(colorChip, visor, ext, visorsTab);
        }

        return offset - (visors.Count - 1) / visorsTab.NumPerRow * (isDefaultPackage ? 1f : 1.5f) * visorsTab.YOffset - 1.75f;
    }

    private static float AddPackageTitle(string packageName, float yOffset, VisorsTab visorsTab)
    {
        if (textTemplate != null)
        {
            var title = UnityEngine.Object.Instantiate(textTemplate, visorsTab.scroller.Inner);
            title.transform.localPosition = new Vector3(2.25f, yOffset, -1f);
            title.transform.localScale = Vector3.one * 1.5f;
            title.fontSize *= 0.5f;
            title.enableAutoSizing = false;

            visorsTab.StartCoroutine(Effects.Lerp(0.1f, new Action<float>(p => title.SetText(packageName))));
            yOffset -= 0.8f * visorsTab.YOffset;
        }

        return yOffset;
    }

    private static (float xPos, float yPos) CalculatePosition(int index, float yOffset, VisorsTab visorsTab, bool isDefaultPackage)
    {
        var xPos = visorsTab.XRange.Lerp(index % visorsTab.NumPerRow / (visorsTab.NumPerRow - 1f));
        var yPos = yOffset - index / visorsTab.NumPerRow * (isDefaultPackage ? 1f : 1.5f) * visorsTab.YOffset;
        return (xPos, yPos);
    }

    private static ColorChip InstantiateColorChip(VisorsTab visorsTab, VisorData visor, float xPos, float yPos)
    {
        var colorChip = UnityEngine.Object.Instantiate(visorsTab.ColorTabPrefab, visorsTab.scroller.Inner);
        colorChip.transform.localPosition = new Vector3(xPos, yPos, -1f);
        colorChip.Tag = visor;
        colorChip.Deselect();
        colorChip.ProductId = visor.ProductId;
        visorsTab.ColorChips.Add(colorChip);
        return colorChip;
    }

    private static void SetChipAttributes(ColorChip colorChip, VisorData visor, CustomVisorData ext, VisorsTab visorsTab)
    {
        if (ActiveInputManager.currentControlType == ActiveInputManager.InputType.Keyboard)
        {
            colorChip.Button.OnMouseOver.AddListener((Action)(() => visorsTab.SelectVisor(visor)));
            colorChip.Button.OnMouseOut.AddListener((Action)(() => visorsTab.SelectVisor(HatManager.Instance.GetVisorById(DataManager.Player.Customization.Visor))));
            colorChip.Button.OnClick.AddListener((Action)(() => visorsTab.ClickEquip()));
        }
        else
        {
            colorChip.Button.OnClick.AddListener((Action)(() => visorsTab.SelectVisor(visor)));
        }

        colorChip.Button.ClickMask = visorsTab.scroller.Hitbox;
        colorChip.Inner.SetMaskType(PlayerMaterial.MaskType.SimpleUI);
        visorsTab.UpdateMaterials(colorChip.Inner.FrontLayer, visor);

        if (ext != null)
        {
            AdjustChipVisuals(colorChip, ext, visor, visorsTab);
        }

        visor.SetPreview(colorChip.Inner.FrontLayer, visorsTab.HasLocalPlayer() ? PlayerControl.LocalPlayer.Data.DefaultOutfit.ColorId : ((int)DataManager.Player.Customization.Color));
        colorChip.Inner.transform.localPosition = visor.ChipOffset;
    }

    private static void AdjustChipVisuals(ColorChip colorChip, CustomVisorData ext, VisorData visor, VisorsTab visorsTab)
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
            description.transform.localPosition = new Vector3(0f, -0.65f, -1f);
            description.alignment = TextAlignmentOptions.Center;
            description.transform.localScale = Vector3.one * 0.65f;

            visorsTab.StartCoroutine(Effects.Lerp(0.1f, new Action<float>(p => { description.SetText($"{visor.name}\nby {ext.Author}"); })));
        }
    }
}
