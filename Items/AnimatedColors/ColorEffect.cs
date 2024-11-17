using UnityEngine;

namespace TheBetterRoles.Modules;

public abstract class ColorEffect
{
    public abstract int EffectId { get; }
    public static Color MultiColorGradient(Color[] colors, float speed)
    {
        if (colors == null || colors.Length == 0)
        {
            colors = [
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

        float cycleTime = Time.time % totalTime;

        int currentIndex = Mathf.FloorToInt(cycleTime / speed);
        int nextIndex = (currentIndex + 1) % colors.Length;

        float blendFactor = (cycleTime % speed) / speed;

        return Color.Lerp(colors[currentIndex], colors[nextIndex], blendFactor);
    }

    public static Color Shadow(Color color)
    {
        return new Color(color.r - 0.3f, color.g - 0.3f, color.b - 0.3f);
    }

    public static Color Visor(Color color)
    {
        return new Color(color.r + 0.5f, color.g + 0.5f, color.b + 0.5f);
    }

    public abstract void SetEffect(Renderer rend, int colorId, bool isBodyMaterial);
}