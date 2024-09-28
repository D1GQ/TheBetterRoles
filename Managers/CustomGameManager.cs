using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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

    [HarmonyPatch(typeof(EndGameManager))]
    public class EndGameManagerPatch
    {
        [HarmonyPatch(nameof(EndGameManager.SetEverythingUp))]
        [HarmonyPrefix]
        public static bool SetEverythingUp_Prefix(EndGameManager __instance)
        {
            List<NetworkedPlayerInfo> players = winners;
            var first = winners.First();
            var role = first.BetterData().RoleInfo.RoleType;
            var team = first.BetterData().RoleInfo.Role.RoleTeam;
            var teamColor = Utils.GetCustomRoleColor(first.BetterData().RoleInfo.RoleType);
            bool flag = winners.Any(data => data.Object == PlayerControl.LocalPlayer);

            if (flag)
            {
                SoundManager.Instance.PlaySound(__instance.CrewStinger, false);
            }
            else
            {
                SoundManager.Instance.PlaySound(__instance.ImpostorStinger, false);
            }

            switch (team)
            {
                case CustomRoleTeam.Impostor:
                    __instance.WinText.text = "Impostors\nWin";
                    break;
                case CustomRoleTeam.Crewmate:
                    __instance.WinText.text = "Crewmates\nWin";
                    break;
                case CustomRoleTeam.Neutral:
                    __instance.WinText.text = $"{Utils.GetCustomRoleName(role)}\nWin";
                    break;
            }

            __instance.WinText.alignment = TMPro.TextAlignmentOptions.Right;
            __instance.WinText.color = teamColor;
            __instance.BackgroundBar.material.SetColor("_Color", teamColor);

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

            return false;
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
