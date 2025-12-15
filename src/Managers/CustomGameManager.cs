using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Hazel;
using InnerNet;
using Reactor.Networking.Rpc;
using TheBetterRoles.Data;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Modules;
using TheBetterRoles.Network;
using TheBetterRoles.Network.RPCs;
using TheBetterRoles.Roles;
using TheBetterRoles.Roles.Core.Interfaces;

namespace TheBetterRoles.Managers;

internal static class CustomGameManager
{
    internal static bool ShouldCheckWinConditions => !GameState.IsFreePlay && !GameState.IsExilling && !TBRGameSettings.NoGameEnd.GetBool();

    internal static void GameStart()
    {
        CatchedGameData.Instance.OnGameStart();
        CatchedGameData.Instance.CurrentGameMode.OnGameStart();
        CustomLoadingBarManager.ToggleLoadingBar(true);
        CustomLoadingBarManager.SetLoadingPercent(0f, "Loading");

        if (GameState.IsHost)
        {
            Rpc<RpcSyncAllSettings>.Instance.Send(new(), true);

            CustomRoleManager.GatherAvailableGhostRolesOnStart();
            foreach (var player in Main.AllPlayerControls)
            {
                if (player == null) continue;
                player.RemainingEmergencies = Main.CurrentOptions.GetInt(Int32OptionNames.NumEmergencyMeetings);
            }

            Logger.LogHeader("Game Settings Start", "Settings");
            foreach (var option in OptionItem.AllTBROptions)
            {
                Logger.Log($"{Utils.RemoveHtmlText(option.GetParentPath().Replace(" ", string.Empty))} -> {Utils.RemoveHtmlText(option.ValueAsString())}", "Settings");
            }
            Logger.LogHeader("Game Settings End", "Settings");
        }
    }

    internal static void EndGame(HashSet<byte> Winners, EndGameReason reason, RoleClassTeam team)
    {
        CatchedGameData.Instance.OnGameEnd();
        CatchedGameData.Instance.CatchedGameEndReason = reason;
        CatchedGameData.Instance.CatchedWinTeam = team;

        if (reason != EndGameReason.ByHost)
        {
            RoleListener.InvokeRoles<IRoleGameplayAction>(role => role.GameEnd());
        }

        foreach (var playerId in Winners)
        {
            Utils.PlayerDataFromPlayerId(playerId).AddWinner();
        }

        CatchedGameData.Instance.CurrentGameMode.OnGameEnd();

        AmongUsClient.Instance.StartCoroutine(AmongUsClient.Instance.CoEndGame());

        // AmongUsClient.Instance.GameState = InnerNetClient.GameStates.Ended;
        List<ClientData> obj2 = AmongUsClient.Instance.allClients.ToArray().ToList();
        lock (obj2)
        {
            AmongUsClient.Instance.allClients.Clear();
        }

        /*
        EndGameResult endGameResult = new EndGameResult(GameOverReason.HumansDisconnect, false);
        var obj = AmongUsClient.Instance.Dispatcher;
        lock (obj)
        {
            AmongUsClient.Instance.Dispatcher.Add((Action)(() =>
            {
                AmongUsClient.Instance.OnGameEnd(endGameResult);
            }));
        }
        */

        if (GameState.IsHost)
        {
            _ = new LateTask(() =>
            {
                GameManager.Instance.RpcEndGame(GameOverReason.CrewmateDisconnect, false);
            }, 1f, shouldLog: false);
        }

        CustomRoleManager.QueuedGhostRoles.Clear();
    }

    internal static bool HasWon(this PlayerControl player) => player.Data.HasWon();
    internal static bool HasWon(this NetworkedPlayerInfo data) => CatchedGameData.Instance?.CatchedWinners.Contains(data.PlayerId) == true || CatchedGameData.Instance?.CatchedSubWinners.Contains(data.PlayerId) == true;

