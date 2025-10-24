using HarmonyLib;
using TheBetterRoles.Modules;
using static OverlayKillAnimation;

namespace TheBetterRoles.Patches.UI;

internal class KillOverlayPatch
{
    [HarmonyPatch(typeof(_CoShow_d__18))]
    internal class _CoShow_d__18Patch
    {
        [HarmonyPatch(nameof(_CoShow_d__18.MoveNext))]
        [HarmonyPostfix]
        private static void CoShow_MoveNext_Postfix(bool __result)
        {
            if (GameState.IsMeeting)
            {
                MeetingHud.Instance.ButtonParent.gameObject.SetActive(!__result);
            }
        }
    }
}