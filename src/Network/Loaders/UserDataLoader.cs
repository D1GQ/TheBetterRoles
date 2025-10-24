using BepInEx.Unity.IL2CPP.Utils;
using Il2CppInterop.Runtime.Attributes;
using System.Collections;
using System.Text.Json;
using TheBetterRoles.Data;
using TheBetterRoles.Items.Interfaces;
using TheBetterRoles.Network.Configs;
using UnityEngine;
using UnityEngine.Networking;

namespace TheBetterRoles.Network.Loaders;

/// <summary>
/// The UserDataLoader class is responsible for asynchronously fetching user and banned user data 
/// from an online repository. It retrieves data in JSON format, parses it, and updates the 
/// appropriate collections within the game. The class ensures proper error handling, retries in 
/// case of connectivity issues, and prevents duplicate fetches.
/// </summary>
internal class UserDataLoader : MonoBehaviour, IGithubLoader
{
    public string RepositoryUrl => "https://raw.githubusercontent.com/D1GQ/TBR_Data/main";
    private const string userDataFileName = "userData.json";
    private const string bacDataFileName = "bacData.json";
    private bool hasErrored;

    /// <summary>
    /// Coroutine to fetch user data from the remote repository.
    /// Handles connectivity checks, downloads the user data file, and deserializes its content.
    /// </summary>
    [HideFromIl2Cpp]
    internal IEnumerator CoFetchUserData()
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

        NetworkLogger.Log($"Downloading UserData items");
        GithubAPI.DownloadingContentQueue.Add(true);
        string callBack = "";
        yield return GitHubFile.CoDownloadManifest(RepositoryUrl, userDataFileName, (string text) =>
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
            var users = JsonSerializer.Deserialize<List<UserData>>(callBack, options);
            NetworkLogger.Log($"Loaded {users.Count} UserData items");
            if (users != null)
            {
                foreach (var user in users)
                {
                    ReadOnlyManager.AllUsers.Add(user);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error parsing JSON: {ex.Message}");
            NetworkLogger.Error($"Error parsing JSON: {ex.Message}");
            hasErrored = true;
            GithubAPI.DownloadingContentQueue.Add(false);
        }

        GithubAPI.DownloadingContentQueue.Add(false);
        this.StartCoroutine(CoFetchBACData());
    }

    /// <summary>
    /// Coroutine to fetch banned account data (BAC) from the remote repository.
    /// Handles connectivity checks, downloads the bac data file, and deserializes its content.
    /// </summary>
    [HideFromIl2Cpp]
    private IEnumerator CoFetchBACData()
    {
        int count = 0;
        float hang = 0;
        while (!GithubAPI.IsInternetAvailable())
        {
            count++;
            if (count >= 17) yield break;
            if (hang < 30f) hang += 2.5f;
            yield return new WaitForSeconds(hang);
        }

        NetworkLogger.Log($"Downloading BacData items");
        GithubAPI.DownloadingContentQueue.Add(true);
        var www = new UnityWebRequest($"{RepositoryUrl}/{bacDataFileName}", UnityWebRequest.kHttpVerbGET)
        {
            downloadHandler = new DownloadHandlerBuffer()
        };
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Logger.Error($"Error downloading {bacDataFileName}: {www.error}");
            NetworkLogger.Error($"Error downloading {bacDataFileName}: {www.error}");
            hasErrored = true;
            yield break;
        }

        var options = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true
        };

        try
        {
            var bannedUsers = JsonSerializer.Deserialize<List<BannedUserData>>(www.downloadHandler.text, options);
            NetworkLogger.Log($"Loaded {bannedUsers.Count} BacData items");
            if (bannedUsers != null)
            {
                foreach (var user in bannedUsers)
                {
                    ReadOnlyManager.AllBannedUsers.Add(user);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error parsing JSON: {ex.Message}");
            NetworkLogger.Error($"Error parsing JSON: {ex.Message}");
            hasErrored = true;
            GithubAPI.DownloadingContentQueue.Add(false);
        }

        GithubAPI.SetConnectedAPI(www, hasErrored);
        www.Dispose();

        GithubAPI.DownloadingContentQueue.Add(false);
        Destroy(this);
    }
}