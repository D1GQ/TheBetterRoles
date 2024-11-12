using BepInEx.Unity.IL2CPP.Utils;
using Il2CppInterop.Runtime.Attributes;
using System.Collections;
using System.Text.Json;
using TheBetterRoles.Items;
using UnityEngine;
using UnityEngine.Networking;

namespace TheBetterRoles.Managers;

public class CustomHatManager : MonoBehaviour
{
    private bool isRunning;

    private const string RepositoryUrl = "https://raw.githubusercontent.com/D1GQ/TBR_Hats/main";
    private const string ManifestFileName = "manifest.json";
    private static readonly string HatsDirectory = BetterDataManager.filePathFolderHats;

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

        var www = new UnityWebRequest($"{RepositoryUrl}/{ManifestFileName}", UnityWebRequest.kHttpVerbGET)
        {
            downloadHandler = new DownloadHandlerBuffer()
        };
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Logger.Error($"Error downloading manifest: {www.error}");
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
            Logger.Error("Manifest deserialization failed or no 'Hats' key found.");
            isRunning = false;
            yield break;
        }

        if (!Directory.Exists(HatsDirectory)) Directory.CreateDirectory(HatsDirectory);

        List<string> toDownload = GenerateDownloadList(response["Hats"]);

        Logger.Log($"Downloading {toDownload.Count} hat folders.");

        foreach (var folderName in toDownload)
        {
            yield return CoDownloadHatFolder(folderName);
        }

        isRunning = false;
    }

    private List<string> GenerateDownloadList(IEnumerable<string> availableFolders)
    {
        var toDownload = new List<string>();

        foreach (var folderName in availableFolders)
        {
            var folderPath = Path.Combine(HatsDirectory, folderName);
            if (!Directory.Exists(folderPath))
            {
                toDownload.Add(folderName);
            }
        }

        return toDownload;
    }

    private IEnumerator CoDownloadHatFolder(string folderName)
    {
        string url = $"https://api.github.com/repos/D1GQ/TBR_Hats/contents/Hats/{folderName}";
        Logger.Log($"Fetching contents from: {url}");

        var www = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
        {
            downloadHandler = new DownloadHandlerBuffer()
        };

        www.SetRequestHeader("User-Agent", "Unity");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Logger.Error($"Error fetching folder contents for '{folderName}': {www.error}");
            yield break;
        }

        var response = JsonSerializer.Deserialize<List<GitHubFile>>(www.downloadHandler.text);
        www.Dispose();

        if (response == null || response.Count == 0)
        {
            Logger.Error($"No files found in folder '{folderName}' or failed to deserialize response.");
            yield break;
        }

        var folderPath = Path.Combine(HatsDirectory, folderName);
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        foreach (var file in response)
        {
            if (file.type == "file")
            {
                yield return CoDownloadFile(file.download_url, folderPath, file.name);
            }
            else if (file.type == "dir")
            {
                yield return CoDownloadHatFolder(Path.Combine(folderName, file.name));
            }
        }
    }

    private IEnumerator CoDownloadFile(string fileUrl, string folderPath, string fileName)
    {
        var www = new UnityWebRequest(fileUrl, UnityWebRequest.kHttpVerbGET)
        {
            downloadHandler = new DownloadHandlerBuffer()
        };
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Logger.Error($"Error downloading file '{fileName}': {www.error}");
            yield break;
        }

        var filePath = Path.Combine(folderPath, fileName);
        File.WriteAllBytes(filePath, www.downloadHandler.data);

        Logger.Log($"Saved file: {fileName}");
        www.Dispose();
    }

    public class GitHubFile
    {
        public string name { get; set; }
        public string type { get; set; }
        public string download_url { get; set; }
    }

    public static List<CustomHat> customHats { get; private set; } = [];
    private static List<string> Loaded = [];

    public static void LoadAll()
    {
        string path = BetterDataManager.filePathFolderHats;

        if (Directory.Exists(path))
        {
            foreach (var subDir in Directory.GetDirectories(path))
            {
                if (!Loaded.Contains(subDir))
                {
                    string configPath = Path.Combine(subDir, "config.json");

                    if (File.Exists(configPath))
                    {
                        var customHat = CustomHat.Serialize(configPath);
                        customHats.Add(customHat);
                        Loaded.Add(subDir);
                    }
                }
            }
        }
        else
        {
            Logger.Error($"Directory '{path}' does not exist.");
        }
    }
}
