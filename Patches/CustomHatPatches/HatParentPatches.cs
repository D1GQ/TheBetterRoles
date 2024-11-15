using HarmonyLib;
using PowerTools;
using TheBetterRoles.Managers;
using UnityEngine;

namespace TheBetterRoles.Patches;

[HarmonyPatch(typeof(HatParent))]
public static class HatParentPatches
{
    [HarmonyPatch(nameof(HatParent.SetHat), typeof(int))]
    [HarmonyPrefix]
    private static bool SetHatPrefix(HatParent __instance, int color)
    {
        if (!__instance.IsCached()) return true;

        __instance.PopulateFromViewData();
        __instance.SetMaterialColor(color);
        return false;
    }

    [HarmonyPatch(nameof(HatParent.UpdateMaterial))]
    [HarmonyPrefix]
    private static bool UpdateMaterialPrefix(HatParent __instance)
    {
        if (!__instance.TryGetCached(out var asset)) return true;

        SetMaterialForLayers(__instance, asset.MatchPlayerColor);

        var colorId = __instance.matProperties.ColorId;
        SetLayerColors(__instance, colorId);

        var maskType = __instance.matProperties.MaskType;
        SetLayerMaskInteraction(__instance, maskType);

        if (__instance.matProperties.MaskLayer > 0) return false;
        SetMaskLayerBasedOnLocalPlayer(__instance);

        return false;
    }

    [HarmonyPatch(nameof(HatParent.LateUpdate))]
    [HarmonyPrefix]
    private static bool LateUpdatePrefix(HatParent __instance)
    {
        if (!__instance.Parent || !__instance.Hat || !__instance.TryGetCached(out var hatViewData)) return false;

        UpdateLayerSpritesForLateUpdate(__instance, hatViewData);

        return false;
    }

    [HarmonyPatch(nameof(HatParent.SetFloorAnim))]
    [HarmonyPrefix]
    private static bool SetFloorAnimPrefix(HatParent __instance)
    {
        if (!__instance.TryGetCached(out var hatViewData)) return true;

        __instance.BackLayer.enabled = false;
        __instance.FrontLayer.enabled = true;
        __instance.FrontLayer.sprite = hatViewData.FloorImage;
        return false;
    }

    [HarmonyPatch(nameof(HatParent.SetIdleAnim))]
    [HarmonyPrefix]
    private static bool SetIdleAnimPrefix(HatParent __instance, int colorId)
    {
        if (!__instance.Hat || !__instance.IsCached()) return false;

        __instance.viewAsset = null;
        __instance.PopulateFromViewData();
        __instance.SetMaterialColor(colorId);
        return false;
    }

    [HarmonyPatch(nameof(HatParent.SetClimbAnim))]
    [HarmonyPrefix]
    private static bool SetClimbAnimPrefix(HatParent __instance)
    {
        if (!__instance.TryGetCached(out var hatViewData) || !__instance.options.ShowForClimb) return false;

        __instance.BackLayer.enabled = false;
        __instance.FrontLayer.enabled = true;
        __instance.FrontLayer.sprite = hatViewData.ClimbImage;
        return false;
    }

    [HarmonyPatch(nameof(HatParent.PopulateFromViewData))]
    [HarmonyPrefix]
    private static bool PopulateFromHatViewDataPrefix(HatParent __instance)
    {
        if (!__instance.TryGetCached(out var asset)) return true;

        __instance.UpdateMaterial();
        UpdateSpriteAnimNodeSync(__instance);

        SetLayerVisibility(__instance, asset);

        if (!__instance.HideHat()) return false;
        __instance.FrontLayer.enabled = false;
        __instance.BackLayer.enabled = false;
        return false;
    }

    private static void SetMaterialForLayers(HatParent __instance, bool matchPlayerColor)
    {
        var material = matchPlayerColor
            ? DestroyableSingleton<HatManager>.Instance.PlayerMaterial
            : DestroyableSingleton<HatManager>.Instance.DefaultShader;

        __instance.FrontLayer.sharedMaterial = material;
        if (__instance.BackLayer) __instance.BackLayer.sharedMaterial = material;
    }

