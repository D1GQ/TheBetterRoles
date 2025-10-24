#if DEBUG_MULTIACCOUNTS
using AmongUs.Data;
using HarmonyLib;
#endif

namespace TheBetterRoles.Helpers;

// Use guest account on Account Startup. Only use when testing on vanilla servers for multiple client instances!
// DO NOT RELEASE A BUILD WITH THIS ENABLED!
internal class MultiAccounts
{
#if DEBUG_MULTIACCOUNTS
#warning "MultiAccounts mode is enabled in Debug_MultiAccounts mode! Ensure this is disabled before release."

    internal static void SetLevel()
    {
        DataManager.Player.stats.level = 50 - 1;
        DataManager.Player.Save();
    }

    [HarmonyPatch(typeof(EOSManager), nameof(EOSManager.StartInitialLoginFlow))]
    internal static class EOSManager_StartInitialLoginFlow
    {
        internal static bool Prefix(EOSManager __instance)
        {
            __instance.DeleteDeviceID(new Action(__instance.EndMergeGuestAccountFlow));

            __instance.StartTempAccountFlow();
            __instance.CloseStartupWaitScreen();

            return false;
        }
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.Update))]
    internal static class AmongUsClient_Update
    {
        internal static void Postfix()
        {
            SetLevel();
        }
    }

    [HarmonyPatch(typeof(InnerNet.InnerNetClient), nameof(InnerNet.InnerNetClient.JoinGame))]
    internal static class InnerNet_InnerNetClient_JoinGame
    {
        internal static void Prefix() => DataManager.Player.Account.LoginStatus = EOSManager.AccountLoginStatus.LoggedIn;
    }

    [HarmonyPatch(typeof(EOSManager), nameof(EOSManager.IsFreechatAllowed))]
    internal static class EOSManager_IsFreechatAllowed
    {
        internal static void Postfix(ref bool __result) => __result = true;
    }

    [HarmonyPatch(typeof(EOSManager), nameof(EOSManager.IsFriendsListAllowed))]
    internal static class EOSManager_IsFriendsListAllowed
    {
        internal static void Postfix(ref bool __result) => __result = true;
    }

    [HarmonyPatch(typeof(EOSManager), nameof(EOSManager.IsAllowedOnline))]
    internal static class EOSManager_IsAllowedOnline
    {
        internal static void Prefix(ref bool canOnline) => canOnline = true;
    }

    [HarmonyPatch(typeof(EOSManager), nameof(EOSManager.IsMinorOrWaiting))]
    internal static class EOSManager_IsMinorOrWaiting
    {
        internal static void Postfix(ref bool __result) => __result = false;
    }

    [HarmonyPatch(typeof(FullAccount), nameof(FullAccount.CanSetCustomName))]
    internal static class FullAccount_CanSetCustomName
    {
        internal static void Prefix(ref bool canSetName) => canSetName = true;
    }

    [HarmonyPatch(typeof(AccountManager), nameof(AccountManager.CanPlayOnline))]
    internal static class AccountManager_CanPlayOnline
    {
        internal static void Postfix(ref bool __result) => __result = true;
    }

#endif
}
