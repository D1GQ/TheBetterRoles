using HarmonyLib;
using Hazel;
using InnerNet;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;

namespace TheBetterRoles.Patches.Client;

[HarmonyPatch(typeof(InnerNetClient))]
internal class InnerNetClientPatch
{
    [HarmonyPatch(nameof(InnerNetClient.SendOrDisconnect))]
    [HarmonyPrefix]
    private static bool SendOrDisconnect_Prefix(InnerNetClient __instance, [HarmonyArgument(0)] MessageWriter msg)
    {
        if (__instance?.connection != null)
        {
            SendErrors sendErrors = __instance.connection.Send(msg);
            if (sendErrors != SendErrors.None && !GameState.IsFreePlay)
            {
                NetworkLogger.Warning("Failed to send message: " + sendErrors.ToString());
                __instance.EnqueueDisconnect(DisconnectReasons.Error, "Failed to send message: " + sendErrors.ToString());
            }
        }
        else
        {
            __instance.EnqueueDisconnect(DisconnectReasons.Custom, "InnerNetClient.connection is null");
        }

        return false;
    }

    [HarmonyPatch(nameof(InnerNetClient.StartRpcImmediately))]
    [HarmonyPostfix]
    private static void StartRpcImmediately_Postfix(uint targetNetId, byte callId, int targetClientId, ref MessageWriter __result)
    {
        if (callId == 255) return; // Skip reactor RPC call for manual logging

        try
        {
            var gameId = AmongUsClient.Instance?.GameId ?? 0;
            string sentTo = !GameState.IsLocalGame && !GameState.IsFreePlay
                ? $"{GameCode.IntToGameNameV2(gameId)}({gameId})"
                : "LocalServer";
            string logMessage = targetClientId < 0
                ? $"{Utils.PlayerFromNetId(targetNetId)?.Data?.PlayerName ?? "???"}({targetNetId}) (StartRpcImmediately) Sent To: {sentTo} - Called Id: {Enum.GetName(typeof(RpcCalls), callId)}({callId})"
                : $"{Utils.PlayerFromNetId(targetNetId)?.Data?.PlayerName ?? "???"}({targetNetId}) (StartRpcImmediately) Sent To: {sentTo} ~ For {Utils.PlayerFromClientId(targetClientId)}({targetClientId}) - Called Id: {Enum.GetName(typeof(RpcCalls), callId)}({callId})";

            NetworkLogger.LogSend(logMessage);
        }
        catch { }
    }

/*
    [HarmonyPatch(nameof(InnerNetClient.StartRpc))]
    [HarmonyPostfix]
    private static void StartRpc_Postfix(uint targetNetId, byte callId, ref MessageWriter __result)
    {
        if (callId == 255) return; // Skip reactor RPC call for manual logging

        try
        {
            var gameId = AmongUsClient.Instance?.GameId ?? 0;
            string sentTo = !GameState.IsLocalGame && !GameState.IsFreePlay
                ? $"{GameCode.IntToGameNameV2(gameId)}({gameId})"
                : "LocalServer";
            string logMessage = $"{Utils.PlayerFromNetId(targetNetId)?.Data?.PlayerName ?? "???"}({targetNetId}) (StartRpc) Sent To: {sentTo} - Called Id: {Enum.GetName(typeof(RpcCalls), callId)}({callId})";

            NetworkLogger.LogSend(logMessage);
        }
        catch { }
    }
*/

    [HarmonyPatch(nameof(InnerNetClient.CanBan))]
    [HarmonyPrefix]
    private static bool CanBan_Prefix(ref bool __result)
    {
        __result = GameState.IsHost;
        return false;
    }

    [HarmonyPatch(nameof(InnerNetClient.CanKick))]
    [HarmonyPrefix]
    private static bool CanKick_Prefix(ref bool __result)
    {
        __result = GameState.IsHost || (GameState.IsInGamePlay && (GameState.IsMeeting || GameState.IsExilling));
        return false;
    }
}
