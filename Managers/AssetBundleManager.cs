using BepInEx.Unity.IL2CPP.Utils;
using System.Collections;
using System.Reflection;
using TheBetterRoles.Helpers;
using UnityEngine;

namespace TheBetterRoles.Managers;

public class AssetBundleManager : MonoBehaviour
{
    private class Result<T>
    {
        private T? _value;
        private Exception? _error;

        public void SetValue(T value) => (_value, _error) = (value, null);
        public void SetError(Exception error) => (_value, _error) = (default, error);
        public T? GetValue() => _error != null ? throw _error : _value;
    }

    private static AssetBundleManager? Instance;
    private static readonly Dictionary<string, AssetBundle> loadedBundles = new();
    private bool _errored;

    public static Shader? GlitchShader { get; private set; }
    public static bool Errored => Instance?._errored ?? false;

    public void Awake()
    {
        if (Instance != null) return;
        Instance = this;

        Logger.Log("Loading assets");
        this.StartCoroutine(InitializeAssets());
    }


    private IEnumerator InitializeAssets()
    {
        AssetBundle? shadersBundle;
        try
        {
            shadersBundle = LoadEmbeddedBundle("TheBetterRoles.Resources.AssetBundles.shaders");
            LogAssetNames(shadersBundle);
        }
        catch (Exception e)
        {
            HandleError(e);
            yield break;
        }

        var glitchResult = new Result<Shader>();
        yield return LoadAsset(shadersBundle, "GlitchShader", glitchResult);

        try
        {
            GlitchShader = glitchResult.GetValue();
            Logger.Log("Assets loaded successfully.");
        }
        catch (Exception e)
        {
            HandleError(e);
        }
    }

    private static void LogAssetNames(AssetBundle? assetBundle)
    {
        if (assetBundle == null)
        {
            Logger.Error("AssetBundle is null. Cannot log asset names.");
            return;
        }

        foreach (string name in assetBundle.GetAllAssetNames())
        {
            Logger.Log(name);
        }
    }

    private static AssetBundle? LoadEmbeddedBundle(string resourceName)
    {
        if (loadedBundles.TryGetValue(resourceName, out var bundle))
        {
            return bundle;
        }

        var assembly = Assembly.GetExecutingAssembly();
        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new FileNotFoundException($"Embedded asset bundle '{resourceName}' not found.");
        }

        byte[] buffer = new byte[stream.Length];
        stream.Read(buffer, 0, buffer.Length);

        bundle = AssetBundle.LoadFromMemory(buffer);
        if (bundle == null) throw new Exception($"Failed to load embedded asset bundle '{resourceName}'.");

        loadedBundles[resourceName] = bundle;
        Logger.Log($"Embedded asset bundle '{resourceName}' loaded successfully.");
        return bundle;
    }

    private static IEnumerator LoadAsset<T>(AssetBundle bundle, string objectName, Result<T> result) where T : UnityEngine.Object
    {
        AssetBundleRequest bundleReq;

        try
        {
            bundleReq = bundle.LoadAssetAsync<T>(objectName);
            if (bundleReq == null) throw new NullReferenceException();
        }
        catch (Exception e)
        {
            result.SetError(e);
            yield break;
        }

        while (!bundleReq.WasCollected && !bundleReq.isDone) yield return null;

        try
        {
            if (CastHelper.TryCast<T>(bundleReq.asset, out var item))
            {
                result.SetValue(item);
            }
        }
        catch (Exception e)
        {
            result.SetError(e);
        }
    }

    private void HandleError(Exception e)
    {
        _errored = true;
        Logger.Error($"Error loading assets: {e.Message}");
        this.StartCoroutine(ShowErrorDialog(e));
    }

    private static IEnumerator ShowErrorDialog(Exception e)
    {
        // Placeholder for custom error dialog implementation.
        Logger.Log("Displaying error dialog.");
        yield return null;
    }
}
