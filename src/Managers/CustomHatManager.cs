using TheBetterRoles.Data;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using TheBetterRoles.Network.Configs;
using UnityEngine;

namespace TheBetterRoles.Managers;

internal static class CustomHatManager
{
    internal const string InnerslothSkinPackageName = "Innersloth Skins";
    internal const string DeveloperSkinPackageName = "Developer Skins";
    internal const string InnerslothHatPackageName = "Innersloth Hats";
    internal const string DeveloperHatPackageName = "Developer Hats";
    internal const string InnerslothVisorPackageName = "Innersloth Visors";
    internal const string DeveloperVisorPackageName = "Developer Visors";
    internal const string InnerslothNamePlatePackageName = "Innersloth Nameplates";
    internal const string DeveloperNamePlatePackageName = "Developer Nameplates";

    internal static List<CustomSkinData> UnregisteredSkins { get; private set; } = [];
    internal static List<CustomSkinData> CustomSkinsCache { get; set; } = [];
    internal static List<string> SkinsCacheProdId { get; set; } = [];

    internal static List<CustomHatData> UnregisteredHats { get; private set; } = [];
    internal static List<CustomHatData> CustomHatsCache { get; set; } = [];

    internal static List<CustomVisorData> UnregisteredVisors { get; private set; } = [];
    internal static List<CustomVisorData> CustomVisorsCache { get; set; } = [];

    internal static List<CustomNamePlateData> UnregisteredNamePlates { get; private set; } = [];
    internal static List<CustomNamePlateData> CustomNamePlatesCache { get; set; } = [];

    private static List<string> loaded = [];

    internal static void RegisterSkins()
    {
        string path = TBRDataManager.SkinsFolder;

        if (Directory.Exists(path))
        {
            foreach (var subDir in Directory.GetDirectories(path))
            {
                if (!loaded.Contains(subDir))
                {
                    string configPath = Path.Combine(subDir, "config.json");

                    if (File.Exists(configPath))
                    {
                        var customSkin = CustomSkinData.Deserialize(configPath);
                        UnregisteredSkins.Add(customSkin);
                        loaded.Add(subDir);
                    }
                }
            }
        }
        else
        {
            Logger.Error($"Directory '{path}' does not exist.");
        }
    }

    internal static void RegisterHats()
    {
        string path = TBRDataManager.HatsFolder;

        if (Directory.Exists(path))
        {
            foreach (var subDir in Directory.GetDirectories(path))
            {
                if (!loaded.Contains(subDir))
                {
                    string configPath = Path.Combine(subDir, "config.json");

                    if (File.Exists(configPath))
                    {
                        var customHat = CustomHatData.Deserialize(configPath);
                        UnregisteredHats.Add(customHat);
                        loaded.Add(subDir);
                    }
                }
            }
        }
        else
        {
            Logger.Error($"Directory '{path}' does not exist.");
        }
    }

    internal static void RegisterVisors()
    {
        string path = TBRDataManager.VisorsFolder;

        if (Directory.Exists(path))
        {
            foreach (var subDir in Directory.GetDirectories(path))
            {
                if (!loaded.Contains(subDir))
                {
                    string configPath = Path.Combine(subDir, "config.json");

                    if (File.Exists(configPath))
                    {
                        var customVisor = CustomVisorData.Deserialize(configPath);
                        UnregisteredVisors.Add(customVisor);
                        loaded.Add(subDir);
                    }
                }
            }
        }
        else
        {
            Logger.Error($"Directory '{path}' does not exist.");
        }
    }

    internal static void RegisterNamePlates()
    {
        string path = TBRDataManager.NamePlatesFolder;

        if (Directory.Exists(path))
        {
            foreach (var subDir in Directory.GetDirectories(path))
            {
                if (!loaded.Contains(subDir))
                {
                    string configPath = Path.Combine(subDir, "config.json");

                    if (File.Exists(configPath))
                    {
                        var customNamePlate = CustomNamePlateData.Deserialize(configPath);
                        UnregisteredNamePlates.Add(customNamePlate);
                        loaded.Add(subDir);
                    }
                }
            }
        }
        else
        {
            Logger.Error($"Directory '{path}' does not exist.");
        }
    }

