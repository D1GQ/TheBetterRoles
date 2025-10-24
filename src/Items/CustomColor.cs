using TheBetterRoles.Modules;
using UnityEngine;

namespace TheBetterRoles.Items;

internal class CustomColor
{
    private static int NextId = 0;

    internal CustomColor(string name, Color32 color, Color32 shadow, ColorEffect colorEffect = null)
    {
        ColorId = NextId;
        NextId++;
        Name = name;
        Color = color;
        Shadow = shadow;
        ColorEffect = colorEffect;
    }

    internal CustomColor(string name, Color32 color, Color32 shadow, ref int colorIdToSet, ColorEffect colorEffect = null)
    {
        ColorId = NextId;
        colorIdToSet = ColorId;
        NextId++;
        Name = name;
        Color = color;
        Shadow = shadow;
        ColorEffect = colorEffect;
    }

    internal int ColorId { get; }
    internal string Name { get; }
    private float Luminance => 0.2126f * Color.r + 0.7152f * Color.g + 0.0722f * Color.b;
    internal bool LighterColor => Luminance > 128;
    internal bool Hide { get; set; }
    internal bool Animated => ColorEffect != null;
    internal ColorEffect? ColorEffect { get; }
    internal Color32 Color { get; }
    internal Color32 Shadow { get; }
}