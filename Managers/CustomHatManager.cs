using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace TheBetterRoles.Managers;

public static class CustomHatManager
{
    private static List<string> developerHatIdList =
    [
        "d1gq"
    ];

    public const string ResourcesDirectory = "TheBetterHats";
    public const string InnerslothPackageName = "Innersloth Hats";
    public const string DeveloperPackageName = "Developer Hats";

    public static List<CustomHatData> UnregisteredHats { get; private set; } = [];
    public static Dictionary<string, HatViewData> ViewDataCache { get; private set; } = [];
    public static List<CustomHatData> CustomHatsCache { get; set; } = [];
    private static List<string> loaded = [];

    public static void LoadAll()
    {
        string path = BetterDataManager.filePathFolderHats;

        if (Directory.Exists(path))
        {
            foreach (var subDir in Directory.GetDirectories(path))
            {
                if (!loaded.Contains(subDir))
                {
                    string configPath = Path.Combine(subDir, "config.json");

                    if (File.Exists(configPath))
                    {
                        var customHat = CustomHatData.Serialize(configPath);
                        if (customHat.Package == DeveloperPackageName && !developerHatIdList.Contains(customHat.Id)) return;
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

    public static HatData CreateHatBehaviour(CustomHatData ch)
    {
        var viewData = ViewDataCache[ch.Name] = ScriptableObject.CreateInstance<HatViewData>();
        var hat = ScriptableObject.CreateInstance<HatData>();

        viewData.MainImage = LoadHatSprite(ch.Folder, ch.Sprite);
        if (viewData.MainImage == null)
        {
            throw new FileNotFoundException("File not downloaded yet");
        }
        viewData.LeftMainImage = LoadHatSprite(ch.Folder, ch.FlipSprite);

        viewData.FloorImage = viewData.MainImage;
        if (!string.IsNullOrEmpty(ch.BackSprite))
        {
            viewData.BackImage = LoadHatSprite(ch.Folder, ch.BackSprite);
            viewData.LeftBackImage = LoadHatSprite(ch.Folder, ch.FlipBackSprite);
            ch.Behind = true;
        }

        if (!string.IsNullOrEmpty(ch.ClimbSprite))
        {
            viewData.ClimbImage = LoadHatSprite(ch.Folder, ch.ClimbSprite);
            viewData.LeftClimbImage = viewData.ClimbImage;
        }
        viewData.MatchPlayerColor = ch.ColorBase;

        hat.name = ch.Name;
        hat.displayOrder = 99;
        hat.ProductId = "hat_" + ch.Id;
        hat.InFront = !ch.Behind;
        hat.NoBounce = !ch.Bounce;
        hat.ChipOffset = new Vector2(0f, 0.2f);
        hat.Free = true;

        CustomHatsCache.Add(ch);
        hat.ViewDataRef = new AssetReference(ViewDataCache[hat.name].Pointer);
        hat.CreateAddressableAsset();
        return hat;
    }

    public static Sprite? LoadHatSprite(string folder, string path)
    {
        var texture = Utils.loadTextureFromDisk(Path.Combine(BetterDataManager.filePathFolderHats, folder, "sprites", path));
        if (texture == null) return null;
        var sprite = Sprite.Create(texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.53f, 0.575f),
            texture.width * 0.375f);
        if (sprite == null) return null;
        texture.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
        sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
        return sprite;
    }

    public static bool TryGetCached(this HatParent hatParent, out HatViewData? asset)
    {
        if (hatParent && hatParent.Hat) return hatParent.Hat.TryGetCached(out asset);
        asset = null;
        return false;
    }

    public static bool TryGetCached(this HatData hat, out HatViewData? asset)
    {
        return ViewDataCache.TryGetValue(hat.name, out asset);
    }

    public static bool IsCached(this HatData hat)
    {
        return ViewDataCache.ContainsKey(hat.name);
    }

    public static bool IsCached(this HatParent hatParent)
    {
        return hatParent.Hat.IsCached();
    }
}