    internal static SkinData CreateSkinBehaviour(CustomSkinData customSkin)
    {
        CustomSkinsCache.Add(customSkin);

        var skinData = ScriptableObject.CreateInstance<SkinData>();

        skinData.name = customSkin.Name;
        skinData.displayOrder = 99;
        skinData.ProductId = "skin_" + customSkin.Id;
        skinData.ChipOffset = new Vector2(0f, 0f);
        skinData.Free = true;
        SkinsCacheProdId.Add(skinData.ProductId);

        var viewData = ScriptableObject.CreateInstance<SkinViewData>();

        viewData.IdleFrame = Utils.LoadCosmeticSprite(TBRDataManager.SkinsFolder, customSkin.Folder, customSkin.Sprite);
        viewData.EjectFrame = viewData.IdleFrame;
        viewData.MatchPlayerColor = customSkin.ColorBase;
        skinData.ViewDataRef = new CustomAddressables<SkinViewData>(viewData, skinData.ProductId).AssetReference;
        var preview = Utils.LoadCosmeticSprite(TBRDataManager.SkinsFolder, customSkin.Folder, customSkin.Preview);
        skinData.PreviewData = new CustomAddressables<PreviewViewData>(new PreviewViewData() { PreviewSprite = preview }, skinData.ProductId + "_preview").AssetReference;

        var refAsset = HatManager.Instance.GetSkinById(customSkin.RefSkinId).CreateAddressableAsset();
        refAsset.LoadAsync((Action)(() =>
        {
            SkinViewData refSkin = refAsset.GetAsset();
            viewData.IdleAnim = refSkin.IdleAnim;
            viewData.IdleLeftAnim = refSkin.IdleLeftAnim;
            viewData.RunAnim = refSkin.RunAnim;
            viewData.RunLeftAnim = refSkin.RunLeftAnim;
            viewData.EnterVentAnim = refSkin.EnterVentAnim;
            viewData.EnterLeftVentAnim = refSkin.EnterLeftVentAnim;
            viewData.ExitVentAnim = refSkin.ExitVentAnim;
            viewData.ExitLeftVentAnim = refSkin.ExitLeftVentAnim;
            viewData.ClimbAnim = refSkin.ClimbAnim;
            viewData.ClimbDownAnim = refSkin.ClimbDownAnim;
            viewData.SpawnAnim = refSkin.SpawnAnim;
            viewData.SpawnLeftAnim = refSkin.SpawnLeftAnim;
            viewData.KillTongueImpostor = refSkin.KillTongueImpostor;
            viewData.KillTongueVictim = refSkin.KillTongueVictim;
            viewData.KillShootImpostor = refSkin.KillShootImpostor;
            viewData.KillShootVictim = refSkin.KillShootVictim;
            viewData.KillNeckImpostor = refSkin.KillNeckImpostor;
            viewData.KillNeckVictim = refSkin.KillNeckVictim;
            viewData.KillStabImpostor = refSkin.KillStabImpostor;
            viewData.KillStabVictim = refSkin.KillStabVictim;
            viewData.KillRHMVictim = refSkin.KillRHMVictim;
        }));

        return skinData;
    }

