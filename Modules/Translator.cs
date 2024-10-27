using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TheBetterRoles;

public static class Translator
{
    public static Dictionary<string, Dictionary<int, string>> translateMaps;
    public const string LANGUAGE_FOLDER_NAME = "Language";
    public static void Init()
    {
        Logger.Log("Loading language files...", "Translator");
        LoadLangs();
        Logger.Log("Language file loaded successfully", "Translator");
    }
    public static void LoadLangs()
    {
        try
        {
            // Get the directory containing the JSON files (e.g., TOHE.Resources.Lang)
            string jsonDirectory = "TheBetterRoles.Resources.Lang";
            // Get the assembly containing the resources
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            string[] jsonFileNames = GetJsonFileNames(assembly, jsonDirectory);

            translateMaps = [];


            if (jsonFileNames.Length == 0)
            {
                Logger.Error("Json Translation files does not exist.", "Translator");
                return;
            }
            foreach (string jsonFileName in jsonFileNames)
            {
                // Read the JSON file content
                using Stream resourceStream = assembly.GetManifestResourceStream(jsonFileName);

                if (resourceStream != null)
                {
                    using StreamReader reader = new(resourceStream);

                    string jsonContent = reader.ReadToEnd();
                    // Deserialize the JSON into a dictionary
                    Dictionary<string, string> jsonDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
                    if (jsonDictionary.TryGetValue("LanguageID", out string languageIdObj) && int.TryParse(languageIdObj, out int languageId))
                    {
                        // Remove the "LanguageID" entry
                        jsonDictionary.Remove("LanguageID");

                        // Handle the rest of the data and merge it into the resulting translation map
                        MergeJsonIntoTranslationMap(translateMaps, languageId, jsonDictionary);
                    }
                    else
                    {
                        //Logger.Error(jsonDictionary["HostText"], "Translator");
                        Logger.Error($"Invalid JSON format in {jsonFileName}: Missing or invalid 'LanguageID' field.", "Translator");
                    }
                }
            }

            // Convert the resulting translation map to JSON
            string mergedJson = JsonSerializer.Serialize(translateMaps, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch (Exception ex)
        {
            Logger.Error($"Error: {ex}", "Translator");
        }
        if (!Directory.Exists(LANGUAGE_FOLDER_NAME)) Directory.CreateDirectory(LANGUAGE_FOLDER_NAME);
    }
    static void MergeJsonIntoTranslationMap(Dictionary<string, Dictionary<int, string>> translationMaps, int languageId, Dictionary<string, string> jsonDictionary)
    {
        foreach (var kvp in jsonDictionary)
        {
            string textString = kvp.Key;
            if (kvp.Value is string translation)
            {

                // If the textString is not already in the translation map, add it
                if (!translationMaps.ContainsKey(textString))
                {
                    translationMaps[textString] = [];
                }

                // Add or update the translation for the current id and textString
                if (textString.ToLower().Contains("role."))
                {
                    translationMaps[textString][languageId] = FormatRoleColor(translation.Replace("\\n", "\n").Replace("\\r", "\r"), textString);
                }
                else
                {
                    translationMaps[textString][languageId] = translation.Replace("\\n", "\n").Replace("\\r", "\r");
                }
            }
        }
    }

    // Function to get a list of JSON file names in a directory
    static string[] GetJsonFileNames(System.Reflection.Assembly assembly, string directoryName)
    {
        string[] resourceNames = assembly.GetManifestResourceNames();
        return resourceNames.Where(resourceName => resourceName.StartsWith(directoryName) && resourceName.EndsWith(".json")).ToArray();
    }

    public static string GetString(string s, Dictionary<string, string> replacementDic = null, bool console = false, bool showInvalid = true, bool vanilla = false)
    {
        if (vanilla)
        {
            string nameToFind = s;
            if (Enum.TryParse(nameToFind, out StringNames text))
            {
                return DestroyableSingleton<TranslationController>.Instance.GetString(text);
            }
            else
            {
                return showInvalid ? $"<INVALID:{nameToFind}> (vanillaStr)" : nameToFind;
            }
        }
        var langId = TranslationController.InstanceExists ? TranslationController.Instance.currentLanguage.languageID : SupportedLangs.English;
        if (console) langId = SupportedLangs.English;
        if (Main.ForceOwnLanguage.Value) langId = GetUserTrueLang();
        string str = GetString(s, langId, showInvalid);
        if (replacementDic != null)
            foreach (var rd in replacementDic)
            {
                str = str.Replace(rd.Key, rd.Value);
            }
        return str;
    }

    public static string GetString(string str, SupportedLangs langId, bool showInvalid = true)
    {
        var res = showInvalid ? $"<INVALID:{str}>" : str;
        try
        {
            if (translateMaps.TryGetValue(str, out var dic) && (!dic.TryGetValue((int)langId, out res) || res == "" || (langId is not SupportedLangs.SChinese and not SupportedLangs.TChinese && Regex.IsMatch(res, @"[\u4e00-\u9fa5]") && res == GetString(str, SupportedLangs.SChinese)))) //strに該当する&無効なlangIdかresが空
            {
                if (langId == SupportedLangs.English) res = $"*{str}";
                else res = GetString(str, SupportedLangs.English);
            }
            if (!translateMaps.ContainsKey(str))
            {
                var stringNames = EnumHelper.GetAllValues<StringNames>().Where(x => x.ToString() == str).ToArray();
                if (stringNames != null && stringNames.Any())
                    res = GetString(stringNames.FirstOrDefault());
            }
        }
        catch (Exception Ex)
        {
            Logger.Error($"Error oucured at [{str}] in String.csv", "Translator");
            Logger.Error("Here was the error:\n" + Ex.ToString(), "Translator");
        }
        return res;
    }
    public static string GetString(StringNames stringName)
        => DestroyableSingleton<TranslationController>.Instance.GetString(stringName, new Il2CppReferenceArray<Il2CppSystem.Object>(0));

    public static string FormatRoleColor(string text, string jsonStr)
    {
        var str = text;

        foreach (var role in CustomRoleManager.allRoles)
        {
            string rolePattern = $@"\b{Regex.Escape(role.RoleName)}(s|es)?\b";
            str = Regex.Replace(str, rolePattern, match => $"<{role.RoleColor}>{match.Value}</color>", RegexOptions.IgnoreCase);
        }

        if (text.Contains(' '))
        {
            foreach (var team in Enum.GetValues(typeof(CustomRoleTeam)).Cast<CustomRoleTeam>())
            {
                string teamName = Utils.GetCustomRoleTeamName(team);
                string teamColor = Utils.GetCustomRoleTeamColor(team);

                string teamPattern = $@"\b{Regex.Escape(teamName)}(s|es)?\b";
                str = Regex.Replace(str, teamPattern, match => $"<color={teamColor}>{match.Value}</color>", RegexOptions.IgnoreCase);
            }
        }

        return str;
    }

    public static SupportedLangs GetUserTrueLang()
    {
        try
        {
            var name = CultureInfo.CurrentUICulture.Name;
            if (name.StartsWith("en")) return SupportedLangs.English;
            if (name.StartsWith("zh_CHT")) return SupportedLangs.TChinese;
            if (name.StartsWith("zh")) return SupportedLangs.SChinese;
            if (name.StartsWith("ru")) return SupportedLangs.Russian;
            return TranslationController.Instance.currentLanguage.languageID;
        }
        catch
        {
            return SupportedLangs.English;
        }
    }
}
