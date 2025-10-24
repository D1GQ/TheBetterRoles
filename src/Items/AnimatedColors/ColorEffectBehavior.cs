using TheBetterRoles.Helpers;
using UnityEngine;

namespace TheBetterRoles.Modules;

/// <summary>
/// MonoBehaviour for custom color effects.
/// </summary>
internal class ColorEffectBehavior : MonoBehaviour
{
    private static Dictionary<Renderer, ColorEffectBehavior> activeEffects = [];
    private Renderer renderer;
    private ColorEffect? myEffect;
    private bool bodyMaterial;

    internal static void Add(Renderer rend, ColorEffect effect, bool isBodyMaterial = true)
    {
        var effectBehavior = activeEffects.ContainsKey(rend) ? activeEffects[rend] : rend.gameObject.AddComponent<ColorEffectBehavior>();
        effectBehavior.renderer = rend;
        effect.ColorEffectBehavior = effectBehavior;
        effectBehavior.myEffect = effect;
        effectBehavior.bodyMaterial = isBodyMaterial;
        activeEffects[rend] = effectBehavior;
    }

    internal static void TryRemove(Renderer rend)
    {
        if (activeEffects.TryGetValue(rend, out var effectBehavior))
        {
            if (effectBehavior != null)
            {
                effectBehavior.DestroyMono();
                activeEffects.Remove(rend);
            }
        }
    }

    private void Update()
    {
        if (renderer == null)
        {
            Destroy(this);
            return;
        }
        if (!renderer.enabled || !renderer.gameObject.active)
        {
            return;
        }

        myEffect?.SetEffect(renderer, bodyMaterial);
    }
}