    private static void SetLayerColors(HatParent __instance, int colorId)
    {
        PlayerMaterial.SetColors(colorId, __instance.FrontLayer);
        if (__instance.BackLayer) PlayerMaterial.SetColors(colorId, __instance.BackLayer);

        __instance.FrontLayer.material.SetInt(PlayerMaterial.MaskLayer, __instance.matProperties.MaskLayer);
        if (__instance.BackLayer) __instance.BackLayer.material.SetInt(PlayerMaterial.MaskLayer, __instance.matProperties.MaskLayer);
    }

    private static void SetLayerMaskInteraction(HatParent __instance, PlayerMaterial.MaskType maskType)
    {
        SpriteMaskInteraction interaction = maskType switch
        {
            PlayerMaterial.MaskType.ScrollingUI => SpriteMaskInteraction.VisibleInsideMask,
            PlayerMaterial.MaskType.Exile => SpriteMaskInteraction.VisibleOutsideMask,
            _ => SpriteMaskInteraction.None
        };

        if (__instance.FrontLayer) __instance.FrontLayer.maskInteraction = interaction;
        if (__instance.BackLayer) __instance.BackLayer.maskInteraction = interaction;
    }

    private static void SetMaskLayerBasedOnLocalPlayer(HatParent __instance)
    {
        PlayerMaterial.SetMaskLayerBasedOnLocalPlayer(__instance.FrontLayer, __instance.matProperties.IsLocalPlayer);
        if (__instance.BackLayer) PlayerMaterial.SetMaskLayerBasedOnLocalPlayer(__instance.BackLayer, __instance.matProperties.IsLocalPlayer);
    }

    private static void UpdateSpriteAnimNodeSync(HatParent __instance)
    {
        var spriteAnimNodeSync = __instance.SpriteSyncNode ?? __instance.GetComponent<SpriteAnimNodeSync>();
        if (spriteAnimNodeSync)
        {
            spriteAnimNodeSync.NodeId = __instance.Hat.NoBounce ? 1 : 0;
        }
    }

    private static void SetLayerVisibility(HatParent __instance, HatViewData asset)
    {
        if (__instance.Hat.InFront)
        {
            __instance.BackLayer.enabled = false;
            __instance.FrontLayer.enabled = true;
            __instance.FrontLayer.sprite = asset.MainImage;
        }
        else if (asset.BackImage)
        {
            __instance.BackLayer.enabled = true;
            __instance.FrontLayer.enabled = true;
            __instance.BackLayer.sprite = asset.BackImage;
            __instance.FrontLayer.sprite = asset.MainImage;
        }
        else
        {
            __instance.BackLayer.enabled = true;
            __instance.FrontLayer.enabled = false;
            __instance.FrontLayer.sprite = null;
            __instance.BackLayer.sprite = asset.MainImage;
        }
    }

    private static void UpdateLayerSpritesForLateUpdate(HatParent __instance, HatViewData hatViewData)
    {
        if (__instance.FrontLayer.sprite != hatViewData.ClimbImage && __instance.FrontLayer.sprite != hatViewData.FloorImage)
        {
            if ((__instance.Hat.InFront || hatViewData.BackImage) && hatViewData.LeftMainImage)
            {
                __instance.FrontLayer.sprite = __instance.Parent.flipX ? hatViewData.LeftMainImage : hatViewData.MainImage;
            }

            if (hatViewData.BackImage && hatViewData.LeftBackImage)
            {
                __instance.BackLayer.sprite = __instance.Parent.flipX ? hatViewData.LeftBackImage : hatViewData.BackImage;
            }
            else if (!hatViewData.BackImage && !__instance.Hat.InFront && hatViewData.LeftMainImage)
            {
                __instance.BackLayer.sprite = __instance.Parent.flipX ? hatViewData.LeftMainImage : hatViewData.MainImage;
            }
        }
        else if (__instance.FrontLayer.sprite == hatViewData.ClimbImage || __instance.FrontLayer.sprite == hatViewData.LeftClimbImage)
        {
            var spriteAnimNodeSync = __instance.SpriteSyncNode ?? __instance.GetComponent<SpriteAnimNodeSync>();
            if (spriteAnimNodeSync)
            {
                spriteAnimNodeSync.NodeId = 0;
            }
        }
    }
}