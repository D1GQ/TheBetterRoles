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
using TMPro;
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

public class BetterCachedPlayerData
{
    public byte PlayerId;
    public string? PlayerName;
    public CustomRoles Role;
    public CustomRoleTeam RoleTeam;
    public List<NetworkedPlayerInfo.TaskInfo>? Tasks;
    public NetworkedPlayerInfo.PlayerOutfit? Outfit;
    public bool AmOwner;
    public bool Disconnected;
    public bool IsDead;
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

    [HarmonyPatch(typeof(HudManager))]
    public class HudManagerPatch
    {
        [HarmonyPatch(nameof(HudManager.ShowEmblem))]
        [HarmonyPrefix]
        private static void ShowEmblem_Prefix() => HudManager.Instance?.GameLoadAnimation?.gameObject?.SetActive(false);
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
            Vector3 position = introCutscene.BackgroundBar.transform.position;
            position.y -= 0.25f;
            introCutscene.BackgroundBar.transform.position = position;
            introCutscene.BackgroundBar.material.SetColor("_Color", PlayerControl.LocalPlayer.GetTeamColor());
            UnityEngine.Object.Destroy(introCutscene.TeamTitle.gameObject.GetComponent<TextTranslatorTMP>());
            introCutscene.TeamTitle.text = PlayerControl.LocalPlayer.GetRoleTeamName();
            introCutscene.TeamTitle.color = PlayerControl.LocalPlayer.GetTeamColor();
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
                        PoolablePlayer poolablePlayer = introCutscene.CreatePlayer(i, maxDepth, data, !PlayerControl.LocalPlayer.Is(CustomRoleTeam.Crewmate));
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

