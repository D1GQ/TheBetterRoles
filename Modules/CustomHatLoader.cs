using BepInEx.Unity.IL2CPP.Utils;
using Il2CppInterop.Runtime.Attributes;
using System.Collections;
using System.Text.Json;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using TheBetterRoles.Managers;
using UnityEngine;
using UnityEngine.Networking;

namespace TheBetterRoles.Modules;

public class CustomHatLoader : MonoBehaviour
{
    private bool isRunning;

    private const string RepositoryUrl = "https://raw.githubusercontent.com/D1GQ/TBR_Hats/main";
    private const string ManifestFileName = "manifest.json";
    private readonly string HatsDirectory = TBRDataManager.filePathFolderHats;

    public void Start()
    {
        FetchHats();
    }

    public void FetchHats()
    {
        if (isRunning) return;
        this.StartCoroutine(CoFetchHats());
    }

    [HideFromIl2Cpp]
    private IEnumerator CoFetchHats()
    {
        isRunning = true;

        while (!Utils.IsInternetAvailable())
        {
            yield return new WaitForSeconds(5f);
        }

        var www = new UnityWebRequest($"{RepositoryUrl}/{ManifestFileName}", UnityWebRequest.kHttpVerbGET)
        {
            downloadHandler = new DownloadHandlerBuffer()
        };
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Logger.Error($"Error downloading {ManifestFileName}: {www.error}");
            isRunning = false;
            yield break;
        }

        var options = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
        };

        var response = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(www.downloadHandler.text, options);

        www.Dispose();

        if (response == null || !response.ContainsKey("Hats"))
        {
            Logger.Error($"{ManifestFileName} deserialization failed or no 'Hats' key found.");
            isRunning = false;
            yield break;
        }

        Logger.Log($"Hats {ManifestFileName} loaded! Hats available: {response["Hats"].Count}");

        if (!Directory.Exists(HatsDirectory)) Directory.CreateDirectory(HatsDirectory);

        List<string> toDownload = GenerateDownloadList(response["Hats"]);

        Logger.Log($"Downloading {toDownload.Count} hats.");

        foreach (var folderName in toDownload)
        {
            yield return CoDownloadHatFolder(folderName);
        }

        isRunning = false;
        Destroy(this);
    }

    private List<string> GenerateDownloadList(IEnumerable<string> availableFolders)
    {
        var toDownload = new List<string>();

        foreach (var folderName in availableFolders)
        {
            var folderPath = Path.Combine(HatsDirectory, folderName);
            var configFilePath = Path.Combine(folderPath, "config.json");
            var spritesFolderPath = Path.Combine(folderPath, "sprites");

            if (!Directory.Exists(folderPath) || !File.Exists(configFilePath))
            {
                toDownload.Add(folderName);
                continue;
            }

            try
            {
                var config = JsonSerializer.Deserialize<CustomHatData>(File.ReadAllText(configFilePath));

                if (!string.IsNullOrEmpty(config.Sprite) && !File.Exists(Path.Combine(spritesFolderPath, config.Sprite)) ||
                    !string.IsNullOrEmpty(config.FlipSprite) && !File.Exists(Path.Combine(spritesFolderPath, config.FlipSprite)) ||
                    !string.IsNullOrEmpty(config.BackSprite) && !File.Exists(Path.Combine(spritesFolderPath, config.BackSprite)) ||
                    !string.IsNullOrEmpty(config.FlipBackSprite) && !File.Exists(Path.Combine(spritesFolderPath, config.FlipBackSprite)) ||
                    !string.IsNullOrEmpty(config.ClimbSprite) && !File.Exists(Path.Combine(spritesFolderPath, config.ClimbSprite)))
                {
                    toDownload.Add(folderName);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error reading config for '{folderName}': {ex.Message}");
                toDownload.Add(folderName);
            }
        }

        return toDownload;
    }


    private IEnumerator CoDownloadHatFolder(string folderName)
    {
        string configUrl = $"{RepositoryUrl}/Hats/{folderName}/config.json";
        // Logger.Log($"Fetching config file from: {configUrl}");

        var wwwConfig = new UnityWebRequest(configUrl, UnityWebRequest.kHttpVerbGET)
        {
            downloadHandler = new DownloadHandlerBuffer()
        };
        yield return wwwConfig.SendWebRequest();

        if (wwwConfig.result != UnityWebRequest.Result.Success)
        {
            Logger.Error($"Error fetching config file for '{folderName}': {wwwConfig.error}");
            yield break;
        }

        CustomHatData config;
        try
        {
            config = JsonSerializer.Deserialize<CustomHatData>(wwwConfig.downloadHandler.text);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to deserialize config.json for '{folderName}': {ex.Message}");
            yield break;
        }

        var folderPath = Path.Combine(HatsDirectory, folderName);
        var spritesPath = Path.Combine(folderPath, "sprites");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        if (!Directory.Exists(spritesPath))
        {
            Directory.CreateDirectory(spritesPath);
        }

        string configFilePath = Path.Combine(folderPath, "config.json");
        File.WriteAllText(configFilePath, wwwConfig.downloadHandler.text);
        // Logger.Log($"Saved config file: {configFilePath}");

        wwwConfig.Dispose();

        List<string> filesToDownload = [config.Sprite, config.FlipSprite, config.BackSprite, config.FlipBackSprite, config.ClimbSprite];

        foreach (var fileEntry in filesToDownload)
        {
            string fileName = fileEntry;
            if (string.IsNullOrEmpty(fileName)) continue;

            string fileUrl = $"{RepositoryUrl}/Hats/{folderName}/sprites/{fileName}";
            string localFilePath = Path.Combine(spritesPath, fileName);

            if (File.Exists(localFilePath))
            {
                // Logger.Log($"File already exists: {fileName}. Skipping download.");
                continue;
            }

            Logger.Log($"Downloading file from: {fileUrl}");
            yield return CoDownloadFile(fileUrl, localFilePath, fileName);
        }

        CustomHatManager.LoadAll();
    }

    private IEnumerator CoDownloadFile(string fileUrl, string localFilePath, string fileName)
    {
        var www = new UnityWebRequest(fileUrl, UnityWebRequest.kHttpVerbGET)
        {
            downloadHandler = new DownloadHandlerBuffer()
        };
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Logger.Error($"Error downloading file '{fileName}' from URL '{fileUrl}': {www.error} (Response Code: {(int)www.responseCode})");
            yield break;
        }

        File.WriteAllBytes(localFilePath, www.downloadHandler.data);

        Logger.Log($"Saved file: {localFilePath}");
        www.Dispose();
    }

    public class GitHubFile
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Download_Url { get; set; }
    }
}