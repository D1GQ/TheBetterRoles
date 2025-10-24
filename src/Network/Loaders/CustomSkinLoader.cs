using Il2CppInterop.Runtime.Attributes;
using System.Collections;
using System.Text.Json;
using TheBetterRoles.Data;
using TheBetterRoles.Items.Interfaces;
using TheBetterRoles.Managers;
using TheBetterRoles.Network.Configs;
using UnityEngine;
using UnityEngine.Networking;

namespace TheBetterRoles.Network.Loaders;

/// <summary>
/// The CustomSkinLoader class handles the downloading, configuration, and management of custom skins for the game.
/// It fetches skin configuration data and assets from a remote repository, manages the download and 
/// installation of the skin assets, and ensures that they are available for use in the game.
/// The class checks the availability of the internet and manages retries if the connection is not initially available.
/// It processes configuration files and sprite assets, ensuring that missing assets are downloaded.
/// It also integrates with the CustomHatManager to load the downloaded skins.
/// </summary>
internal class CustomSkinLoader : MonoBehaviour, IGithubLoader
{
    public string RepositoryUrl => "https://raw.githubusercontent.com/D1GQ/TBR_Hats/main";
    private const string ManifestFileName = "manifest.json";
    private const string ManifestName = "Skins";
    private readonly string SkinsDirectory = TBRDataManager.SkinsFolder;

    /// <summary>
    /// Coroutine to fetch the manifest from the remote repository and check the availability of skins.
    /// If the skins are missing, it will download them and ensure that they are correctly stored for use in-game.
    /// </summary>
    [HideFromIl2Cpp]
    internal IEnumerator CoFetchSkins()
    {
        int count = 0;
        float hang = 0;
        while (!GithubAPI.IsInternetAvailable())
        {
            count++;
            if (count >= 17)
            {
                Destroy(this);
                yield break;
            }
            if (hang < 30f) hang += 2.5f;
            yield return new WaitForSeconds(hang);
        }

        GithubAPI.DownloadingContentQueue.Add(true);
        string callBack = "";
        yield return GitHubFile.CoDownloadManifest(RepositoryUrl, ManifestFileName, (string text) =>
        {
            callBack = text;
        });

        var options = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
        };

        var response = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(callBack, options);

        if (response == null || !response.ContainsKey(ManifestName))
        {
            Logger.Error($"{ManifestFileName} deserialization failed or no '{ManifestName}' key found.");
            NetworkLogger.Error($"{ManifestFileName} deserialization failed or no '{ManifestName}' key found.");
            yield break;
        }

        NetworkLogger.Log($"Skins {ManifestFileName} loaded! Skins available: {response[ManifestName].Count}");

        if (!Directory.Exists(SkinsDirectory)) Directory.CreateDirectory(SkinsDirectory);

        List<string> toDownload = GenerateDownloadList(response[ManifestName]);

        NetworkLogger.Log($"Downloading {toDownload.Count} Skins.");

        foreach (var folderName in toDownload)
        {
            yield return CoDownloadSkinFolder(folderName);
        }

        CustomHatManager.RegisterSkins();

        GithubAPI.DownloadingContentQueue.Add(false);
        Destroy(this);
    }

    /// <summary>
    /// Generates a list of skins that need to be downloaded based on missing configuration or sprite assets.
    /// Ensures that any incomplete or missing skin data is redownloaded to maintain consistency.
    /// </summary>
    [HideFromIl2Cpp]
    private List<string> GenerateDownloadList(IEnumerable<string> availableFolders)
    {
        var toDownload = new List<string>();

        foreach (var folderName in availableFolders)
        {
            var folderPath = Path.Combine(SkinsDirectory, folderName);
            var configFilePath = Path.Combine(folderPath, "config.json");
            var spritesFolderPath = Path.Combine(folderPath, "sprites");

            if (!Directory.Exists(folderPath) || !File.Exists(configFilePath))
            {
                toDownload.Add(folderName);
                continue;
            }

            try
            {
                var config = JsonSerializer.Deserialize<CustomSkinData>(File.ReadAllText(configFilePath));

                if (!string.IsNullOrEmpty(config.Sprite) && !File.Exists(Path.Combine(spritesFolderPath, config.Sprite)) ||
                    !string.IsNullOrEmpty(config.Preview) && !File.Exists(Path.Combine(spritesFolderPath, config.Preview)) ||
                    !string.IsNullOrEmpty(config.Eject) && !File.Exists(Path.Combine(spritesFolderPath, config.Eject)))
                {
                    toDownload.Add(folderName);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error reading config for '{folderName}': {ex.Message}");
                NetworkLogger.Error($"Error reading config for '{folderName}': {ex.Message}");
                toDownload.Add(folderName);
            }
        }

        return toDownload;
    }

    /// <summary>
    /// Downloads a specific skin folder, including its configuration and sprite assets.
    /// Ensures the directory structure is created and all necessary files are retrieved.
    /// </summary>
    [HideFromIl2Cpp]
    private IEnumerator CoDownloadSkinFolder(string folderName)
    {
        string configUrl = $"{RepositoryUrl}/{ManifestName}/{folderName}/config.json";

        var wwwConfig = new UnityWebRequest(configUrl, UnityWebRequest.kHttpVerbGET)
        {
            downloadHandler = new DownloadHandlerBuffer()
        };
        yield return wwwConfig.SendWebRequest();

        if (wwwConfig.result != UnityWebRequest.Result.Success)
        {
            Logger.Error($"Error fetching config file for '{folderName}': {wwwConfig.error}");
            NetworkLogger.Error($"Error fetching config file for '{folderName}': {wwwConfig.error}");
            yield break;
        }

        CustomSkinData config;
        try
        {
            config = JsonSerializer.Deserialize<CustomSkinData>(wwwConfig.downloadHandler.text);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to deserialize config.json for '{folderName}': {ex.Message}");
            NetworkLogger.Error($"Failed to deserialize config.json for '{folderName}': {ex.Message}");
            yield break;
        }

        var folderPath = Path.Combine(SkinsDirectory, folderName);
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

        wwwConfig.Dispose();

        List<string> filesToDownload = [config.Sprite, config.Preview, config.Eject];

        foreach (var fileEntry in filesToDownload)
        {
            string fileName = fileEntry;
            if (string.IsNullOrEmpty(fileName)) continue;

            string fileUrl = $"{RepositoryUrl}/{ManifestName}/{folderName}/sprites/{fileName}";
            string localFilePath = Path.Combine(spritesPath, fileName);

            if (File.Exists(localFilePath))
            {
                continue;
            }

            NetworkLogger.Log($"Downloading file from: {fileUrl}");
            yield return GitHubFile.CoDownloadFile(fileUrl, localFilePath, fileName);
        }

        CustomHatManager.RegisterSkins();
    }
}