    internal static HatData CreateHatBehaviour(CustomHatData customHat)
    {
        CustomHatsCache.Add(customHat);

        var viewData = ScriptableObject.CreateInstance<HatViewData>();

        viewData.MainImage = Utils.LoadCosmeticSprite(TBRDataManager.HatsFolder, customHat.Folder, customHat.Sprite);
        viewData.LeftMainImage = Utils.LoadCosmeticSprite(TBRDataManager.HatsFolder, customHat.Folder, customHat.FlipSprite);
        viewData.FloorImage = viewData.MainImage;
        viewData.BackImage = Utils.LoadCosmeticSprite(TBRDataManager.HatsFolder, customHat.Folder, customHat.BackSprite);
        if (viewData.BackImage != null)
        {
            viewData.LeftBackImage = Utils.LoadCosmeticSprite(TBRDataManager.HatsFolder, customHat.Folder, customHat.FlipBackSprite);
            customHat.Behind = true;
        }
        viewData.ClimbImage = Utils.LoadCosmeticSprite(TBRDataManager.HatsFolder, customHat.Folder, customHat.ClimbSprite);
        viewData.LeftClimbImage = viewData.ClimbImage;

        viewData.MatchPlayerColor = customHat.ColorBase;

        var hatData = ScriptableObject.CreateInstance<HatData>();

        hatData.name = customHat.Name;
        hatData.displayOrder = 99;
        hatData.ProductId = "hat_" + customHat.Id;
        hatData.InFront = !customHat.Behind;
        hatData.NoBounce = !customHat.Bounce;
        hatData.ChipOffset = new Vector2(0f, 0.2f);
        hatData.Free = true;

        hatData.ViewDataRef = new CustomAddressables<HatViewData>(viewData, hatData.ProductId).AssetReference;
        hatData.PreviewData = new CustomAddressables<PreviewViewData>(new PreviewViewData() { PreviewSprite = viewData.MainImage }, hatData.ProductId + "_preview").AssetReference;

        return hatData;
    }

    internal static VisorData CreateVisorBehaviour(CustomVisorData customVisor)
    {
        CustomVisorsCache.Add(customVisor);

        var viewData = ScriptableObject.CreateInstance<VisorViewData>();

        viewData.IdleFrame = Utils.LoadCosmeticSprite(TBRDataManager.VisorsFolder, customVisor.Folder, customVisor.Sprite);
        viewData.LeftIdleFrame = Utils.LoadCosmeticSprite(TBRDataManager.VisorsFolder, customVisor.Folder, customVisor.FlipSprite);
        viewData.ClimbFrame = Utils.LoadCosmeticSprite(TBRDataManager.VisorsFolder, customVisor.Folder, customVisor.ClimbSprite);
        viewData.FloorFrame = viewData.IdleFrame ?? viewData.LeftIdleFrame ?? viewData.ClimbFrame ?? null;

        viewData.MatchPlayerColor = customVisor.ColorBase;

        var visorData = ScriptableObject.CreateInstance<VisorData>();

        visorData.name = customVisor.Name;
        visorData.displayOrder = 99;
        visorData.ProductId = "visor_" + customVisor.Id;
        visorData.behindHats = customVisor.BehindHats;
        visorData.ChipOffset = new Vector2(0f, 0.2f);
        visorData.Free = true;

        visorData.ViewDataRef = new CustomAddressables<VisorViewData>(viewData, visorData.ProductId).AssetReference;
        visorData.PreviewData = new CustomAddressables<PreviewViewData>(new PreviewViewData() { PreviewSprite = viewData.IdleFrame }, visorData.ProductId + "_preview").AssetReference;

        return visorData;
    }

    internal static NamePlateData CreateNamePlateBehaviour(CustomNamePlateData customNamePlat)
    {
        CustomNamePlatesCache.Add(customNamePlat);

        var viewData = ScriptableObject.CreateInstance<NamePlateViewData>();

        Sprite original = Utils.LoadCosmeticSprite(TBRDataManager.NamePlatesFolder, customNamePlat.Folder, customNamePlat.Sprite);
        viewData.Image = Sprite.Create(
            original.texture,
            original.rect,
            new Vector2(0.5f, 0.5f)
        );

        var namePlateData = ScriptableObject.CreateInstance<NamePlateData>();

        namePlateData.name = customNamePlat.Name;
        namePlateData.displayOrder = 99;
        namePlateData.ProductId = "nameplate_" + customNamePlat.Id;
        namePlateData.ChipOffset = new Vector2(0f, 0f);
        namePlateData.Free = true;

        namePlateData.ViewDataRef = new CustomAddressables<NamePlateViewData>(viewData, namePlateData.ProductId).AssetReference;
        namePlateData.PreviewData = new CustomAddressables<PreviewViewData>(new PreviewViewData() { PreviewSprite = viewData.Image }, namePlateData.ProductId + "_preview").AssetReference;

        return namePlateData;
    }
}
