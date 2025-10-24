using Il2CppInterop.Runtime.Attributes;
using System.Collections;
using System.Text.Json;
using TheBetterRoles.Data;
using TheBetterRoles.Items.Interfaces;
using TheBetterRoles.Network.Configs;
using UnityEngine;

namespace TheBetterRoles.Network.Loaders;

internal class CosmeticConfigLoader : MonoBehaviour, IGithubLoader
{
    public string RepositoryUrl => "https://raw.githubusercontent.com/D1GQ/TBR_Hats/main";
    private const string ConfigurationFileName = "configuration.json";

    /// <summary>
    /// Coroutine to fetch the cosmetic configuration from the remote repository. Retries if no internet connection is found.
    /// </summary>
    [HideFromIl2Cpp]
    internal IEnumerator CoFetchCosmeticConfigs()
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

        NetworkLogger.Log($"Downloading Cosmetic Configs");
        GithubAPI.DownloadingContentQueue.Add(true);
        string callBack = "";
        yield return GitHubFile.CoDownloadManifest(RepositoryUrl, ConfigurationFileName, (string text) =>
        {
            callBack = text;
        });

        var options = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true
        };

        try
        {
            var Configs = JsonSerializer.Deserialize<List<CustomCosmeticConfig>>(callBack, options);
            NetworkLogger.Log($"Loaded {Configs.Count} Cosmetic Configs");
            if (Configs != null)
            {
                foreach (var cosmeticConfig in Configs)
                {
                    ReadOnlyManager.AllCustomCosmeticConfigurations.Add(cosmeticConfig);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error parsing JSON: {ex.Message}");
            NetworkLogger.Error($"Error parsing JSON: {ex.Message}");
        }

        GithubAPI.DownloadingContentQueue.Add(false);
        Destroy(this);
    }
}
