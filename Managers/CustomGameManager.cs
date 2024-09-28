using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using InnerNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Sentry.MeasurementUnit;

namespace TheBetterRoles;

public enum EndGameReason
{
    Tasks,
    Sabotage,
    Outnumbered,
    CustomFromRole
}

public class CustomGameManager
{
    [HarmonyPatch(typeof(GameManager))]
    public class GameManagerPatch
    {
        [HarmonyPatch(nameof(GameManager.FixedUpdate))]
        [HarmonyPrefix]
        public static void FixedUpdate_Prefix(GameManager __instance)
        {
            __instance.ShouldCheckForGameEnd = false;

            CheckWinConditions();
        }
    }

    [HarmonyPatch(typeof(IntroCutscene))]
    public class IntroCutscenePatch
    {
        [HarmonyPatch(nameof(IntroCutscene.BeginCrewmate))]
        [HarmonyPrefix]
        private static bool BeginCrewmate_Prefix(IntroCutscene __instance)
        {
            List<PlayerControl> teamToShow = PlayerControl.LocalPlayer.Is(CustomRoleTeam.Crewmate) ? Main.AllPlayerControls.ToList() : Main.AllPlayerControls.Where(p => p.IsTeammate()).ToList();

            Begin(__instance, teamToShow);

            return false;
        }

        [HarmonyPatch(nameof(IntroCutscene.BeginImpostor))]
        [HarmonyPrefix]
        private static bool BeginImpostor_Prefix(IntroCutscene __instance)
        {
            List<PlayerControl> teamToShow = PlayerControl.LocalPlayer.Is(CustomRoleTeam.Crewmate) ? Main.AllPlayerControls.ToList() : Main.AllPlayerControls.Where(p => p.IsTeammate()).ToList();

            Begin(__instance, teamToShow);

            return false;
        }