                    __instance.ourCrewmate.ToggleName(false);
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
                }, 0.0001f, shoudLog: false);
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
            SetupGameSummary(__instance);

            List<BetterCachedPlayerData> players = CachedPlayerData.ToArray().Where(CheckWinner).ToList();
            var anyFlag = players.Any();
            BetterCachedPlayerData? first = anyFlag ? players.First() : null;
            var role = first?.GetOldBetterData()?.RoleInfo?.RoleType ?? CustomRoles.Crewmate;
            var team = first?.GetOldBetterData()?.RoleInfo?.Role?.RoleTeam ?? CustomRoleTeam.None;
            var teamColor = Color.white;
            if (winReason != EndGameReason.CustomFromRole)
            {
                teamColor = anyFlag ? Utils.HexToColor32(Utils.GetCustomRoleTeamColor(first.GetOldBetterData().RoleInfo.Role.RoleTeam)) : Color.red;
            }
            else
            {
                teamColor = anyFlag ? Utils.GetCustomRoleColor(first.GetOldBetterData().RoleInfo.RoleType) : Color.red;
            }

            bool flag = players.Any(data => data.AmOwner);

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
                    __instance.WinText.text = $"{Translator.GetString(StringNames.ImpostorsCategory)}\n<size=75%>Win";
                    Logger.Log($"Game Has Ended: Team -> Impostors, Reason: {Enum.GetName(winReason)}, Players: {string.Join(" - ", players.Select(d => d.PlayerName))}");
                    break;
                case CustomRoleTeam.Crewmate:
                    __instance.WinText.text = $"{Translator.GetString(StringNames.Crewmates)}\n<size=75%>Win";
                    Logger.Log($"Game Has Ended: Team -> Crewmates, Reason: {Enum.GetName(winReason)}, Players: {string.Join(" - ", players.Select(d => d.PlayerName))}");
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
                BetterCachedPlayerData cachedPlayerData = players[i];
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
                poolablePlayer.UpdateFromPlayerOutfit(cachedPlayerData.Outfit, PlayerMaterial.MaskType.None, cachedPlayerData.IsDead, true, null, false);

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

    public static void SetupGameSummary(EndGameManager endGameManager)
    {
        return;

        try
        {
            List<BetterCachedPlayerData> players = CachedPlayerData.ToArray().Where(CheckWinner).ToList();
            var anyFlag = players.Any();
            BetterCachedPlayerData? first = anyFlag ? players.First() : null;
            var role = first?.GetOldBetterData()?.RoleInfo?.Role;
            if (role == null) return;

            Logger.LogHeader($"Game Has Ended - {Enum.GetName(typeof(MapNames), GameStates.GetActiveMapId)}/{GameStates.GetActiveMapId}", "GamePlayManager");

            Logger.LogHeader("Game Summary Start", "GameSummary");

            GameObject SummaryObj = UnityEngine.Object.Instantiate(endGameManager.WinText.gameObject, endGameManager.WinText.transform.parent.transform);
            SummaryObj.name = "SummaryObj (TMP)";
            SummaryObj.transform.SetSiblingIndex(0);
            Camera localCamera;
            if (DestroyableSingleton<HudManager>.InstanceExists)
            {
                localCamera = DestroyableSingleton<HudManager>.Instance.GetComponentInChildren<Camera>();
            }
            else
            {
                localCamera = Camera.main;
            }

            SummaryObj.transform.position = AspectPosition.ComputeWorldPosition(localCamera, AspectPosition.EdgeAlignments.LeftTop, new Vector3(1f, 0.2f, -5f));
            SummaryObj.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
            TextMeshPro SummaryText = SummaryObj.GetComponent<TextMeshPro>();
            if (SummaryText != null)
            {
                SummaryText.autoSizeTextContainer = false;
                SummaryText.enableAutoSizing = false;
                SummaryText.lineSpacing = -25f;
                SummaryText.alignment = TextAlignmentOptions.TopLeft;
                SummaryText.color = Color.white;

                BetterCachedPlayerData[] playersData = CachedPlayerData
                    .ToArray()
                    .OrderBy(pd => pd.Disconnected)  // Disconnected players last
                    .ThenBy(pd => pd.IsDead)          // Dead players after live players
                    .ThenBy(pd => CheckWinner(pd) ? 0 : 1) // Winners first
                    .ThenBy(pd => pd.RoleTeam == CustomRoleTeam.Crewmate) // Crewmates first
                    .ThenBy(pd => pd.RoleTeam == CustomRoleTeam.Impostor) // Impostors next
                    .ThenBy(pd => pd.RoleTeam == CustomRoleTeam.Neutral) // Neutral last
                    .ToArray();


                string winTeam = role.RoleName;
                switch (role.RoleTeam)
                {
                    case CustomRoleTeam.Impostor:
                        winTeam = Translator.GetString(StringNames.ImpostorsCategory);
                        break;
                    case CustomRoleTeam.Crewmate:
                        winTeam = Translator.GetString(StringNames.Crewmates);
                        break;
                }
                string winColor = role.RoleColor;
                string winTag;

                switch (winReason)
                {
                    case EndGameReason.Tasks:
                        winTag = Translator.GetString("Game.Summary.Result.TasksCompletion");
                        break;
                    case EndGameReason.Sabotage:
                        winTag = Translator.GetString("Game.Summary.Result.Sabotage");
                        break;
                    case EndGameReason.Outnumbered:
                        winTag = Translator.GetString("Game.Summary.Result.Outnumbered");
                        break;
                    case EndGameReason.CustomFromRole:
                        winTag = Translator.GetString("Game.Summary.Result.RoleCondition");
                        break;
                    default:
                        winTag = "Unknown";
                        break;
                }

                Logger.Log($"{winTeam}: {winTag}", "GameSummary");

                string SummaryHeader = $"<align=\"center\"><size=150%>   {Translator.GetString("GameSummary")}</size></align>";
                SummaryHeader += $"\n\n<size=90%><color={winColor}>{winTeam} {Translator.GetString("Game.Summary.Won")}</color></size>" +
                    $"\n<size=60%>\n{Translator.GetString("Game.Summary.By")} {winTag}</size>";

                StringBuilder sb = new StringBuilder();

                foreach (var data in playersData)
                {
                    var name = $"<color={Utils.Color32ToHex(Palette.PlayerColors[data.Outfit.ColorId])}>{data.GetOldBetterData().RealName}</color>";
                    string playerTheme(string text) => $"<color={Utils.GetCustomRoleTeamColor(data.GetOldBetterData().RoleInfo.Role.RoleTeam)}>{text}</color>";

                    var roleInfo = $"({Utils.GetCustomRoleNameAndColor(data.GetOldBetterData().RoleInfo.Role.RoleType)})";

                    if (data.GetOldBetterData().RoleInfo.Role.HasTask)
                    {
                        roleInfo += $" → {playerTheme($"{Translator.GetString("Tasks")}: {data.Tasks.ToArray().Where(task => task.Complete).Count()}/{data.Tasks.Count}")}";
                    }
                    if (data.GetOldBetterData().RoleInfo.Role.CanKill)
                    {
                        roleInfo += $" → {playerTheme($"{Translator.GetString("Kills")}: {data.GetOldBetterData().RoleInfo.Kills}")}";
                    }

                    string deathReason;
                    if (data.Disconnected)
                    {
                        deathReason = $"『<color=#838383><b>{Translator.GetString("DC")}</b></color>』";
                    }
                    else if (!data.IsDead)
                    {
                        deathReason = $"『<color=#80ff00><b>{Translator.GetString("Alive")}</b></color>』";
                    }
                    else if (data.IsDead)
                    {
                        deathReason = $"『<color=#ff0600><b>{Translator.GetString("Dead")}</b></color>』";
                    }
                    else
                    {
                        deathReason = $"『<color=#838383<b>Unknown</b></color>』";
                    }

                    Logger.Log($"{name} {roleInfo} {deathReason}", "GameSummary");

                    sb.AppendLine($"- {name} {roleInfo} {deathReason}\n");
                }

                SummaryText.text = $"{SummaryHeader}\n\n<size=58%>{sb}</size>";
                Logger.LogHeader("Game Summary End", "GameSummary");
            }
        } catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }

    private static bool CheckWinner(BetterCachedPlayerData data) => data != null && winners.Contains(data.PlayerId);

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

    public static List<BetterCachedPlayerData> CachedPlayerData = [];
    public static List<byte> winners = [];
    public static EndGameReason winReason;
    public static CustomRoleTeam winTeam;

    public static bool GameHasEnded = false;
    public static bool ShouldCheckConditions => !GameStates.IsFreePlay && !GameStates.IsExilling && GameStates.IsInGamePlay && GameManager.Instance.GameHasStarted;

    public static void EndGame(List<byte> Winners, EndGameReason reason, CustomRoleTeam team)
    {
        // Set player data for endgame
        CachedPlayerData.Clear();
        foreach (var data in GameData.Instance.AllPlayers)
        {
            List<NetworkedPlayerInfo.TaskInfo> newTasks = [];
            foreach (var task in data.Tasks)
            {
                var newTask = new NetworkedPlayerInfo.TaskInfo()
                { 
                    Id = task.Id,
                    TypeId = task.TypeId,
                    Complete = task.Complete,
                };
                newTasks.Add(newTask);
            }
            var newData = new BetterCachedPlayerData()
            {
                AmOwner = data.Object == PlayerControl.LocalPlayer,
                PlayerName = data.PlayerName,
                Tasks = newTasks,
                PlayerId = data.PlayerId,
                Role = data.GetOldBetterData().RoleInfo.Role.RoleType,
                RoleTeam = data.GetOldBetterData().RoleInfo.Role.RoleTeam,
                IsDead = data.IsDead,
                Disconnected = data.Disconnected,
                Outfit = new NetworkedPlayerInfo.PlayerOutfit()
                {
                    PlayerName = data.PlayerName,
                    ColorId = data.DefaultOutfit.ColorId,
                    SkinId = data.DefaultOutfit.SkinId,
                    HatId = data.DefaultOutfit.HatId,
                    VisorId = data.DefaultOutfit.VisorId,
                    PetId = data.DefaultOutfit.PetId,
                    NamePlateId = data.DefaultOutfit.NamePlateId,
                },
            };
            CachedPlayerData.Add(newData);
        }

        winners.Clear();
        winners = Winners;
        winReason = reason;
        winTeam = team;

        PlayerControl.LocalPlayer.StartCoroutine(AmongUsClient.Instance.CoEndGame());
        AmongUsClient.Instance.GameState = InnerNetClient.GameStates.Ended;

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

        _ = new LateTask(() =>
        {
            GameHasEnded = false;
        }, 5f, shoudLog: false);
    }

    public static void CheckWinConditions()
    {
        if (!GameStates.IsHost || !ShouldCheckConditions || GameHasEnded) return;

        if (CheckCrewAndImpAmount())
        {
            var Impostors = GetPlayerIdsFromTeam(CustomRoleTeam.Impostor);
            Logger.Log("Ending Game As Host: Crew Outnumbered");
            ActionRPCs.EndGameSync(Impostors, EndGameReason.Outnumbered);
            GameHasEnded = true;
        }
        else if (CheckSabotageWin())
        {
            var Impostors = GetPlayerIdsFromTeam(CustomRoleTeam.Impostor);
            Logger.Log("Ending Game As Host: Critical Sabotage");
            ActionRPCs.EndGameSync(Impostors, EndGameReason.Sabotage);
            GameHasEnded = true;
        }
        else if (CheckCrewmateWin())
        {
            var Crewmates = GetPlayerIdsFromTeam(CustomRoleTeam.Crewmate);
            Logger.Log("Ending Game As Host: All Tasks");
            ActionRPCs.EndGameSync(Crewmates, EndGameReason.Tasks);
            GameHasEnded = true;
        }
        else if (CheckImposterAmount())
        {
            var Crewmates = GetPlayerIdsFromTeam(CustomRoleTeam.Crewmate);
            Logger.Log("Ending Game As Host: No Imps");
            ActionRPCs.EndGameSync(Crewmates, EndGameReason.Outnumbered);
            GameHasEnded = true;
        }
        else if (CheckCustomWin() is PlayerControl player && player != null)
        {
            Logger.Log($"Ending Game As Host: {player.Data.PlayerName} Role -> {player.GetRoleName()} Win Condition Met");
            List<byte> players = [player.Data.PlayerId];
            foreach (var other in player.BetterData().RoleInfo.Role.RecruitedPlayers)
            {
                players.Add(other.PlayerId);
            }
            ActionRPCs.EndGameSync(players, EndGameReason.CustomFromRole);
            GameHasEnded = true;
        }
    }

    public static List<NetworkedPlayerInfo> GetPlayersFromTeam(CustomRoleTeam team)
    {
        List<NetworkedPlayerInfo> players = [];
        foreach (var data in GameData.Instance.AllPlayers)
        {
            if (data?.BetterData()?.RoleInfo?.Role?.RoleTeam != team) continue;
            players.Add(data);
        }

        return players;
    }

    public static List<byte> GetPlayerIdsFromTeam(CustomRoleTeam team)
    {
        List<byte> players = [];
        foreach (var data in GameData.Instance.AllPlayers)
        {
            if (data?.BetterData()?.RoleInfo?.Role?.RoleTeam != team) continue;
            players.Add(data.PlayerId);
        }

        return players;
    }

    public static bool CheckCrewAndImpAmount()
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
