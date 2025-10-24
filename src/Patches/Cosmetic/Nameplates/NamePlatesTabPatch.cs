using AmongUs.Data;
using HarmonyLib;
using TheBetterRoles.Helpers;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Network.Configs;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Patches.Cosmetic.Nameplates;

[HarmonyPatch]
internal static class NamePlatesTabPatch
{
    private static TextMeshPro textTemplate;

    [HarmonyPatch(typeof(NameplatesTab), nameof(NameplatesTab.OnEnable))]
    [HarmonyPrefix]
    private static bool OnEnablePrefix(NameplatesTab __instance)
    {
        __instance.scroller.Inner.transform.DestroyChildren();
        __instance.ColorChips = new();

        var unlockedNameplates = HatManager.Instance.GetUnlockedNamePlates().Where(nameplate => nameplate.TryGetConfig().CheckPermission());
        var packages = CreateNamePlatePackages(unlockedNameplates);

        var yOffset = __instance.YStart - 0.5f;
        textTemplate = __instance.transform.Find("Text").GetComponent<TextMeshPro>();
        textTemplate.text = Translator.GetString(StringNames.NamePlates);
        textTemplate.DestroyTextTranslators();

        var orderedKeys = packages.Keys.OrderBy(PackagePriority);
        foreach (var key in orderedKeys)
        {
            yOffset = CreateHatPackage(packages[key], key, yOffset, __instance);
        }

        __instance.scroller.ContentYBounds.max = -(yOffset + 4.1f);
        return false;
    }

    private static Dictionary<string, List<Tuple<NamePlateData, CustomNamePlateData>>> CreateNamePlatePackages(IEnumerable<NamePlateData> Nameplates)
    {
        var packages = new Dictionary<string, List<Tuple<NamePlateData, CustomNamePlateData>>>();

        foreach (var nameplate in Nameplates)
        {
            var ext = CustomHatManager.CustomNamePlatesCache.FirstOrDefault(data => data.Name == nameplate.name);
            var packageKey = ext?.Package ?? CustomHatManager.InnerslothNamePlatePackageName;

            if (!packages.ContainsKey(packageKey))
            {
                packages[packageKey] = [];
            }
            packages[packageKey].Add(new Tuple<NamePlateData, CustomNamePlateData>(nameplate, ext));
        }

        return packages;
    }

    private static int PackagePriority(string package)
    {
        return package switch
        {
            CustomHatManager.InnerslothNamePlatePackageName => 1000,
            CustomHatManager.DeveloperNamePlatePackageName => 0,
            _ => 500
        };
    }

    private static float CreateHatPackage(List<Tuple<NamePlateData, CustomNamePlateData>> Nameplates, string packageName, float yStart, NameplatesTab NameplatesTab)
    {
        var offset = yStart + 0.5f;
        var isDefaultPackage = packageName == CustomHatManager.InnerslothNamePlatePackageName;

        Nameplates = isDefaultPackage ? Nameplates : [.. Nameplates.OrderBy(h => h.Item1.name)];
        offset = AddPackageTitle(packageName, offset, NameplatesTab);

        for (var i = 0; i < Nameplates.Count; i++)
        {
            var (nameplate, ext) = Nameplates[i];
            var (xPos, yPos) = CalculatePosition(i, offset, NameplatesTab, isDefaultPackage);

            var colorChip = InstantiateColorChip(NameplatesTab, nameplate, xPos, yPos);
            SetChipAttributes(colorChip, nameplate, ext, NameplatesTab);
        }

        return offset - (Nameplates.Count - 1) / NameplatesTab.NumPerRow * (isDefaultPackage ? 1f : 1.5f) * NameplatesTab.YOffset - 1.75f;
    }

    private static float AddPackageTitle(string packageName, float yOffset, NameplatesTab NameplatesTab)
    {
        if (textTemplate != null)
        {
            var title = UnityEngine.Object.Instantiate(textTemplate, NameplatesTab.scroller.Inner);
            title.transform.localPosition = new Vector3(2.25f, yOffset, -1f);
            title.transform.localScale = Vector3.one * 1.5f;
            title.fontSize *= 0.5f;
            title.enableAutoSizing = false;

            NameplatesTab.StartCoroutine(Effects.Lerp(0.1f, new Action<float>(p => title.SetText(packageName))));
            yOffset -= 0.4f * NameplatesTab.YOffset;
        }

        return yOffset;
    }

    private static (float xPos, float yPos) CalculatePosition(int index, float yOffset, NameplatesTab NameplatesTab, bool isDefaultPackage)
    {
        var xPos = NameplatesTab.XRange.Lerp(index % NameplatesTab.NumPerRow / (NameplatesTab.NumPerRow - 1f));
        var yPos = yOffset - index / NameplatesTab.NumPerRow * (isDefaultPackage ? 1f : 1.5f) * NameplatesTab.YOffset;
        return (xPos, yPos);
    }

    private static ColorChip InstantiateColorChip(NameplatesTab NameplatesTab, NamePlateData nameplate, float xPos, float yPos)
    {
        var colorChip = UnityEngine.Object.Instantiate(NameplatesTab.ColorTabPrefab, NameplatesTab.scroller.Inner);
        colorChip.transform.localPosition = new Vector3(xPos, yPos, -1f);
        colorChip.Tag = nameplate;
        colorChip.Deselect();
        colorChip.ProductId = nameplate.ProductId;
        NameplatesTab.ColorChips.Add(colorChip);
        return colorChip;
    }

    private static void SetChipAttributes(ColorChip colorChip, NamePlateData nameplate, CustomNamePlateData ext, NameplatesTab NameplatesTab)
    {
        if (ActiveInputManager.currentControlType == ActiveInputManager.InputType.Keyboard)
        {
            colorChip.Button.OnMouseOver.AddListener((Action)(() => NameplatesTab.SelectNameplate(nameplate)));
            colorChip.Button.OnMouseOut.AddListener((Action)(() => NameplatesTab.SelectNameplate(HatManager.Instance.GetNamePlateById(DataManager.Player.Customization.NamePlate))));
            colorChip.Button.OnClick.AddListener((Action)(() => NameplatesTab.ClickEquip()));
        }
        else
        {
            colorChip.Button.OnClick.AddListener((Action)(() => NameplatesTab.SelectNameplate(nameplate)));
        }

        if (ext != null)
        {
            AdjustChipVisuals(colorChip, ext, nameplate, NameplatesTab);
        }

        var asset = nameplate.CreateAddressableAsset();
        asset?.LoadAsync((Action)(() =>
        {
            colorChip.Cast<NameplateChip>().image.sprite = asset.GetAsset().Image;
        }));
    }

    private static void AdjustChipVisuals(ColorChip colorChip, CustomNamePlateData ext, NamePlateData nameplate, NameplatesTab NameplatesTab)
    {
        if (textTemplate != null)
        {
            var description = UnityEngine.Object.Instantiate(textTemplate, colorChip.transform);
            description.transform.localPosition = new Vector3(0f, -0.12f, -5f);
            description.alignment = TextAlignmentOptions.Left;
            description.transform.localScale = Vector3.one * 0.40f;
            description.outlineColor = Color.black;
            description.outlineWidth = 0.05f;

            NameplatesTab.StartCoroutine(Effects.Lerp(0.1f, new Action<float>(p => { description.SetText($"by {ext.Author}"); })));
        }
    }
}
