using HarmonyLib;
using UnityEngine;

namespace TheBetterRoles.Patches.Game.Ship;

[HarmonyPatch(typeof(ZiplineBehaviour))]
internal class ZiplineBehaviourPatch
{
    // Set hand color during camouflage comms
    [HarmonyPatch(nameof(ZiplineBehaviour.CoAnimatePlayerJumpingOnToZipline))]
    [HarmonyPostfix]
    private static void CoAnimatePlayerJumpingOnToZipline_Postfix(/*ZiplineBehaviour __instance,*/ [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(2)] HandZiplinePoolable hand)
    {
        PlayerMaterial.SetColors(player.cosmetics.bodyMatProperties.ColorId, hand.handRenderer);
        hand.transform.localScale = new Vector3(1f, 1f, 1f);
    }
}