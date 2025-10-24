using Il2CppInterop.Runtime.Attributes;
using System.Collections;
using UnityEngine.Networking;

namespace TheBetterRoles.Network.Loaders;

/// <summary>
/// Class representing a file from GitHub (for custom hats).
/// </summary>
internal class GitHubFile
{
    internal string Name { get; set; }
    internal string Type { get; set; }
    internal string Download_Url { get; set; }

    /// <summary>
    /// Downloads an individual visor-related file from the remote repository and saves it locally.
    /// Handles errors and logs any failed downloads to avoid missing assets.
    /// </summary>
    [HideFromIl2Cpp]
    internal static IEnumerator CoDownloadFile(string fileUrl, string localFilePath, string fileName)
    {
        var www = new UnityWebRequest(fileUrl, UnityWebRequest.kHttpVerbGET)
        {
            downloadHandler = new DownloadHandlerBuffer()
        };

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Logger.Error($"Error downloading file '{fileName}' from URL '{fileUrl}': {www.error} (Response Code: {(int)www.responseCode})");
            NetworkLogger.Error($"Error downloading file '{fileName}' from URL '{fileUrl}': {www.error} (Response Code: {(int)www.responseCode})");
            yield break;
        }

        File.WriteAllBytes(localFilePath, www.downloadHandler.GetNativeData().ToArray());

        NetworkLogger.Log($"Saved file: {localFilePath}");
        www.Dispose();
    }

    /// <summary>
    /// Downloads an individual visor-related file from the remote repository and saves it locally.
    /// Handles errors and logs any failed downloads to avoid missing assets.
    /// </summary>
    [HideFromIl2Cpp]
    internal static IEnumerator CoDownloadManifest(string fileUrl, string fileName, Action<string> Callback)
    {
        var www = new UnityWebRequest($"{fileUrl}/{fileName}", UnityWebRequest.kHttpVerbGET)
        {
            downloadHandler = new DownloadHandlerBuffer()
        };
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Logger.Error($"Error downloading {fileUrl}/{fileName}: {www.error}");
            NetworkLogger.Error($"Error downloading {fileUrl}/{fileName}: {www.error}");
            yield break;
        }

        var response = www.downloadHandler.text;
        www.Dispose();
        Callback.Invoke(response);
    }
}
