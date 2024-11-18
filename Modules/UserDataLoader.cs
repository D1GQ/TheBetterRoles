using BepInEx.Unity.IL2CPP.Utils;
using Il2CppInterop.Runtime.Attributes;
using System.Collections;
using System.Text.Json;
using TheBetterRoles.Items;
using UnityEngine;
using UnityEngine.Networking;

namespace TheBetterRoles.Modules
{
    public class UserDataLoader : MonoBehaviour
    {
        private bool isRunning;

        private const string RepositoryUrl = "https://raw.githubusercontent.com/D1GQ/TBR_Data/main";
        private const string DataFileName = "userData.json";

        public void Start()
        {
            FetchData();
        }

        public void FetchData()
        {
            if (isRunning) return;
            this.StartCoroutine(CoFetchData());
        }

        [HideFromIl2Cpp]
        private IEnumerator CoFetchData()
        {
            isRunning = true;

            Logger.Log($"Downloading UserData items");
            var www = new UnityWebRequest($"{RepositoryUrl}/{DataFileName}", UnityWebRequest.kHttpVerbGET)
            {
                downloadHandler = new DownloadHandlerBuffer()
            };
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Logger.Error($"Error downloading {DataFileName}: {www.error}");
                isRunning = false;
                Destroy(this);
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
                Logger.Log($"Loading {users.Count} UserData items");
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
        }
    }
}