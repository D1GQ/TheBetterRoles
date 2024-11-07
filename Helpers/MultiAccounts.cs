using HarmonyLib;

namespace TheBetterRoles.Helpers;

// Use guest account on Account Startup. Only use when testing on vanilla servers for multiple client instances!
// DO NOT RELEASE A BUILD WITH THIS ENABLED!
public class MultiAccounts
{
#if DEBUG_MULTIACCOUNTS
#warning "MultiAccounts mode is enabled in Debug_MultiAccounts mode! Ensure this is disabled before release."

    [HarmonyPatch(typeof(EOSManager), nameof(EOSManager.StartInitialLoginFlow))]
    public static class EOSManager_StartInitialLoginFlow
    {
        public static bool Prefix(EOSManager __instance)
        {
            __instance.DeleteDeviceID(new Action(__instance.EndMergeGuestAccountFlow));

            __instance.StartTempAccountFlow();
            __instance.CloseStartupWaitScreen();

            return false;
        }
    }

    [HarmonyPatch(typeof(EOSManager), nameof(EOSManager.IsFreechatAllowed))]
    public static class EOSManager_IsFreechatAllowed
    {
        public static void Postfix(ref bool __result) => __result = true;
    }

    [HarmonyPatch(typeof(EOSManager), nameof(EOSManager.IsFriendsListAllowed))]
    public static class EOSManager_IsFriendsListAllowed
    {
        public static void Postfix(ref bool __result) => __result = true;
    }

    [HarmonyPatch(typeof(EOSManager), nameof(EOSManager.IsAllowedOnline))]
    public static class EOSManager_IsAllowedOnline
    {
        public static void Prefix(ref bool canOnline) => canOnline = true;
    }

    [HarmonyPatch(typeof(EOSManager), nameof(EOSManager.IsMinorOrWaiting))]
    public static class EOSManager_IsMinorOrWaiting
    {
        public static void Postfix(ref bool __result) => __result = false;
    }

#endif
}
