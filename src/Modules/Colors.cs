using UnityEngine;

namespace TheBetterRoles.Modules;

internal static class Colors
{
    // Convert a string to a colored string using a Unity Color object
    /// <summary>
    /// Converts the provided string into a colored string by wrapping it with a 
    /// <color> tag using the Unity Color object. The Unity Color is automatically 
    /// converted to its hexadecimal representation before being applied to the string.
    /// </summary>
    /// <param name="str">The string to be colored.</param>
    /// <param name="color">The Unity Color object representing the desired color.</param>
    /// <returns>A string wrapped in a color tag, displaying the string in the specified Unity color.</returns>
    internal static string ToColor(this string str, Color color) => $"<{Color32ToHex(color)}>{str}</color>";

    // Modify the alpha value of an existing color
    /// <summary>
    /// Returns a new color that preserves the RGB (Red, Green, Blue) values of the 
    /// original color, but applies a new alpha (opacity) value from the provided new color.
    /// This can be used to adjust the transparency of an existing color without changing its color components.
    /// </summary>
    /// <param name="oldColor">The original color whose RGB values will be preserved.</param>
    /// <param name="newColor">The color that provides the new alpha value.</param>
    /// <returns>A new color with the same RGB values as the original color, but with the new alpha value.</returns>
    internal static Color ToColor(this Color oldColor, Color newColor) => new(newColor.r, newColor.g, newColor.b, oldColor.a);

    // Set a new alpha value while keeping the original RGB values
    /// <summary>
    /// Creates and returns a new color with the same RGB values as the original color, 
    /// but with the specified alpha (transparency) value. This method is useful for 
    /// changing the transparency of a color while maintaining its original color components.
    /// </summary>
    /// <param name="oldColor">The original color whose RGB values will remain the same.</param>
    /// <param name="alpha">The new alpha (transparency) value to apply to the color.</param>
    /// <returns>A new color with the same RGB values as the original color, but with the specified alpha value.</returns>
    internal static Color ToAlpha(this Color oldColor, float alpha) => new(oldColor.r, oldColor.g, oldColor.b, alpha);

    /// <summary>
    /// Converts a Color32 object to a hexadecimal string representation.
    /// </summary>
    /// <param name="color">The Color32 object to convert.</param>
    /// <returns>A hexadecimal string representing the Color32 object.</returns>
    internal static string Color32ToHex(this Color32 color) => $"#{color.r:X2}{color.g:X2}{color.b:X2}{255:X2}";

    /// <summary>
    /// Converts a hexadecimal color string to a Color32 object.
    /// </summary>
    /// <param name="hex">The hexadecimal color string to convert.</param>
    /// <returns>A Color32 object representing the hexadecimal string.</returns>
    internal static Color HexToColor(this string hex)
    {
        if (hex.StartsWith("#"))
        {
            hex = hex.Substring(1);
        }

        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

        return new Color32(r, g, b, 255);
    }

    internal static Color32 ColorToColor32(this Color color)
    {
        return new Color32(
            (byte)(color.r * 255),
            (byte)(color.g * 255),
            (byte)(color.b * 255),
            (byte)(color.a * 255)
        );
    }

    internal static Color LerpColor(this Color[] colors, (float min, float max) lerpRange, float t, bool reverse = false)
    {
        float normalizedT = Mathf.InverseLerp(lerpRange.min, lerpRange.max, t);

        if (colors.Length == 1)
            return colors[0];

        if (reverse)
        {
            colors.Reverse();
        }

        if (normalizedT <= 0f)
            return colors[0];
        if (normalizedT >= 1f)
            return colors[^1];

        float segmentSize = 1f / (colors.Length - 1);
        int segmentIndex = (int)(normalizedT / segmentSize);
        float segmentT = (normalizedT - segmentIndex * segmentSize) / segmentSize;

        return Color.Lerp(colors[segmentIndex], colors[segmentIndex + 1], segmentT);
    }

    internal static readonly Color CrewmateBlue = new Color32(140, byte.MaxValue, byte.MaxValue, byte.MaxValue);
    internal static readonly Color ImpostorRed = new Color32(byte.MaxValue, 25, 25, byte.MaxValue);
    internal static readonly Color NeutralGray = new Color32(128, 128, 128, byte.MaxValue);
    internal static readonly Color ApocalypseGray = new Color32(64, 64, 64, byte.MaxValue);
}