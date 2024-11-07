using Reactor.Utilities.Extensions;
using System.Reflection;
using UnityEngine;

namespace TheBetterRoles.Items;

public class AssetBundles
{
    public static AssetBundle? ShadersBundle { get; private set; }
    public static Shader? GlitchShader { get; private set; }
    private static bool loaded;
    public static void LoadAssetBundles()
    {
        if (loaded) return;

        ShadersBundle = AssetBundle.LoadFromMemory(Assembly.GetCallingAssembly().GetManifestResourceStream("TheBetterRoles.Resources.AssetBundles.shaders").ReadFully());
        GlitchShader = ShadersBundle.LoadAsset<Shader>("glitchshader");

        loaded = true;
    }
}
