using System.Text.Json;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;

namespace TheBetterRoles.Managers;

class BetterDataManager
{
    public static string filePathFolder = Path.Combine(Main.GetGamePathToAmongUs(), "BetterRole_Data");
    public static string filePathFolderSaveInfo = Path.Combine(filePathFolder, "SaveInfo");
    public static string filePathFolderSettings = Path.Combine(filePathFolder, "Settings");
    public static string filePathFolderHats = Path.Combine(filePathFolder, "Hats");
    public static string SettingsFile => Path.Combine(filePathFolderSettings, $"Preset-{Main.Preset.Value}.json");
    public static string banPlayerListFile = Path.Combine(filePathFolderSaveInfo, "BanPlayerList.txt");
    public static string banNameListFile = Path.Combine(filePathFolderSaveInfo, "BanNameList.txt");
    public static string banWordListFile = Path.Combine(filePathFolderSaveInfo, "BanWordList.txt");
    public static Dictionary<string, string> _settingsFileCache = [];
    public static Dictionary<int, string> TempSettings = [];
    public static Dictionary<int, string> HostSettings = [];

    public static void SetUp()
    {
        if (!Directory.Exists(filePathFolder))
        {
            Directory.CreateDirectory(filePathFolder);
        }

        if (!Directory.Exists(filePathFolderSettings))
        {
            Directory.CreateDirectory(filePathFolderSettings);
        }

        if (!Directory.Exists(filePathFolderSaveInfo))
        {
            Directory.CreateDirectory(filePathFolderSaveInfo);
        }

        if (!Directory.Exists(filePathFolderHats))
        {
            Directory.CreateDirectory(filePathFolderHats);
        }

        if (!File.Exists(banPlayerListFile))
        {
            File.WriteAllText(banPlayerListFile, "// Example\nFriendCode#0000\nHashPUID\n// Or\nFriendCode#0000, HashPUID");
        }

        if (!File.Exists(banNameListFile))
        {
            File.WriteAllText(banNameListFile, "// Example\nBanName1\nBanName2");
        }

        if (!File.Exists(banWordListFile))
        {
            File.WriteAllText(banWordListFile, "// Example\nStart");
        }

        if (!File.Exists(SettingsFile))
        {
            var initialData = new Dictionary<string, string>();
            string json = JsonSerializer.Serialize(initialData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
        }
    }

    public static void LoadData()
    {
        LoadSettingsIntoTemp();
    }

    private static Dictionary<string, string> LoadFileIfNeeded()
    {
        if (!_settingsFileCache.Any())
        {
            string filePath = SettingsFile;
            string json = File.ReadAllText(filePath);
            _settingsFileCache = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
        }
        return _settingsFileCache;
    }

    public static void LoadSettingsIntoTemp()
    {
        var jsonData = LoadFileIfNeeded();

        if (jsonData.Any() && !TempSettings.Any())
        {
            foreach (var kvp in jsonData)
            {
                if (int.TryParse(kvp.Key, out var @int))
                {
                    TempSettings[@int] = kvp.Value;
                }
            }
        }
    }

    public static void SaveSetting(int id, string input)
    {
        if (GameState.IsInGame && !GameState.IsHost)
        {
            HostSettings[id] = input;
            return;
        }

        TempSettings[id] = input;

        var jsonData = LoadFileIfNeeded();
        jsonData[id.ToString()] = input;

        string json = JsonSerializer.Serialize(jsonData, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsFile, json);
    }

    public static bool CanLoadSetting(int id)
    {
        if (GameState.IsInGame && !GameState.IsHost)
        {
            return HostSettings.ContainsKey(id);
        }

        if (TempSettings.ContainsKey(id))
        {
            return true;
        }

        var jsonData = LoadFileIfNeeded();
        return jsonData.ContainsKey(id.ToString()) && !string.IsNullOrEmpty(jsonData[id.ToString()]);
    }

    public static bool LoadBoolSetting(int id, bool Default = false)
    {
        if (GameState.IsInGame && !GameState.IsHost)
        {
            if (HostSettings.TryGetValue(id, out var setting) && bool.TryParse(setting, out var result))
            {
                return result;
            }
            SaveSetting(id, Default.ToString());
            return Default;
        }

        if (TempSettings.TryGetValue(id, out var tempSetting) && bool.TryParse(tempSetting, out var tempBool))
        {
            return tempBool;
        }

        var jsonData = LoadFileIfNeeded();
        if (jsonData.TryGetValue(id.ToString(), out var value) && bool.TryParse(value, out var fileBool))
        {
            return fileBool;
        }

        SaveSetting(id, Default.ToString());
        return Default;
    }

    public static float LoadFloatSetting(int id, float Default = 1f)
    {
        if (GameState.IsInGame && !GameState.IsHost)
        {
            if (HostSettings.TryGetValue(id, out var setting) && float.TryParse(setting, out var result))
            {
                return result;
            }
            SaveSetting(id, Default.ToString());
            return Default;
        }

        if (TempSettings.TryGetValue(id, out var tempSetting) && float.TryParse(tempSetting, out var tempFloat))
        {
            return tempFloat;
        }

        var jsonData = LoadFileIfNeeded();
        if (jsonData.TryGetValue(id.ToString(), out var value) && float.TryParse(value, out var fileFloat))
        {
            return fileFloat;
        }

        SaveSetting(id, Default.ToString());
        return Default;
    }

    public static int LoadIntSetting(int id, int Default = 1)
    {
        if (GameState.IsInGame && !GameState.IsHost)
        {
            if (HostSettings.TryGetValue(id, out var setting) && int.TryParse(setting, out var result))
            {
                return result;
            }
            SaveSetting(id, Default.ToString());
            return Default;
        }

        if (TempSettings.TryGetValue(id, out var tempSetting) && int.TryParse(tempSetting, out var tempInt))
        {
            return tempInt;
        }

        var jsonData = LoadFileIfNeeded();
        if (jsonData.TryGetValue(id.ToString(), out var value) && int.TryParse(value, out var fileInt))
        {
            return fileInt;
        }

        SaveSetting(id, Default.ToString());
        return Default;
    }


    public static void SaveBanList(string friendCode = "", string hashPUID = "")
    {
        if (!string.IsNullOrEmpty(friendCode) || !string.IsNullOrEmpty(hashPUID))
        {
            // Create the new string with the separator if both are not empty
            string newText = string.Empty;

            if (!string.IsNullOrEmpty(friendCode))
            {
                newText = friendCode;
            }

            if (!string.IsNullOrEmpty(hashPUID))
            {
                if (!string.IsNullOrEmpty(newText))
                {
                    newText += ", ";
                }
                newText += Utils.GetHashStr(hashPUID);
            }

            // Check if the file already contains the new entry
            if (!File.Exists(banPlayerListFile) || !File.ReadLines(banPlayerListFile).Any(line => line.Equals(newText)))
            {
                // Append the new string to the file if it's not already present
                File.AppendAllText(banPlayerListFile, Environment.NewLine + newText);
            }
        }
    }
}
