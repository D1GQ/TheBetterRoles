using BepInEx.Unity.IL2CPP.Utils;
using Il2CppInterop.Runtime.Attributes;
using Reactor.Utilities.Extensions;
using TheBetterRoles.Items;
using TheBetterRoles.Network.Loaders;
using UnityEngine;
using UnityEngine.Networking;

namespace TheBetterRoles.Network;

internal class GithubAPI : MonoBehaviour
{
    internal static BoolQueue DownloadingContentQueue = new();
    internal static GithubAPI? Instance { get; private set; }
    internal static bool HasConnectedAPI { get; private set; } = false;
    internal static bool FinishedData { get; private set; } = false;
    internal static bool Finished { get; private set; } = false;
    private static GameObject Obj { get; set; }

    private static bool hasTryConnect = false;
    internal static void Connect()
    {
        if (hasTryConnect) return;
        hasTryConnect = true;

        Obj = new GameObject("GithubAPI(TBR)");
        Obj.DontDestroy();
        Instance = Obj.AddComponent<GithubAPI>();
    }

    internal void Start()
    {
        if (!Main.ConnectGithubAPI.Value)
        {
            NetworkLogger.Warning("GithubAPI Disabled!");
            ConnectToAPIUsers();
            return;
        }

        ConnectToAPI();
    }

    [HideFromIl2Cpp]
    private void ConnectToAPI()
    {
        ConnectToAPIUsers();

        var newsLoader = Obj.AddComponent<NewsLoader>();
        this.StartCoroutine(newsLoader.CoFetchNewsData());

        var cosmeticConfigLoader = Obj.AddComponent<CosmeticConfigLoader>();
        this.StartCoroutine(cosmeticConfigLoader.CoFetchCosmeticConfigs());

        var customHatLoader = Obj.AddComponent<CustomHatLoader>();
        this.StartCoroutine(customHatLoader.CoFetchHats());

        var customVisorLoader = Obj.AddComponent<CustomVisorLoader>();
        this.StartCoroutine(customVisorLoader.CoFetchVisors());

        var customNamePlateLoader = Obj.AddComponent<CustomNamePlateLoader>();
        this.StartCoroutine(customNamePlateLoader.CoFetchNamePlates());

        var customSkinLoader = Obj.AddComponent<CustomSkinLoader>();
        this.StartCoroutine(customSkinLoader.CoFetchSkins());
    }

    [HideFromIl2Cpp]
    private void ConnectToAPIUsers()
    {
        var userDataLoader = Obj.AddComponent<UserDataLoader>();
        this.StartCoroutine(userDataLoader.CoFetchUserData());
    }

    internal static void SetConnectedAPI(UnityWebRequest www, bool hasErrored)
    {
        if (www.result == UnityWebRequest.Result.ConnectionError ||
            www.result == UnityWebRequest.Result.ProtocolError || hasErrored)
        {
            HasConnectedAPI = false;
        }
        else
        {
            HasConnectedAPI = true;
        }
    }

    internal static bool IsInternetAvailable()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
            return false;

        UnityWebRequest www = null;
        try
        {
            www = UnityWebRequest.Get("https://clients3.google.com/generate_204");
            www.SendWebRequest();
            while (!www.isDone) { }
            return www.result == UnityWebRequest.Result.Success && www.responseCode == 204;
        }
        catch
        {
            return false;
        }
        finally
        {
            www?.Dispose();
        }
    }
}
