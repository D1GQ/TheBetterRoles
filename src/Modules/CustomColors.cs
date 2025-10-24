using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TheBetterRoles.Items;
using UnityEngine;

namespace TheBetterRoles.Modules;

internal class CustomColors
{
    // Colors with custom effects
    internal static readonly int RainbowId;
    internal static readonly int GalaxyId;
    internal static readonly int FireId;
    internal static readonly int WaterId;
    internal static readonly int GhostId;
    internal static readonly int CamouflageId;

    internal static readonly Dictionary<int, string> ColorStrings = [];

    internal static CustomColor? GetPlayerColorById(int colorId) => PlayerColors[colorId] ?? null;

    internal static readonly List<CustomColor> PlayerColors = [
        // AU Colors
        new("Red", new Color32(198, 17, 17, byte.MaxValue), new Color32(122, 8, 56, byte.MaxValue)),
        new("Blue", new Color32(19, 46, 210, byte.MaxValue), new Color32(9, 21, 142, byte.MaxValue)),
        new("Green", new Color32(17, 128, 45, byte.MaxValue), new Color32(10, 77, 46, byte.MaxValue)),
        new("Pink", new Color32(238, 84, 187, byte.MaxValue), new Color32(172, 43, 174, byte.MaxValue)),
        new("Orange", new Color32(240, 125, 13, byte.MaxValue), new Color32(180, 62, 21, byte.MaxValue)),
        new("Yellow", new Color32(246, 246, 87, byte.MaxValue), new Color32(195, 136, 34, byte.MaxValue)),
        new("Black", new Color32(63, 71, 78, byte.MaxValue), new Color32(30, 31, 38, byte.MaxValue)),
        new("White", new Color32(215, 225, 241, byte.MaxValue), new Color32(132, 149, 192, byte.MaxValue)),
        new("Purple", new Color32(107, 47, 188, byte.MaxValue), new Color32(59, 23, 124, byte.MaxValue)),
        new("Brown", new Color32(113, 73, 30, byte.MaxValue), new Color32(94, 38, 21, byte.MaxValue)),
        new("Cyan", new Color32(56, byte.MaxValue, 221, byte.MaxValue), new Color32(36, 169, 191, byte.MaxValue)),
        new("Lime", new Color32(80, 240, 57, byte.MaxValue), new Color32(21, 168, 66, byte.MaxValue)),
        new("Maroon", Palette.FromHex(6233390), Palette.FromHex(4263706)),
        new("Rose", Palette.FromHex(15515859), Palette.FromHex(14586547)),
        new("Banana", Palette.FromHex(15787944), Palette.FromHex(13810825)),
        new("Gray", Palette.FromHex(7701907), Palette.FromHex(4609636)),
        new("Tan", Palette.FromHex(9537655), Palette.FromHex(5325118)),
        new("Coral", Palette.FromHex(14115940), Palette.FromHex(11813730)),

        // Custom Colors
        new("Tamarind", new Color32(48, 28, 34, byte.MaxValue), new Color32(30, 11, 16, byte.MaxValue)),
        new("Army", new Color32(39, 45, 31, byte.MaxValue), new Color32(11, 30, 24, byte.MaxValue)),
        new("Olive", new Color32(154, 140, 61, byte.MaxValue), new Color32(104, 95, 40, byte.MaxValue)),
        new("Turquoise", new Color32(22, 132, 176, byte.MaxValue), new Color32(15, 89, 117, byte.MaxValue)),
        new("Mint", new Color32(111, 192, 156, byte.MaxValue), new Color32(65, 148, 111, byte.MaxValue)),
        new("Lavender", new Color32(173, 126, 201, byte.MaxValue), new Color32(131, 58, 203, byte.MaxValue)),
        new("Nougat", new Color32(160, 101, 56, byte.MaxValue), new Color32(115, 15, 78, byte.MaxValue)),
        new("Peach", new Color32(255, 164, 119, byte.MaxValue), new Color32(238, 128, 100, byte.MaxValue)),
        new("Wasabi", new Color32(112, 143, 46, byte.MaxValue), new Color32(72, 92, 29, byte.MaxValue)),
        new("HotPink", new Color32(255, 51, 102, byte.MaxValue), new Color32(232, 0, 58, byte.MaxValue)),
        new("Petrol", new Color32(0, 99, 105, byte.MaxValue), new Color32(0, 61, 54, byte.MaxValue)),
        new("Lemon", new Color32(0xDB, 0xFD, 0x2F, byte.MaxValue), new Color32(0x74, 0xE5, 0x10, byte.MaxValue)),
        new("SignalOrange", new Color32(0xF7, 0x44, 0x17, byte.MaxValue), new Color32(0x9B, 0x2E, 0x0F, byte.MaxValue)),
        new("Teal", new Color32(0x25, 0xB8, 0xBF, byte.MaxValue), new Color32(0x12, 0x89, 0x86, byte.MaxValue)),
        new("Blurple", new Color32(61, 44, 142, byte.MaxValue), new Color32(25, 14, 90, byte.MaxValue)),
        new("Sunrise", new Color32(0xFF, 0xCA, 0x19, byte.MaxValue), new Color32(0xDB, 0x44, 0x42, byte.MaxValue)),
        new("Ice", new Color32(0xA8, 0xDF, 0xFF, byte.MaxValue), new Color32(0x59, 0x9F, 0xC8, byte.MaxValue)),
        new("Fuchsia", new Color32(164, 17, 129, byte.MaxValue), new Color32(104, 3, 79, byte.MaxValue)),
        new("RoyalGreen", new Color32(9, 82, 33, byte.MaxValue), new Color32(0, 46, 8, byte.MaxValue)),
        new("Slime", new Color32(244, 255, 188, byte.MaxValue), new Color32(167, 239, 112, byte.MaxValue)),
        new("Navy", new Color32(9, 43, 119, byte.MaxValue), new Color32(0, 13, 56, byte.MaxValue)),
        new("Darkness", new Color32(36, 39, 40, byte.MaxValue), new Color32(10, 10, 10, byte.MaxValue)),
        new("Ocean", new Color32(55, 159, 218, byte.MaxValue), new Color32(62, 92, 158, byte.MaxValue)),
        new("Sundown", new Color32(252, 194, 100, byte.MaxValue), new Color32(197, 98, 54, byte.MaxValue)),
        new("Crimson", new Color32(220, 20, 60, byte.MaxValue), new Color32(139, 0, 0, byte.MaxValue)),
        new("Periwinkle", new Color32(204, 204, 255, byte.MaxValue), new Color32(136, 136, 204, byte.MaxValue)),
        new("Chartreuse", new Color32(127, 255, 0, byte.MaxValue), new Color32(85, 170, 0, byte.MaxValue)),
        new("Charcoal", new Color32(54, 69, 79, byte.MaxValue), new Color32(38, 50, 56, byte.MaxValue)),
        new("Magenta", new Color32(255, 0, 255, byte.MaxValue), new Color32(139, 0, 139, byte.MaxValue)),
        new("Rust", new Color32(183, 65, 14, byte.MaxValue), new Color32(136, 45, 9, byte.MaxValue)),
        new("SeafoamGreen", new Color32(128, 255, 180, byte.MaxValue), new Color32(78, 155, 123, byte.MaxValue)),
        new("Ochre", new Color32(204, 119, 34, byte.MaxValue), new Color32(148, 87, 24, byte.MaxValue)),
        new("Slate", new Color32(112, 128, 144, byte.MaxValue), new Color32(72, 84, 101, byte.MaxValue)),

        new("Rainbow", new Color32(103, 118, 255, byte.MaxValue), new Color32(103, 118, 255, byte.MaxValue), ref RainbowId, new RainbowColorEffect()),
        new("Galaxy", new Color32(228, 0, 255, byte.MaxValue), new Color32(136, 0, 255, byte.MaxValue), ref GalaxyId, new GalaxyColorEffect()),
        new("Fire", new Color32(255, 3, 0, byte.MaxValue), new Color32(255, 178, 0, byte.MaxValue), ref FireId, new FireColorEffect()),
        new("Water", new Color32(13, 112, 200, byte.MaxValue), new Color32(205, 232, 255, byte.MaxValue), ref WaterId, new WaterColorEffect()),
        new("Ghost", new Color32(255, 255, 255, byte.MaxValue), new Color32(255, 255, 255, byte.MaxValue), ref GhostId, new GhostColorEffect()),
        new("Camouflage", new Color32(140, 140, 140, byte.MaxValue), new Color32(140, 140, 140, byte.MaxValue), ref CamouflageId) { Hide = true }
    ];

    internal static void ReplaceColorPalette()
    {
        Palette.PlayerColors = new Il2CppStructArray<Color32>(PlayerColors.Count);
        Palette.ShadowColors = new Il2CppStructArray<Color32>(PlayerColors.Count);

        for (int i = 0; i < PlayerColors.Count; i++)
        {
            ColorStrings[i] = PlayerColors[i].Name;
            Palette.PlayerColors[i] = PlayerColors[i].Color;
            Palette.ShadowColors[i] = PlayerColors[i].Shadow;
        }
    }
}