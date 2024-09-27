using HarmonyLib;

namespace TheBetterRoles.Patches;

[HarmonyPatch(typeof(StatsManager), nameof(StatsManager.BanMinutesLeft), MethodType.Getter)]
public static class DisconnectPenaltyPatch
{
    public static bool Prefix(StatsManager __instance, ref int __result)
    {
        if (__instance.BanPoints != 0f)
        {
            __instance.BanPoints = 0f;
            __result = 0;
            return false;
        }
        return true;
    }
}
