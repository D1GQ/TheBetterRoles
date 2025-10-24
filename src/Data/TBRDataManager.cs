namespace TheBetterRoles.Data;

class TBRDataManager
{
    internal static GameSettingsFile GameSettingsFile = new();

    internal static string filePathFolder = Path.Combine(Main.GetGamePathToAmongUs(), "BetterRole_Data");
    internal static string OldSaveInfoFolder = Path.Combine(filePathFolder, "SaveInfo");
    internal static string SettingsFolder = Path.Combine(filePathFolder, "Settings");

    internal static string OldFolderHatsFolder = Path.Combine(filePathFolder, "Hats");
    internal static string CosmeticsFolder = Path.Combine(filePathFolder, "Cosmetics");
    internal static string SkinsFolder = Path.Combine(CosmeticsFolder, "Skins");
    internal static string HatsFolder = Path.Combine(CosmeticsFolder, "Hats");
    internal static string VisorsFolder = Path.Combine(CosmeticsFolder, "Visors");
    internal static string NamePlatesFolder = Path.Combine(CosmeticsFolder, "Nameplates");
    internal static string SettingsFile => Path.Combine(SettingsFolder, $"Preset-{Main.Preset.Value}.dat");
    internal static string LogFile = Path.Combine(filePathFolder, $"betterrole-log{GetGameInstanceText()}.txt");
    internal static string PreviousLogFile = Path.Combine(filePathFolder, "betterrole-previous-log.txt");
    internal static string NetworkLogFile = Path.Combine(filePathFolder, $"network-log{GetGameInstanceText()}.txt");

    private static string GetGameInstanceText()
    {
        int index = Main.GetGameInstanceIndex();
        if (index > 0)
        {
            return $"-{index}";
        }
        else
        {
            return "";
        }
    }

    internal static void SetUp()
    {
        SetupFolders();

        if (!File.Exists(SettingsFile))
        {
            File.WriteAllText(SettingsFile, "");
        }
    }

    private static readonly List<(string, int)> Folders =
        [
            (OldFolderHatsFolder, -1),
            (OldSaveInfoFolder, -1),
            (filePathFolder, 0),
            (SettingsFolder, 0),
            (CosmeticsFolder, 1),
            (SkinsFolder, 1),
            (HatsFolder, 1),
            (VisorsFolder, 1),
            (NamePlatesFolder, 1)
        ];

    private static void SetupFolders()
    {
        foreach (var list in Folders)
        {
            if (!Directory.Exists(list.Item1))
            {
                if (list.Item2 == -1) continue;

                var folder = Directory.CreateDirectory(list.Item1);

                if (list.Item2 == 1)
                {
                    folder.Attributes = FileAttributes.Hidden;
                }
            }
            else
            {
                if (list.Item2 == -1)
                {
                    Directory.Delete(list.Item1, true);
                }
            }
        }
    }

    internal static void LoadData()
    {
        GameSettingsFile.Init();
    }

    internal static void SaveSetting(int id, object? input)
    {
        GameSettingsFile.Settings[id] = input;
        GameSettingsFile.Save();
    }

    internal static bool CanLoadSetting<T>(int id)
    {
        if (GameSettingsFile.Settings.TryGetValue(id, out var value))
        {
            try
            {
                var converted = Convert.ChangeType(value, typeof(T));
                return true;
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    internal static T? LoadSetting<T>(int id, T? Default = default)
    {
        if (GameSettingsFile.Settings.TryGetValue(id, out var value))
        {
            try
            {
                var converted = (T)Convert.ChangeType(value, typeof(T));
                return converted;
            }
            catch
            {
                SaveSetting(id, Default);
            }
        }

        SaveSetting(id, Default);
        return Default;
    }
}
