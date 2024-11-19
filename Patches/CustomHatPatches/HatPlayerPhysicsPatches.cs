using HarmonyLib;
using TheBetterRoles.Managers;

namespace TheBetterRoles.Patches;

[HarmonyPatch(typeof(PlayerPhysics))]
internal static class HatPlayerPhysicsPatches
{
    [HarmonyPatch(nameof(PlayerPhysics.HandleAnimation))]
    [HarmonyPostfix]
    private static void HandleAnimationPostfix(PlayerPhysics __instance)
    {
        var currentAnimation = __instance.Animations.Animator.GetCurrentAnimation();
        if (currentAnimation == __instance.Animations.group.ClimbUpAnim) return;
        if (currentAnimation == __instance.Animations.group.ClimbDownAnim) return;
        var hatParent = __instance.myPlayer.cosmetics.hat;
        if (hatParent == null || hatParent == null || __instance?.myPlayer?.Visible == false || __instance?.myPlayer?.ExtendedData().CosmeticsActiveQueue == false) return;
        if (!hatParent.TryGetCached(out var viewData)) return;
        if (viewData.LeftMainImage != null)
        {
            if (__instance.FlipX)
            {
                (hatParent.FrontLayer.sprite) = viewData.LeftMainImage;
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
