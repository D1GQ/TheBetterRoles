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
/// The CustomHatLoader class handles the downloading, configuration, and management of custom hats for the game.
/// It fetches hat configuration data and assets from a remote repository, manages the download and 
/// installation of the hat assets, and ensures that they are available for use in the game.
/// The class checks the availability of the internet and manages retries if the connection is not initially available.
/// It processes configuration files and sprite assets, ensuring that missing assets are downloaded.
/// It also integrates with the CustomHatManager to load the downloaded hats.
/// </summary>
internal class CustomHatLoader : MonoBehaviour, IGithubLoader
{
    public string RepositoryUrl => "https://raw.githubusercontent.com/D1GQ/TBR_Hats/main";
    private const string ManifestFileName = "manifest.json";
    private const string ManifestName = "Hats";
    private readonly string HatsDirectory = TBRDataManager.HatsFolder;

    /// <summary>
    /// Coroutine to fetch the manifest from the remote repository and check the availability of hats.
    /// If the hats are missing, it will download them.
    /// </summary>
    [HideFromIl2Cpp]
    internal IEnumerator CoFetchHats()
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

        NetworkLogger.Log($"Hats {ManifestFileName} loaded! Hats available: {response[ManifestName].Count}");

        if (!Directory.Exists(HatsDirectory)) Directory.CreateDirectory(HatsDirectory);

        List<string> toDownload = GenerateDownloadList(response[ManifestName]);

        NetworkLogger.Log($"Downloading {toDownload.Count} Hats.");

        foreach (var folderName in toDownload)
        {
            yield return CoDownloadHatFolder(folderName);
        }

        CustomHatManager.RegisterHats();

        GithubAPI.DownloadingContentQueue.Add(false);
        Destroy(this);
    }

    /// <summary>
    /// Generates a list of hats that need to be downloaded based on the current local and remote assets.
    /// </summary>
    [HideFromIl2Cpp]
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
                NetworkLogger.Error($"Error reading config for '{folderName}': {ex.Message}");
                toDownload.Add(folderName);
            }
        }

        return toDownload;
    }

    /// <summary>
    /// Downloads a specific hat folder, including its configuration and sprite assets.
    /// </summary>
    [HideFromIl2Cpp]
    private IEnumerator CoDownloadHatFolder(string folderName)
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

        CustomHatData config;
        try
        {
            config = JsonSerializer.Deserialize<CustomHatData>(wwwConfig.downloadHandler.text);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to deserialize config.json for '{folderName}': {ex.Message}");
            NetworkLogger.Error($"Failed to deserialize config.json for '{folderName}': {ex.Message}");
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

        wwwConfig.Dispose();

        List<string> filesToDownload = [config.Sprite, config.FlipSprite, config.BackSprite, config.FlipBackSprite, config.ClimbSprite];

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

        CustomHatManager.RegisterHats();
    }
}