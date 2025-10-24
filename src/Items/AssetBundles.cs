using Reactor.Utilities.Extensions;
using System.Reflection;
using UnityEngine;

namespace TheBetterRoles.Items;

internal class AssetBundles
{
    private static AssetBundle? ShadersBundle { get; set; }
    internal static Shader? GlitchShader { get; private set; }
    internal static Shader? GrayscaleShader { get; private set; }

    internal static void LoadAssetBundles()
    {
        ShadersBundle = AssetBundle.LoadFromMemory(Assembly.GetCallingAssembly().GetManifestResourceStream("TheBetterRoles.Resources.AssetBundles.shaders").ReadFully());
        GlitchShader = ShadersBundle.LoadAsset<Shader>("GlitchShader")?.DontDestroy();
        GrayscaleShader = ShadersBundle.LoadAsset<Shader>("GrayscaleShader")?.DontDestroy();
    }
}
