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
/// The CustomVisorLoader class handles the downloading, configuration, and management of custom visors for the game.
/// It fetches visor configuration data and assets from a remote repository, manages the download and 
/// installation of the visor assets, and ensures that they are available for use in the game.
/// The class checks the availability of the internet and manages retries if the connection is not initially available.
/// It processes configuration files and sprite assets, ensuring that missing assets are downloaded.
/// It also integrates with the CustomHatManager to load the downloaded visors.
/// </summary>
internal class CustomVisorLoader : MonoBehaviour, IGithubLoader
{
    public string RepositoryUrl => "https://raw.githubusercontent.com/D1GQ/TBR_Hats/main";
    private const string ManifestFileName = "manifest.json";
    private const string ManifestName = "Visors";
    private readonly string VisorsDirectory = TBRDataManager.VisorsFolder;

    /// <summary>
    /// Coroutine to fetch the manifest from the remote repository and check the availability of visors.
    /// If the visors are missing, it will download them and ensure that they are correctly stored for use in-game.
    /// </summary>
    [HideFromIl2Cpp]
    internal IEnumerator CoFetchVisors()
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

        NetworkLogger.Log($"Visors {ManifestFileName} loaded! Visors available: {response[ManifestName].Count}");

        if (!Directory.Exists(VisorsDirectory)) Directory.CreateDirectory(VisorsDirectory);

        List<string> toDownload = GenerateDownloadList(response[ManifestName]);

        NetworkLogger.Log($"Downloading {toDownload.Count} Visors.");

        foreach (var folderName in toDownload)
        {
            yield return CoDownloadVisorFolder(folderName);
        }

        CustomHatManager.RegisterVisors();

        GithubAPI.DownloadingContentQueue.Add(false);
        Destroy(this);
    }

    /// <summary>
    /// Generates a list of visors that need to be downloaded based on missing configuration or sprite assets.
    /// Ensures that any incomplete or missing visor data is redownloaded to maintain consistency.
    /// </summary>
    [HideFromIl2Cpp]
    private List<string> GenerateDownloadList(IEnumerable<string> availableFolders)
    {
        var toDownload = new List<string>();

        foreach (var folderName in availableFolders)
        {
            var folderPath = Path.Combine(VisorsDirectory, folderName);
            var configFilePath = Path.Combine(folderPath, "config.json");
            var spritesFolderPath = Path.Combine(folderPath, "sprites");

            if (!Directory.Exists(folderPath) || !File.Exists(configFilePath))
            {
                toDownload.Add(folderName);
                continue;
            }

            try
            {
                var config = JsonSerializer.Deserialize<CustomVisorData>(File.ReadAllText(configFilePath));

                if (!string.IsNullOrEmpty(config.Sprite) && !File.Exists(Path.Combine(spritesFolderPath, config.Sprite)) ||
                    !string.IsNullOrEmpty(config.FlipSprite) && !File.Exists(Path.Combine(spritesFolderPath, config.FlipSprite)) ||
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
    /// Downloads a specific visor folder, including its configuration and sprite assets.
    /// Ensures the directory structure is created and all necessary files are retrieved.
    /// </summary>
    [HideFromIl2Cpp]
    private IEnumerator CoDownloadVisorFolder(string folderName)
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

        CustomVisorData config;
        try
        {
            config = JsonSerializer.Deserialize<CustomVisorData>(wwwConfig.downloadHandler.text);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to deserialize config.json for '{folderName}': {ex.Message}");
            NetworkLogger.Error($"Failed to deserialize config.json for '{folderName}': {ex.Message}");
            yield break;
        }

        var folderPath = Path.Combine(VisorsDirectory, folderName);
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

        List<string> filesToDownload = [config.Sprite, config.FlipSprite, config.ClimbSprite];

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

        CustomHatManager.RegisterVisors();
    }
}