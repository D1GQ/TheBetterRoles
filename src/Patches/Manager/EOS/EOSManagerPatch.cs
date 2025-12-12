using HarmonyLib;
using TheBetterRoles.Network.Configs;

namespace TheBetterRoles.Patches.Manager.EOS;

[HarmonyPatch(typeof(EOSManager))]
internal class EOSManagerPatch
{
    [HarmonyPatch(nameof(EOSManager.EndFinalPartsOfLoginFlow))]
    [HarmonyPostfix]
    private static void EndFinalPartsOfLoginFlow_Postfix()
    {
        UserDataExtensions.TrySetLocalData();
    }
}