        public static void Begin(IntroCutscene introCutscene, List<PlayerControl> teamToDisplay)
        {
            HudManager.Instance?.GameLoadAnimation?.gameObject?.SetActive(false);

            Vector3 position = introCutscene.BackgroundBar.transform.position;
            position.y -= 0.25f;
            introCutscene.BackgroundBar.transform.position = position;
            introCutscene.BackgroundBar.material.SetColor("_Color", PlayerControl.LocalPlayer.GetTeamColor());
            UnityEngine.Object.Destroy(introCutscene.TeamTitle.gameObject.GetComponent<TextTranslatorTMP>());
            introCutscene.TeamTitle.text = PlayerControl.LocalPlayer.GetRoleTeamName();
            introCutscene.TeamTitle.color = PlayerControl.LocalPlayer.GetRoleColor();
            var flag = PlayerControl.LocalPlayer.Is(CustomRoleTeam.Crewmate);
            introCutscene.ImpostorText.gameObject.SetActive(flag);
            if (flag)
            {
                int Imps = Main.AllPlayerControls.Where(p => p.Is(CustomRoleTeam.Impostor)).Count();
                UnityEngine.Object.Destroy(introCutscene.ImpostorText.gameObject.GetComponent<TextTranslatorTMP>());
                if (Imps == 1)
                {
                    introCutscene.ImpostorText.text = Translator.GetString(StringNames.NumImpostorsS);
                }
                else
                {
                    introCutscene.ImpostorText.text = string.Format(Translator.GetString(StringNames.NumImpostorsP), Imps);
                }
            }
            int maxDepth = Mathf.CeilToInt(7.5f);
            for (int i = 0; i < teamToDisplay.Count; i++)
            {
                PlayerControl playerControl = teamToDisplay[i];
                if (playerControl)
                {
                    NetworkedPlayerInfo data = playerControl.Data;
                    if (!(data == null))
                    {
                        PoolablePlayer poolablePlayer = introCutscene.CreatePlayer(i, maxDepth, data, false);
                        if (i == 0 && data.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                        {
                            introCutscene.ourCrewmate = poolablePlayer;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(nameof(IntroCutscene.ShowRole))]
        [HarmonyPostfix]
        private static void ShowRole_Postfix(IntroCutscene __instance)
        {
            try
            {
                _ = new LateTask(() =>
                {
                    SoundManager.Instance.StopAllSound();
                    Color RoleColor = PlayerControl.LocalPlayer.GetRoleColor();

                    __instance.RoleText.text = PlayerControl.LocalPlayer.GetRoleName();
                    __instance.RoleBlurbText.text = PlayerControl.LocalPlayer.GetRoleInfo();
                    __instance.ImpostorText.gameObject.SetActive(false);
                    __instance.TeamTitle.gameObject.SetActive(false);
                    __instance.BackgroundBar.material.color = RoleColor;
                    __instance.BackgroundBar.transform.SetLocalZ(-15);
                    __instance.transform.Find("BackgroundLayer").transform.SetLocalZ(-16);
                    __instance.YouAreText.color = RoleColor;
                    __instance.RoleText.color = RoleColor;
                    __instance.RoleBlurbText.color = RoleColor;
                }, 0.0025f, shoudLog: false);
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(EndGameManager))]
    public class EndGameManagerPatch
    {
        [HarmonyPatch(nameof(EndGameManager.SetEverythingUp))]
        [HarmonyPrefix]
        public static bool SetEverythingUp_Prefix(EndGameManager __instance)
        {
            List<NetworkedPlayerInfo> players = winners;
            var anyFlag = players.Any();
            NetworkedPlayerInfo? first = anyFlag ? players.First() : null;
            var role = first?.BetterData()?.RoleInfo?.RoleType ?? CustomRoles.Crewmate;
            var team = first?.BetterData()?.RoleInfo?.Role?.RoleTeam ?? CustomRoleTeam.None;
            var teamColor = anyFlag ? Utils.GetCustomRoleColor(first.BetterData().RoleInfo.RoleType) : Color.red;

            bool flag = winners.Any(data => data.Object == PlayerControl.LocalPlayer);

            if (!anyFlag)
            {
                SoundManager.Instance.PlaySound(__instance.DisconnectStinger, false);
            }
            else if (flag)
            {
                SoundManager.Instance.PlaySound(__instance.CrewStinger, false);
            }
            else
            {
                SoundManager.Instance.PlaySound(__instance.ImpostorStinger, false);
            }

            __instance.WinText.alignment = TMPro.TextAlignmentOptions.Right;
            __instance.WinText.color = teamColor;
            __instance.BackgroundBar.material.SetColor("_Color", teamColor);

            switch (team)
            {
                case CustomRoleTeam.Impostor:
                    __instance.WinText.text = "Impostors\n<size=75%>Win";
                    break;
                case CustomRoleTeam.Crewmate:
                    __instance.WinText.text = "Crewmates\n<size=75%>Win";
                    break;
                case CustomRoleTeam.Neutral:
                    if (players.Count <= 1)
                    {
                        __instance.WinText.text = $"{Utils.GetCustomRoleName(role)}\n<size=75%>Win";
                    }
                    else
                    {

                    }
                    break;
                default:
                    __instance.WinText.text = $"Error Occurred";
                    __instance.WinText.color = Color.red;
                    break;
            }

            int num = Mathf.CeilToInt(7.5f);
            for (int i = 0; i < players.Count; i++)
            {
                NetworkedPlayerInfo cachedPlayerData = players[i];
                int num2 = (i % 2 == 0) ? -1 : 1;
                int num3 = (i + 1) / 2;
                float num4 = num3 / num;
                float num5 = Mathf.Lerp(1f, 0.75f, num4);
                float num6 = ((i == 0) ? -8 : -1);
                PoolablePlayer poolablePlayer = UnityEngine.Object.Instantiate<PoolablePlayer>(__instance.PlayerPrefab, __instance.transform);
                poolablePlayer.transform.localPosition = new Vector3(1f * (float)num2 * (float)num3 * num5, FloatRange.SpreadToEdges(-1.125f, 0f, num3, num), num6 + (float)num3 * 0.01f) * 0.9f;
                float num7 = Mathf.Lerp(1f, 0.65f, num4) * 0.9f;
                Vector3 vector = new Vector3(num7, num7, 1f);
                poolablePlayer.transform.localScale = vector;
                if (cachedPlayerData.IsDead)
                {
                    poolablePlayer.SetBodyAsGhost();
                    poolablePlayer.SetDeadFlipX(i % 2 == 0);
                }
                else
                {
                    poolablePlayer.SetFlipX(i % 2 == 0);
                }
                poolablePlayer.UpdateFromPlayerOutfit(cachedPlayerData.DefaultOutfit, PlayerMaterial.MaskType.None, cachedPlayerData.IsDead, true, null, false);

                poolablePlayer.SetName(cachedPlayerData.PlayerName, vector.Inv(), Color.white, -15f);
                Vector3 namePosition = new Vector3(0f, -1.31f, -0.5f);
                poolablePlayer.SetNamePosition(namePosition);
                if (AprilFoolsMode.ShouldHorseAround() && GameOptionsManager.Instance.CurrentGameOptions.GameMode == GameModes.HideNSeek)
                {
                    poolablePlayer.SetBodyType(PlayerBodyTypes.Normal);
                    poolablePlayer.SetFlipX(false);
                }
            }

            winners.Clear();

            return false;
        }
    }

    // Ignore original endgame RPC
    [HarmonyPatch(typeof(InnerNetClient))]
    public class InnerNetClientPatch
    {
        [HarmonyPatch(nameof(InnerNetClient.HandleMessage))]
        [HarmonyPrefix]
        public static bool HandleMessage_Prefix(InnerNetClient __instance, [HarmonyArgument(0)] MessageReader reader)
        {
            byte tag = reader.Tag;
            return tag != 8;
        }
    }

    public static List<NetworkedPlayerInfo> winners = [];
    public static EndGameReason winReason;

    public static bool ShouldCheckConditions => !GameStates.IsFreePlay && !GameStates.IsExilling && GameStates.IsInGamePlay && GameManager.Instance.GameHasStarted;

    public static void EndGame(List<NetworkedPlayerInfo> Winners, EndGameReason reason)
    {
        winners = Winners;
        winReason = reason;

        PlayerControl.LocalPlayer.StartCoroutine(AmongUsClient.Instance.CoEndGame());
        AmongUsClient.Instance.GameState = InnerNet.InnerNetClient.GameStates.Ended;

        AmongUsClient.Instance.GameState = InnerNetClient.GameStates.Ended;
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

        if (GameStates.IsHost)
        {
            GameManager.Instance.RpcEndGame(GameOverReason.HumansDisconnect, false);
        }
    }

    public static void CheckWinConditions()
    {
        if (!GameStates.IsHost || !ShouldCheckConditions) return;

        if (CheckImpostorWin())
        {
            var Impostors = GameData.Instance.AllPlayers.ToArray().Where(d => d.BetterData().RoleInfo.RoleType == CustomRoles.Impostor).ToList();
            ActionRPCs.EndGameSync(Impostors, EndGameReason.Outnumbered);
        }
        else if (CheckSabotageWin())
        {
            var Impostors = GameData.Instance.AllPlayers.ToArray().Where(d => d.BetterData().RoleInfo.RoleType == CustomRoles.Impostor).ToList();
            ActionRPCs.EndGameSync(Impostors, EndGameReason.Sabotage);
        }
        else if (CheckCrewmateWin())
        {
            var Crewmates = GameData.Instance.AllPlayers.ToArray().Where(d => d.BetterData().RoleInfo.RoleType == CustomRoles.Crewmate).ToList();
            ActionRPCs.EndGameSync(Crewmates, EndGameReason.Tasks);
        }
        else if (CheckImposterAmount())
        {
            var Crewmates = GameData.Instance.AllPlayers.ToArray().Where(d => d.BetterData().RoleInfo.RoleType == CustomRoles.Crewmate).ToList();
            ActionRPCs.EndGameSync(Crewmates, EndGameReason.Outnumbered);
        }
        else if (CheckCustomWin() is PlayerControl player && player != null)
        {
            List<NetworkedPlayerInfo> players = [player.Data];
            foreach (var other in player.BetterData().RoleInfo.Role.RecruitedPlayers)
            {
                players.Add(other);
            }
            ActionRPCs.EndGameSync(players, EndGameReason.CustomFromRole);
        }
    }

    public static bool CheckImpostorWin()
    {
        var Impostors = Main.AllAlivePlayerControls.Where(pc => pc.Is(CustomRoleTeam.Impostor));
        var Players = Main.AllAlivePlayerControls.Where(pc => !pc.Is(CustomRoleTeam.Impostor));

        if (Impostors.Count() >= Players.Count())
        {
            return true;
        }

        return false;
    }

    public static bool CheckSabotageWin()
    {
        ISystemType systemType;
        if (ShipStatus.Instance.Systems.TryGetValue(SystemTypes.LifeSupp, out systemType))
        {
            LifeSuppSystemType lifeSuppSystemType = systemType.Cast<LifeSuppSystemType>();
            if (lifeSuppSystemType.Countdown < 0f)
            {
                lifeSuppSystemType.Countdown = 10000f;
                return true;
            }
        }
        foreach (ISystemType systemType2 in ShipStatus.Instance.Systems.Values)
        {
            ICriticalSabotage? criticalSabotage = systemType2.TryCast<ICriticalSabotage>();
            if (criticalSabotage != null && criticalSabotage.Countdown < 0f)
            {
                criticalSabotage.ClearSabotage();
                return true;
            }
        }

        return false;
    }

    public static bool CheckImposterAmount() => Main.AllAlivePlayerControls.Where(pc => pc.Is(CustomRoleTeam.Impostor)).Count() == 0;

    public static bool CheckCrewmateWin() => Main.AllPlayerControls
        .Where(pc => pc.Is(CustomRoleTeam.Crewmate) && pc.BetterData().RoleInfo.RoleAssigned && pc.BetterData().RoleInfo.Role.HasTask)
        .All(pc => pc.myTasks.ToArray().All(t => t.IsComplete));

    public static PlayerControl? CheckCustomWin()
    {
        foreach (var player in Main.AllPlayerControls)
        {
            if (player.BetterData().RoleInfo.RoleAssigned && player.BetterData().RoleInfo.Role.WinCondition())
            {
                return player;
            }
        }

        return null;
    }
}
