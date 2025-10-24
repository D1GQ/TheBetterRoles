using HarmonyLib;
using Innersloth.Assets;
using TheBetterRoles.Monos;

namespace TheBetterRoles.Patches.Cosmetic.Visors;

[HarmonyPatch(typeof(PlayerPhysics))]
internal static class VisorPlayerPhysicsPatche
{
    [HarmonyPatch(nameof(PlayerPhysics.HandleAnimation))]
    [HarmonyPostfix]
    private static void HandleAnimation_Postfix(PlayerPhysics __instance)
    {
        if (__instance?.myPlayer?.ExtendedPC() == null) return;

        var currentAnimation = __instance.Animations.Animator.GetCurrentAnimation();
        if (currentAnimation == __instance.Animations.group.ClimbUpAnim) return;
        if (currentAnimation == __instance.Animations.group.ClimbDownAnim) return;

        var visorParent = __instance.myPlayer.cosmetics.visor;
        if (visorParent == null || visorParent == null || __instance?.myPlayer?.Visible == false || __instance.myPlayer.ExtendedPC().CosmeticsActiveQueue == false) return;
        AddressableAsset<VisorViewData> addressableAsset = visorParent.viewAsset;
        VisorViewData viewData = addressableAsset != null ? addressableAsset.GetAsset() : null;
        if (viewData == null) return;

        if (viewData.LeftIdleFrame != null)
        {
            if (__instance.FlipX)
            {
                visorParent.Image.sprite = viewData.LeftIdleFrame;
            }
            else
            {
                visorParent.Image.sprite = viewData.IdleFrame;
            }
        }
    }
}