    internal static void AddWinner(this PlayerControl player) => player.Data.AddWinner();
    internal static void AddWinner(this NetworkedPlayerInfo data)
    {
        if (CatchedGameData.Instance?.CatchedWinners.Contains(data.PlayerId) == false)
        {
            CatchedGameData.Instance?.CatchedWinners.Add(data.PlayerId);
            data.Object.InvokeRoles<IRoleGameplayAction>(role => role.OnWin());
            RoleListener.InvokeRoles<IRoleGameplayAction>(role => role.OnWinOther(data.Object));
        }
    }

    internal static void AddSubWinner(this PlayerControl player) => player.Data.AddSubWinner();
    internal static void AddSubWinner(this NetworkedPlayerInfo data)
    {
        if (CatchedGameData.Instance?.CatchedSubWinners.Contains(data.PlayerId) == false)
        {
            CatchedGameData.Instance?.CatchedSubWinners.Add(data.PlayerId);
            data.Object.InvokeRoles<IRoleGameplayAction>(role => role.OnWin());
            RoleListener.InvokeRoles<IRoleGameplayAction>(role => role.OnWinOther(data.Object));
        }
    }

    internal static bool CheckWinner(NetworkedPlayerInfo data) => CatchedGameData.Instance?.CatchedWinners.Contains(data.PlayerId) == true;

    [HarmonyPatch(typeof(GameManager))]
    internal class GameManagerPatch
    {
        [HarmonyPatch(nameof(GameManager.FixedUpdate))]
        [HarmonyPrefix]
        internal static void FixedUpdate_Prefix(GameManager __instance)
        {
            __instance.ShouldCheckForGameEnd = false;
        }

        [HarmonyPatch(nameof(GameManager.EndGame))]
        [HarmonyPrefix]
        internal static bool EndGame_Prefix() => false;
    }

    [HarmonyPatch(typeof(ShhhBehaviour))]
    internal class ShhhBehaviourPatch
    {
        [HarmonyPatch(nameof(ShhhBehaviour.CheckForInterrupt))]
        [HarmonyPrefix]
        private static bool ShowEmblem_Prefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(IntroCutscene))]
    internal class IntroCutscenePatch
    {
        [HarmonyPatch(nameof(IntroCutscene.OnDestroy))]
        [HarmonyPrefix]
        private static void OnDestroy_Prefix(/*IntroCutscene __instance*/)
        {
            RoleListener.InvokeRoles<IRoleGameplayAction>(role => role.IntroCutsceneEnd());
            Utils.DirtyAllNames();
        }

        [HarmonyPatch(nameof(IntroCutscene.CoBegin))]
        [HarmonyPostfix]
        private static void CoBegin_Postfix(IntroCutscene __instance, ref Il2CppSystem.Collections.IEnumerator __result)
        {
            __result = CatchedGameData.Instance?.CurrentGameMode.CoPlayIntro(__instance).WrapToIl2Cpp() ?? __result;
        }
    }

    [HarmonyPatch(typeof(EndGameManager))]
    internal class EndGameManagerPatch
    {
        [HarmonyPatch(nameof(EndGameManager.SetEverythingUp))]
        [HarmonyPrefix]
        internal static bool SetEverythingUp_Prefix(EndGameManager __instance)
        {
            CatchedGameData.Instance?.CurrentGameMode.SetUpOutro(__instance);

            return false;
        }
    }

    // Ignore original endgame RPC
    [HarmonyPatch(typeof(InnerNetClient))]
    internal class InnerNetClientPatch
    {
        [HarmonyPatch(nameof(InnerNetClient.HandleMessage))]
        [HarmonyPrefix]
        internal static bool HandleMessage_Prefix(InnerNetClient __instance, [HarmonyArgument(0)] MessageReader reader)
        {
            byte tag = reader.Tag;
            return tag != 8;
        }
    }

    [HarmonyPatch(typeof(InnerNetServer))]
    internal class InnerNetServerPatch
    {
        [HarmonyPatch(nameof(InnerNetServer.HandleMessage))]
        [HarmonyPrefix]
        internal static bool HandleMessage_Prefix(InnerNetServer __instance, [HarmonyArgument(0)] MessageReader reader)
        {
            byte tag = reader.Tag;
            return tag != 8;
        }
    }
}
