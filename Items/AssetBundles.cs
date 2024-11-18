using Reactor.Utilities.Extensions;
using System.Reflection;
using TheBetterRoles.Modules;
using UnityEngine;

namespace TheBetterRoles.Items;

public class AssetBundles
{
    public static AssetBundle? ShadersBundle { get; private set; }
    public static Shader? GlitchShader { get; private set; }

    public static void LoadAssetBundles()
    {
        ShadersBundle = AssetBundle.LoadFromMemory(Assembly.GetCallingAssembly().GetManifestResourceStream("TheBetterRoles.Resources.AssetBundles.shaders").ReadFully());
        GlitchShader = ShadersBundle.LoadAsset<Shader>("GlitchShader")?.DontDestroy();
    }
}
