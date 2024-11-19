using BepInEx.Unity.IL2CPP.Utils;
using Il2CppInterop.Runtime.Attributes;
using System.Collections;
using System.Text.Json;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using UnityEngine;
using UnityEngine.Networking;

namespace TheBetterRoles.Modules;

public class UserDataLoader : MonoBehaviour
{
    private bool isRunning;

    private const string RepositoryUrl = "https://raw.githubusercontent.com/D1GQ/TBR_Data/main";
    private const string userDataFileName = "userData.json";
    private const string bacDataFileName = "bacData.json";

    public void Start()
    {
        FetchData();
    }

    public void FetchData()
    {
        if (isRunning) return;
        this.StartCoroutine(CoFetchUserData());
    }

    [HideFromIl2Cpp]
    private IEnumerator CoFetchUserData()
    {
        isRunning = true;

        while (!Utils.IsInternetAvailable())
        {
            yield return new WaitForSeconds(5f);
        }

        Logger.Log($"Downloading UserData items");
        var www = new UnityWebRequest($"{RepositoryUrl}/{userDataFileName}", UnityWebRequest.kHttpVerbGET)
        {
            downloadHandler = new DownloadHandlerBuffer()
        };
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Logger.Error($"Error downloading {userDataFileName}: {www.error}");
            isRunning = false;
            yield break;
        }

        var options = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true
        };

        try
        {
            var users = JsonSerializer.Deserialize<List<UserData>>(www.downloadHandler.text, options);
            Logger.Log($"Loaded {users.Count} UserData items");
            if (users != null)
            {
                foreach (var user in users)
                {
                    UserData.AllUsers.Add(user);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error parsing JSON: {ex.Message}");
        }

        isRunning = false;
        www.Dispose();

        this.StartCoroutine(CoFetchBACData());
    }

    [HideFromIl2Cpp]
    private IEnumerator CoFetchBACData()
    {
        isRunning = true;

        while (!Utils.IsInternetAvailable())
        {
            yield return new WaitForSeconds(5f);
        }

        Logger.Log($"Downloading BacData items");
        var www = new UnityWebRequest($"{RepositoryUrl}/{bacDataFileName}", UnityWebRequest.kHttpVerbGET)
        {
            downloadHandler = new DownloadHandlerBuffer()
        };
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Logger.Error($"Error downloading {bacDataFileName}: {www.error}");
            isRunning = false;
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
            Logger.Log($"Loaded {bannedUsers.Count} BacData items");
            if (bannedUsers != null)
            {
                foreach (var user in bannedUsers)
                {
                    BannedUserData.AllBannedUsers.Add(user);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error parsing JSON: {ex.Message}");
        }

        isRunning = false;
        www.Dispose();
        Destroy(this);
    }
}