using System.Reflection;
using UnityEngine;

namespace TheBetterRoles.Modules;

public class ColorEffectBehavior : MonoBehaviour
{
    public static List<ColorEffectBehavior> ActiveEffects = [];

    public static readonly ColorEffect?[] allEffects = GetAllCustomRoleInstances();

    public static ColorEffect?[] GetAllCustomRoleInstances() => Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(t => t.IsSubclassOf(typeof(ColorEffect)) && !t.IsAbstract)
        .Select(t => (ColorEffect)Activator.CreateInstance(t))
        .OrderBy(color => color.EffectId)
        .ToArray();

    private static ColorEffectBehavior? CreateEffectById(Renderer rend, int colorId)
    {
        var effect = allEffects.FirstOrDefault(color => color.EffectId == colorId);
        if (effect != null)
        {
            var behavior = rend.gameObject.AddComponent<ColorEffectBehavior>();
            behavior.myEffect = effect;
            return behavior;
        }

        return null;
    }

    public Renderer Renderer;
    public int Id;
    public bool IsBodyMaterial;
    private ColorEffect? myEffect;

    public static bool UsedColor(Renderer rend, int colorId)
    {
        var eff = ActiveEffects.FirstOrDefault(e => e.Renderer == rend);
        if (eff != null)
        {
            return colorId == eff.Id;
        }
        return false;
    }

    public static void AddRend(Renderer rend, int colorId, bool isBodyMaterial = true)
    {
        if (UsedColor(rend, colorId))
        {
            return;
        }
        else
        {
            TryRemove(rend);
        }

        var effect = CreateEffectById(rend, colorId);
        effect.Renderer = rend;
        effect.Id = colorId;
        effect.IsBodyMaterial = isBodyMaterial;
        ActiveEffects.Add(effect);
    }

    public static void TryRemove(Renderer rend)
    {
        var eff = ActiveEffects.FirstOrDefault(e => e.Renderer == rend);
        if (eff != null)
        {
            eff.myEffect.UnsetSetEffect(eff.Renderer, eff.Id, eff.IsBodyMaterial);
            ActiveEffects.Remove(eff);
            Destroy(eff);
        }
    }

    public void Update()
    {
        if (Renderer == null)
        {
            Destroy(this);
            return;
        }
        if (!Renderer.enabled || !Renderer.gameObject.active)
        {
            return;
        }

        myEffect?.SetEffect(Renderer, Id, IsBodyMaterial);
    }
}