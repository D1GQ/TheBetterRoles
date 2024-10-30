using TheBetterRoles.Helpers;
using UnityEngine;
using System.Reflection;

namespace TheBetterRoles.Managers
{
    public static class AssetBundleManager
    {
        private static readonly Dictionary<string, AssetBundle> loadedBundles = [];

        private static AssetBundle? shaders;
        public static Shader? GlitchShader;

        public static async Task LoadAssets()
        {
            shaders = LoadEmbeddedBundle("TheBetterRoles.Resources.AssetBundles.shaders");
            LogAssetNames(shaders);

            // GlitchShader = await LoadAsync<Shader>(shaders, "assets/shaders/glitchshader.shader");
        }

        private static void LogAssetNames(AssetBundle? assetBundle)
        {
            if (assetBundle == null)
            {
                TBRLogger.Error("AssetBundle is null. Cannot log asset names.");
                return;
            }

            string[] assetNames = assetBundle.GetAllAssetNames();
            TBRLogger.Log("Assets in the bundle:");
            foreach (var name in assetNames)
            {
                TBRLogger.Log(name);
            }
        }

        private static AssetBundle? LoadEmbeddedBundle(string resourceName)
        {
            if (loadedBundles.ContainsKey(resourceName))
            {
                return loadedBundles[resourceName];
            }

            var assembly = Assembly.GetExecutingAssembly();
            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                TBRLogger.Error($"Embedded asset bundle '{resourceName}' not found in the assembly.");
                return null;
            }

            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);

            AssetBundle? bundle = AssetBundle.LoadFromMemory(buffer);
            if (bundle != null)
            {
                loadedBundles[resourceName] = bundle;
                TBRLogger.Log($"Embedded asset bundle '{resourceName}' loaded successfully.");
            }
            else
            {
                TBRLogger.Error($"Failed to load embedded asset bundle '{resourceName}'.");
            }
            return bundle;
        }

        public static async Task<T?> LoadAsync<T>(this AssetBundle assetBundle, string assetName) where T : UnityEngine.Object
        {
            if (assetBundle == null)
            {
                TBRLogger.Error("AssetBundle is null. Cannot load asset.");
                return null;
            }

            TBRLogger.Log($"Fetching '{assetName}'");
            AssetBundleRequest request = assetBundle.LoadAssetAsync<T>(assetName);

            while (!request.isDone)
            {
                await Task.Delay(500);
            }

            T? asset = request.asset as T;
            if (asset != null)
            {
                TBRLogger.Log($"Asset '{assetName}' loaded successfully.");
            }
            else
            {
                TBRLogger.Error($"Asset '{assetName}' not found or failed to load in the asset bundle.");
            }

            return asset;
        }
    }
}
