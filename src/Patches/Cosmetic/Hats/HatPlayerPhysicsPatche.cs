using HarmonyLib;
using Innersloth.Assets;
using TheBetterRoles.Monos;

namespace TheBetterRoles.Patches.Cosmetic.Hats;

[HarmonyPatch(typeof(PlayerPhysics))]
internal static class HatPlayerPhysicsPatche
{
    [HarmonyPatch(nameof(PlayerPhysics.HandleAnimation))]
    [HarmonyPostfix]
    private static void HandleAnimation_Postfix(PlayerPhysics __instance)
    {
        if (__instance?.myPlayer?.ExtendedPC() == null) return;

        var currentAnimation = __instance.Animations.Animator.GetCurrentAnimation();
        if (currentAnimation == __instance.Animations.group.ClimbUpAnim) return;
        if (currentAnimation == __instance.Animations.group.ClimbDownAnim) return;

        var hatParent = __instance.myPlayer.cosmetics.hat;
        if (hatParent == null || hatParent == null || __instance?.myPlayer?.Visible == false || __instance.myPlayer.ExtendedPC().CosmeticsActiveQueue == false) return;
        AddressableAsset<HatViewData> addressableAsset = hatParent.viewAsset;
        HatViewData viewData = addressableAsset != null ? addressableAsset.GetAsset() : null;
        if (viewData == null) return;

        if (viewData.LeftMainImage != null)
        {
            if (__instance.FlipX)
            {
                hatParent.FrontLayer.sprite = viewData.LeftMainImage;
            }
            else
            {
                hatParent.FrontLayer.sprite = viewData.MainImage;
            }
        }

        if (viewData.LeftBackImage != null)
        {
            if (__instance.FlipX)
            {
                hatParent.BackLayer.sprite = viewData.LeftBackImage;
            }
            else
            {
                hatParent.BackLayer.sprite = viewData.BackImage;
            }
        }
    }
}