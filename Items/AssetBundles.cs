using Reactor.Utilities.Extensions;
using System.Reflection;
using UnityEngine;

namespace TheBetterRoles.Items;

public class AssetBundles
{
    public static AssetBundle? MaterialsBundle { get; private set; }
    public static Material? GlitchMaterial { get; private set; }
    private static bool loaded;
    public static void LoadAssetBundles()
    {
        if (loaded) return;

        MaterialsBundle = AssetBundle.LoadFromMemory(Assembly.GetCallingAssembly().GetManifestResourceStream("TheBetterRoles.Resources.AssetBundles.materials").ReadFully());
        GlitchMaterial = MaterialsBundle.LoadAsset<Material>("glitchmaterial");

        loaded = true;
    }
}
