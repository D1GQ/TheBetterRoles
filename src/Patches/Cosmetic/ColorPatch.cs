using AmongUs.Data;
using AmongUs.Data.Legacy;
using HarmonyLib;
using TheBetterRoles.Modules;
using UnityEngine;

namespace TheBetterRoles.Patches.Cosmetic;

[HarmonyPatch]
internal class ColorPatch
{
    [HarmonyPatch(typeof(Palette), nameof(Palette.GetColorName))]
    private class ColorStringPatch
    {
        internal static bool Prefix(int colorId, ref string __result)
        {
            if (colorId < Palette.ColorNames.Length)
            {
                __result = Translator.GetString(Palette.ColorNames[colorId]);
                return false;
            }

            if (CustomColors.ColorStrings.TryGetValue(colorId, out var name))
            {
                __result = Translator.GetString($"Color.{name}");
            }
            else
            {
                __result = "???";
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(PlayerTab), nameof(PlayerTab.OnEnable))]
    private static class PlayerTabEnablePatch
    {
        [HarmonyPrefix]
        internal static bool OnEnable_Prefix(PlayerTab __instance)
        {
            __instance.PlayerPreview.gameObject.SetActive(true);
            if (__instance.HasLocalPlayer())
            {
                __instance.PlayerPreview.UpdateFromLocalPlayer(PlayerMaterial.MaskType.None);
            }
            else
            {
                __instance.PlayerPreview.UpdateFromDataManager(PlayerMaterial.MaskType.None);
            }


            float GetHue(Color32 color)
            {
                Color.RGBToHSV(color, out float h, out _, out _);
                return h;
            }
            float GetBrightness(Color32 color)
            {
                Color.RGBToHSV(color, out _, out _, out float v);
                return v;
            }

            var OrganizedColors = CustomColors.PlayerColors
                .Where(color => !color.Hide)
                .OrderBy(color => color.Animated ? 1 : 0)
                .ThenBy(color => GetHue(color.Color))
                .ThenBy(color => GetBrightness(color.Color))
                .ToList();

            int columns = 8; // Number of items per column
            float Spacing = 0.5f;
            float chipSize = 0.95f;

            List<ColorChip> chips = [];
            int totalItems = OrganizedColors.Count;

            int rows = Mathf.CeilToInt(totalItems / (float)columns);

            float xStart = -1.8f;

            for (int i = 0; i < totalItems; i++)
            {
                var playerColor = OrganizedColors[i];

                int columnIndex = i % columns;
                int rowIndex = i / columns;

                float num2 = xStart + columnIndex * Spacing;

                float num3 = __instance.YStart - (rowIndex * Spacing);

                ColorChip colorChip = UnityEngine.Object.Instantiate(__instance.ColorTabPrefab);
                colorChip.transform.SetParent(__instance.ColorTabArea);
                colorChip.transform.localPosition = new Vector3(num2, num3, -1f);

                if (ActiveInputManager.currentControlType == ActiveInputManager.InputType.Keyboard)
                {
                    colorChip.Button.OnMouseOver.AddListener((Action)(() =>
                    {
                        __instance.SelectColor(playerColor.ColorId);
                    }));
                    colorChip.Button.OnMouseOut.AddListener((Action)(() =>
                    {
                        __instance.SelectColor(DataManager.Player.Customization.Color);
                    }));
                    colorChip.Button.OnClick.AddListener((Action)(() =>
                    {
                        __instance.ClickEquip();
                    }));
                }
                else
                {
                    colorChip.Button.OnClick.AddListener((Action)(() =>
                    {
                        __instance.SelectColor(playerColor.ColorId);
                    }));
                }

                colorChip.Inner.SpriteColor = playerColor.Color;
                colorChip.Tag = playerColor.ColorId;
                colorChip.ProductId = playerColor.ColorId.ToString();
                colorChip.transform.localScale = new Vector3(chipSize, chipSize, 1f);
                chips.Add(colorChip);

                if (playerColor.Animated)
                {
                    ColorEffectBehavior.Add(colorChip.Button.GetComponent<SpriteRenderer>(), playerColor.ColorEffect, false);
                }
            }

            foreach (var chip in chips.OrderBy(c => int.Parse(c.ProductId)))
            {
                __instance.ColorChips.Add(chip);
            }

            __instance.currentColor = DataManager.Player.Customization.Color;

            return false;
        }
    }

    [HarmonyPatch(typeof(LegacySaveManager), nameof(LegacySaveManager.LoadPlayerPrefs))]
    private static class LoadPlayerPrefsPatch
    {
        internal static void Postfix([HarmonyArgument(0)] bool overrideLoad)
        {
            if (!LegacySaveManager.loaded || overrideLoad)
            {
                LegacySaveManager.colorConfig %= (uint)CustomColors.PlayerColors.Where(color => !color.Hide).Count();
            }
        }
    }

    [HarmonyPatch(typeof(PlayerMaterial))]
    [HarmonyPatch("SetColors")]
    [HarmonyPatch(new[] { typeof(int), typeof(Renderer) })]
    class PlayerMaterialColorPatch
    {
        [HarmonyPrefix]
        internal static bool RawSetColor_Prefix(int colorId, Renderer rend)
        {
            var color = CustomColors.GetPlayerColorById(colorId);
            if (color != null && color.Animated)
            {
                ColorEffectBehavior.Add(rend, color.ColorEffect);
                return false;
            }

            ColorEffectBehavior.TryRemove(rend);

            return true;
        }
    }
}
