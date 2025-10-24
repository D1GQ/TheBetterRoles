using UnityEngine;

namespace TheBetterRoles.Modules;

/// <summary>
/// Base class for color effects
/// </summary>
internal abstract class ColorEffect
{
    internal ColorEffectBehavior? ColorEffectBehavior;
    internal abstract Color MainColor { get; }
    internal virtual Color ShadowColor => Shadow(MainColor);

    internal static Color MultiColorGradient(Color[] colors, float speed)
    {
        if (colors == null || colors.Length == 0)
        {
            colors =
            [
                Color.red,
                new Color(1f, 0.5f, 0f),
                Color.yellow,
                Color.green,
                Color.cyan,
                Color.blue,
                Color.magenta
            ];
        }

        if (colors.Length < 2)
            throw new ArgumentException("Provide at least two colors for the gradient.");

        float totalTime = colors.Length * speed;

        float cycleTime = Time.timeSinceLevelLoad % totalTime;

        int currentIndex = Mathf.FloorToInt(cycleTime / speed);
        int nextIndex = (currentIndex + 1) % colors.Length;

        float blendFactor = (cycleTime % speed) / speed;

        return Color.Lerp(colors[currentIndex], colors[nextIndex], blendFactor);
    }

    internal static Color Shadow(Color color)
    {
        return new Color(color.r - 0.3f, color.g - 0.3f, color.b - 0.3f);
    }

    internal static Color Visor(Color color)
    {
        return new Color(color.r + 0.5f, color.g + 0.5f, color.b + 0.5f);
    }

    internal abstract void SetEffect(Renderer rend, bool bodyMaterial);

    internal void SetDefaultEffects(Renderer rend, bool bodyMaterial)
    {
        if (bodyMaterial)
        {
            rend.material.SetColor(PlayerMaterial.BackColor, ShadowColor);
            rend.material.SetColor(PlayerMaterial.BodyColor, MainColor);
        }
        else
        {
            if (rend is SpriteRenderer renderer)
            {
                renderer.color = MainColor;
            }
            else if (rend.material.HasProperty("_Color"))
            {
                rend.material.SetColor("_Color", MainColor);
            }
        }
    }

    internal static void SetVisor(Renderer rend)
    {
        if (rend.material.GetColor(PlayerMaterial.VisorColor) != Palette.VisorColor)
        {
            rend.material.SetColor(PlayerMaterial.VisorColor, Palette.VisorColor);
        }
    }

    internal virtual void UnsetSetEffect(Renderer rend, bool isBodyMaterial) { }
}