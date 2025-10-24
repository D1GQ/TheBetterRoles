using HarmonyLib;
using InnerNet;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Buttons;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;

namespace TheBetterRoles.Patches.Client.AUClient;

[HarmonyPatch(typeof(AmongUsClient))]
internal class AmongUsClientPatch
{
    [HarmonyPatch(nameof(AmongUsClient.Awake))]
    [HarmonyPostfix]
    private static void Awake_Postfix()
    {
        Main.LateLoad();
    }

    [HarmonyPatch(nameof(AmongUsClient.OnGameJoined))]
    [HarmonyPostfix]
    private static void OnGameJoined_Postfix()
    {
        Logger.Log($"Successfully joined {GameCode.IntToGameName(AmongUsClient.Instance.GameId)}", "OnGameJoinedPatch");
    }

    [HarmonyPatch(nameof(AmongUsClient.OnPlayerLeft))]
    [HarmonyPostfix]
    private static void OnPlayerLeft_Postfix()
    {
        if (GameState.IsMeeting)
        {
            PlayerVoteAreaButton.UpdateAllButtonStates();
        }

        Utils.DirtyAllNames();
    }

    [HarmonyPatch(nameof(AmongUsClient.CoStartGame))]
    [HarmonyPostfix]
    private static void CoStartGame_Postfix()
    {
        Logger.LogHeader($"Game Has Started - {Enum.GetName(typeof(MapNames), GameState.GetActiveMapId)}/{GameState.GetActiveMapId}", "GamePlayManager");
        CustomGameManager.GameStart();
    }
}