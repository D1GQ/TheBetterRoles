using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using InnerNet;
using Reactor.Networking.Rpc;
using System.Text;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches;
using TheBetterRoles.Roles;
using TheBetterRoles.RPCs;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheBetterRoles.Managers;

public enum EndGameReason
{
    Tasks,
    Sabotage,
    Outnumbered,
    CustomFromRole,
    ByHost,
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

        [HarmonyPatch(nameof(GameManager.EndGame))]
        [HarmonyPrefix]
        public static bool EndGame_Prefix(GameManager __instance) => false;
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
        [HarmonyPatch(nameof(IntroCutscene.OnDestroy))]
        [HarmonyPrefix]
        private static void OnDestroy_Prefix(/*IntroCutscene __instance*/)
        {
            CustomRoleManager.RoleListenerOther(role => role.OnIntroCutsceneEnd());
            Utils.DirtyAllNames();
        }

        [HarmonyPatch(nameof(IntroCutscene.BeginCrewmate))]
        [HarmonyPrefix]
        private static bool BeginCrewmate_Prefix(IntroCutscene __instance)
        {
            List<PlayerControl> teamToShow = PlayerControl.LocalPlayer.Is(CustomRoleTeam.Crewmate) ? Main.AllPlayerControls.ToList() : Main.AllPlayerControls.Where(p => p.IsTeammate()).ToList();

            foreach (PlayerControl player in teamToShow)
            {
                if (CustomRoleBehavior.SubTeam.ContainsKey(player.Data.PlayerId))
                {
                    foreach (var other in CustomRoleBehavior.SubTeam[player.Data.PlayerId])
                    {
                        teamToShow.Add(Utils.PlayerFromPlayerId(other));
                    }
                }
            }

            teamToShow = teamToShow.OrderBy(player => player.IsLocalPlayer() ? 0 : 1).ToList();
            Begin(__instance, teamToShow);

            return false;
        }

        [HarmonyPatch(nameof(IntroCutscene.BeginImpostor))]
        [HarmonyPrefix]
        private static bool BeginImpostor_Prefix(IntroCutscene __instance)
        {
            List<PlayerControl> teamToShow = PlayerControl.LocalPlayer.Is(CustomRoleTeam.Crewmate) ? Main.AllPlayerControls.ToList() : Main.AllPlayerControls.Where(p => p.IsTeammate()).ToList();

            foreach (PlayerControl player in teamToShow)
            {
                if (CustomRoleBehavior.SubTeam.ContainsKey(player.Data.PlayerId))
                {
                    foreach (var other in CustomRoleBehavior.SubTeam[player.Data.PlayerId])
                    {
                        teamToShow.Add(Utils.PlayerFromPlayerId(other));
                    }
                }
            }

            teamToShow = teamToShow.OrderBy(player => player.IsLocalPlayer() ? 0 : 1).ToList();
            Begin(__instance, teamToShow);

            return false;
        }

        public static void Begin(IntroCutscene introCutscene, List<PlayerControl> teamToDisplay)
        {
            Vector3 position = introCutscene.BackgroundBar.transform.position;
            position.y -= 0.25f;
            introCutscene.BackgroundBar.transform.position = position;
            introCutscene.BackgroundBar.material.SetColor("_Color", PlayerControl.LocalPlayer.GetTeamColor());
            introCutscene.TeamTitle.DestroyTextTranslator();
            introCutscene.TeamTitle.text = PlayerControl.LocalPlayer.GetRoleTeamName();
            introCutscene.TeamTitle.color = PlayerControl.LocalPlayer.GetTeamColor();
            var flag = PlayerControl.LocalPlayer.Is(CustomRoleTeam.Crewmate) || PlayerControl.LocalPlayer.Is(CustomRoleTeam.Neutral);
            introCutscene.ImpostorText.gameObject.SetActive(flag);

            if (flag)
            {
                int Imps = Main.AllPlayerControls.Where(p => p.Is(CustomRoleTeam.Impostor)).Count();
                introCutscene.ImpostorText.DestroyTextTranslator();
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
                    SoundManager.Instance.PlaySound(DestroyableSingleton<RoleBehaviour>.Instance.IntroSound, false, 1f, null);
                    Color RoleColor = PlayerControl.LocalPlayer.GetRoleColor();

                    __instance.ourCrewmate.ToggleName(false);
                    __instance.RoleText.text = PlayerControl.LocalPlayer.GetRoleName();

                    var addonText = UnityEngine.Object.Instantiate(__instance.RoleBlurbText, __instance.RoleBlurbText.transform.parent);
                    if (addonText != null)
                    {
                        addonText.name = "Addons(TMP)";
                        addonText.color = Color.white;
                        addonText.text = "";
                        addonText.transform.position += new Vector3(0f, 2.8f, 0f);
                        bool first = true;
                        foreach (var addon in PlayerControl.LocalPlayer.BetterData().RoleInfo.Addons)
                        {
                            if (addon == null) return;

                            if (!first) addonText.text += " + ";
                            addonText.text += $"<color={addon.RoleColor}>{addon.RoleName}</color>";
                            first = false;
                        }
                    }

                    __instance.RoleBlurbText.text = PlayerControl.LocalPlayer.GetRoleInfo();
                    __instance.ImpostorText.gameObject.SetActive(false);
                    __instance.TeamTitle.gameObject.SetActive(false);
                    __instance.BackgroundBar.material.color = RoleColor;
                    __instance.BackgroundBar.transform.SetLocalZ(-15);
                    __instance.transform.Find("BackgroundLayer").transform.SetLocalZ(-15);
                    __instance.YouAreText.color = RoleColor;
                    __instance.RoleText.color = RoleColor;
                    __instance.RoleBlurbText.color = RoleColor;
                }, 0.0001f, shouldLog: false);
                HudManager.Instance?.MapButton?.gameObject?.SetActive(true);
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

            List<NetworkedPlayerInfo> players = playerData.Where(CheckWinner).ToList();
            var anyFlag = players.Any();
            NetworkedPlayerInfo? first = anyFlag ? players.First() : null;
            var role = first?.BetterData()?.RoleInfo?.RoleType ?? CustomRoles.Crewmate;
            var team = winTeam;
            Color teamColor = winTeam != CustomRoleTeam.Neutral ? Utils.HexToColor32(Utils.GetCustomRoleTeamColor(winTeam))
                : first.BetterData().RoleInfo.Role.RoleColor32;

            __instance.WinText.alignment = TextAlignmentOptions.Right;

            __instance.WinText.text = $"<color={Utils.Color32ToHex(teamColor)}>";

            switch (team)
            {
                case CustomRoleTeam.Impostor:
                    __instance.WinText.text += $"{Translator.GetString(StringNames.ImpostorsCategory)}\n<size=75%>";
                    Logger.Log($"Game Has Ended: Team -> Impostors, Reason: {Enum.GetName(winReason)}, Players: {string.Join(" - ", players.Select(d => d.PlayerName))}");
                    break;
                case CustomRoleTeam.Crewmate:
                    __instance.WinText.text += $"{Translator.GetString(StringNames.Crewmates)}\n<size=75%>";
                    Logger.Log($"Game Has Ended: Team -> Crewmates, Reason: {Enum.GetName(winReason)}, Players: {string.Join(" - ", players.Select(d => d.PlayerName))}");
                    break;
                case CustomRoleTeam.Neutral:
                    Logger.Log($"Game Has Ended: Team -> Neutral, Reason: {Enum.GetName(winReason)}, Players: {string.Join(" - ", players.Select(d => d.PlayerName))}");
                    __instance.WinText.text += $"{Utils.GetCustomRoleName(role)}\n<size=75%>";
                    break;
                case CustomRoleTeam.None:
                    if (winReason == EndGameReason.ByHost)
                    {
                        __instance.WinText.text = Translator.GetString("Game.Summary.Abandoned");
                        __instance.WinText.color = Color.gray;
                        teamColor = Color.gray;
                        Logger.Log($"Game Has Ended: By Host");
                    }
                    break;
                default:
                    Logger.Log($"Game Has Ended: Error");
                    __instance.WinText.text = Translator.GetString("Game.Summary.Error");
                    __instance.WinText.color = Color.red;
                    teamColor = Color.red;
                    break;
            }

            __instance.BackgroundBar.material.SetColor("_Color", teamColor);

            foreach (var playerIds in subWinners)
            {
                if (winners.Contains(playerIds)) continue;

                var player = Utils.PlayerDataFromPlayerId(playerIds);
                players.Add(player);
                __instance.WinText.text += $"<size=50%><color=#FFFFFF>+</color><color={player.BetterData().RoleInfo.Role.RoleColor}>" +
                    $"{player.BetterData().RoleInfo.Role.RoleName}</color></size>";
            }

            bool flag = players.Any(data => data.BetterData().IsSelf);

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

            __instance.WinText.text += $" {Translator.GetString("Game.Summary.Wins")}</color>";

            int num = Mathf.CeilToInt(7.5f);
            for (int i = 0; i < players.Count; i++)
            {
                NetworkedPlayerInfo cachedPlayerData = players[i];
                int num2 = i % 2 == 0 ? -1 : 1;
                int num3 = (i + 1) / 2;
                float num4 = num3 / num;
                float num5 = Mathf.Lerp(1f, 0.75f, num4);
                float num6 = i == 0 ? -8 : -1;
                PoolablePlayer poolablePlayer = UnityEngine.Object.Instantiate(__instance.PlayerPrefab, __instance.transform);
                poolablePlayer.transform.localPosition = new Vector3(1f * num2 * num3 * num5, FloatRange.SpreadToEdges(-1.125f, 0f, num3, num), num6 + num3 * 0.01f) * 0.9f;
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

    public static void SetupGameSummary(EndGameManager endGameManager)
    {
        try
        {
            List<NetworkedPlayerInfo> players = playerData;
            var anyFlag = players.Any();
            NetworkedPlayerInfo? first = anyFlag ? players.First() : null;
            var role = first?.BetterData()?.RoleInfo?.Role;
            if (role == null) return;

            Logger.LogHeader($"Game Has Ended - {Enum.GetName(typeof(MapNames), GameState.GetActiveMapId)}/{GameState.GetActiveMapId}", "GamePlayManager");

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

                NetworkedPlayerInfo[] playersData = playerData
                    .ToArray()
                    .OrderBy(pd => pd.Disconnected)  // Disconnected players last
                    .ThenBy(pd => pd.IsDead)          // Dead players after live players
                    .ThenBy(pd => CheckWinner(pd) ? 0 : 1) // Winners first
                    .ThenBy(pd => pd.BetterData().RoleInfo.Role.RoleTeam == CustomRoleTeam.Crewmate) // Crewmates first
                    .ThenBy(pd => pd.BetterData().RoleInfo.Role.RoleTeam == CustomRoleTeam.Impostor) // Impostors next
                    .ThenBy(pd => pd.BetterData().RoleInfo.Role.RoleTeam == CustomRoleTeam.Neutral) // Neutral last
                    .ToArray();


                string winteam = role.RoleName;
                string winColor = role.RoleColor;
                switch (winTeam)
                {
                    case CustomRoleTeam.Impostor:
                        winteam = Translator.GetString(StringNames.ImpostorsCategory);
                        winColor = Utils.GetCustomRoleTeamColor(CustomRoleTeam.Impostor);
                        break;
                    case CustomRoleTeam.Crewmate:
                        winteam = Translator.GetString(StringNames.Crewmates);
                        winColor = Utils.GetCustomRoleTeamColor(CustomRoleTeam.Crewmate);
                        break;
                    case CustomRoleTeam.Neutral:
                        winteam = Translator.GetString("Neutrals");
                        winColor = Utils.GetCustomRoleTeamColor(CustomRoleTeam.Neutral);
                        break;
                }
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

                Logger.Log($"{winteam}: {winTag}", "GameSummary");

                string SummaryHeader = $"<align=\"center\"><size=150%>   {Translator.GetString("GameSummary")}</size></align>";
                SummaryHeader += $"\n\n<size=90%><color={winColor}>{winteam} {Translator.GetString("Game.Summary.Won")}</color></size>" +
                    $"\n<size=60%>\n{Translator.GetString("Game.Summary.By")} {winTag}</size>";

                StringBuilder sb = new StringBuilder();

                foreach (var data in playersData)
                {
                    var name = $"<color={Utils.Color32ToHex(Palette.PlayerColors[data.DefaultOutfit.ColorId])}>{data.BetterData().RealName}</color>";
                    string playerTheme(string text) => $"<color={Utils.GetCustomRoleTeamColor(data.BetterData().RoleInfo.Role.RoleTeam)}>{text}</color>";

                    var roleInfo = $"({Utils.GetCustomRoleNameAndColor(data.BetterData().RoleInfo.Role.RoleType)})";

                    if (data.BetterData().RoleInfo.Role.HasTask)
                    {
                        roleInfo += $" → {playerTheme($"{Translator.GetString("Tasks")}: {data.Tasks.ToArray().Where(task => task.Complete).Count()}/{data.Tasks.Count}")}";
                    }
                    if (data.BetterData().RoleInfo.Role.CanKill)
                    {
                        roleInfo += $" → {playerTheme($"{Translator.GetString("Kills")}: {data.BetterData().RoleInfo.Kills}")}";
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
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }

    private static bool CheckWinner(NetworkedPlayerInfo data) => winners.Contains(data.PlayerId);

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

    [HarmonyPatch(typeof(InnerNetServer))]
    public class InnerNetServerPatch
    {
        [HarmonyPatch(nameof(InnerNetServer.HandleMessage))]
        [HarmonyPrefix]
        public static bool HandleMessage_Prefix(InnerNetServer __instance, [HarmonyArgument(0)] MessageReader reader)
        {
            byte tag = reader.Tag;
            return tag != 8;
        }
    }

    public static List<NetworkedPlayerInfo> playerData => UnityEngine.Object.FindObjectsOfType<NetworkedPlayerInfo>().ToList();
    public static List<byte> winners = [];
    public static List<byte> subWinners = [];
    public static EndGameReason winReason;
    public static CustomRoleTeam winTeam;

    public static bool GameHasEnded = false;
    public static bool ShouldCheckConditions => !GameState.IsFreePlay && !GameState.IsExilling && GameState.IsInGamePlay && GameManager.Instance.GameHasStarted && !BetterGameSettings.NoGameEnd.GetBool();

    public static void GameStart()
    {
        if (GameState.IsHost)
        {
            Rpc<RpcSyncAllSettings>.Instance.Send(new(null));
        }

        GameHasEnded = false;
        CustomRoleManager.availableGhostRoles.Clear();
        CustomRoleBehavior.SubTeam.Clear();
        foreach (var player in Main.AllPlayerControls)
        {
            if (player == null) continue;
            player.RemainingEmergencies = Main.CurrentOptions.GetInt(Int32OptionNames.NumEmergencyMeetings);
        }
    }

    public static void EndGame(List<byte> Winners, EndGameReason reason, CustomRoleTeam team)
    {
        List<byte> subwinners = [];
        CustomRoleManager.RoleListenerOther(role => role.OnGameEnd(ref subwinners));
        GetSubTeamWin(ref subwinners);

        // Set player data for endgame
        winners.Clear();
        subWinners.Clear();
        winners = Winners;
        subWinners = subwinners;
        winReason = reason;
        winTeam = team;

        foreach (var data in GameData.Instance.AllPlayers)
        {
            UnityEngine.Object.DontDestroyOnLoad(data.gameObject);
        }

        AmongUsClient.Instance.StartCoroutine(AmongUsClient.Instance.CoEndGame());

        _ = new LateTask(() =>
        {
            foreach (var data in GameData.Instance.AllPlayers)
            {
                SceneManager.MoveGameObjectToScene(data.gameObject, SceneManager.GetActiveScene());
            }
        }, 0.6f, shouldLog: false);

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
                GameManager.Instance.RpcEndGame(GameOverReason.HumansDisconnect, false);
            }, 1f, shouldLog: false);
        }
    }

    public static void CheckWinConditions()
    {
        if (!GameState.IsHost || !ShouldCheckConditions || GameHasEnded) return;

        CustomRoleTeam team = CustomRoleTeam.None;

        if (CheckSabotageWin())
        {
            team = CustomRoleTeam.Impostor;
            var Impostors = GetPlayerIdsFromTeam(team);
            Logger.Log("Ending Game As Host: Critical Sabotage");
            Rpc<RpcEndGame>.Instance.Send(PlayerControl.LocalPlayer, new(Impostors, EndGameReason.Sabotage, team));
            GameHasEnded = true;
        }
        else if (CheckCustomWin() is PlayerControl player && player != null)
        {
            team = CustomRoleTeam.Neutral;
            Logger.Log($"Ending Game As Host: {player.Data.PlayerName} Role -> {player.GetRoleName()} Win Condition Met");
            List<byte> players = [player.Data.PlayerId];
            Rpc<RpcEndGame>.Instance.Send(PlayerControl.LocalPlayer, new(players, EndGameReason.CustomFromRole, team));
            GameHasEnded = true;
        }
        else if (CheckPlayerAmount(ref team))
        {
            var players = GetPlayerIdsFromTeam(team);
            Logger.Log($"Ending Game As Host: {Utils.GetCustomRoleTeamName(team)} Outnumbered");
            Rpc<RpcEndGame>.Instance.Send(PlayerControl.LocalPlayer, new(players, EndGameReason.Outnumbered, team));
            GameHasEnded = true;
        }
    }

    public static List<byte> GetSubTeamWin(ref List<byte> Subplayers)
    {
        foreach (var playerId in Subplayers)
        {
            if (CustomRoleBehavior.SubTeam.ContainsKey(playerId))
            {
                foreach (var otherId in CustomRoleBehavior.SubTeam[playerId])
                {
                    Subplayers.Add(otherId);
                }
            }
        }

        return Subplayers;
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

    public static bool CheckPlayerAmount(ref CustomRoleTeam team)
    {
        var allPlayers = Main.AllAlivePlayerControls;
        var impostors = allPlayers.Where(pc => pc.Is(CustomRoleTeam.Impostor)).ToList();
        var killingPlayers = allPlayers.Where(pc => pc.Is(CustomRoleTeam.Neutral) && pc.RoleChecksAny(role => role.IsKillingRole)).ToList();

        if (impostors.Count == 0 && killingPlayers.Count == 0)
        {
            team = CustomRoleTeam.Crewmate;
            return true;
        }
        if (impostors.Count >= allPlayers.Length - impostors.Count && killingPlayers.Count == 0)
        {
            team = CustomRoleTeam.Impostor;
            return true;
        }
        if (allPlayers.Length == 1 && killingPlayers.Count == 1 && allPlayers.All(pc => pc.Is(CustomRoleTeam.Neutral)))
        {
            team = CustomRoleTeam.Neutral;
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

    public static NetworkedPlayerInfo CopyNetworkedPlayerInfo(NetworkedPlayerInfo data)
    {
        var il2cppOutfits = new Il2CppSystem.Collections.Generic.Dictionary<PlayerOutfitType, NetworkedPlayerInfo.PlayerOutfit>();
        foreach (var outfitPair in data.Outfits)
        {
            il2cppOutfits.Add(outfitPair.Key, CopyOutfit(outfitPair.Value));  // Copy each outfit
        }

        var il2cppTaskList = new Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo.TaskInfo>();
        foreach (var task in data.Tasks)
        {
            il2cppTaskList.Add(new NetworkedPlayerInfo.TaskInfo(task.TypeId, task.Id) { Complete = task.Complete });
        }

        return new NetworkedPlayerInfo()
        {
            PlayerId = data.PlayerId,
            ClientId = data.ClientId,
            FriendCode = data.FriendCode,
            Puid = data.Puid,
            RoleType = data.RoleType,
            RoleWhenAlive = data.RoleWhenAlive,
            Outfits = il2cppOutfits,
            PlayerLevel = data.PlayerLevel,
            Disconnected = data.Disconnected,
            Role = data.Role,
            Tasks = il2cppTaskList,
            IsDead = data.IsDead,
            _object = data._object
        };
    }

    private static NetworkedPlayerInfo.PlayerOutfit CopyOutfit(NetworkedPlayerInfo.PlayerOutfit outfit)
    {
        return new NetworkedPlayerInfo.PlayerOutfit()
        {
            PlayerName = outfit.PlayerName,
            ColorId = outfit.ColorId,
            HatId = outfit.HatId,
            PetId = outfit.PetId,
            SkinId = outfit.SkinId,
            VisorId = outfit.VisorId,
            NamePlateId = outfit.NamePlateId,
            HatSequenceId = outfit.HatSequenceId,
            PetSequenceId = outfit.PetSequenceId,
            SkinSequenceId = outfit.SkinSequenceId,
            VisorSequenceId = outfit.VisorSequenceId,
            NamePlateSequenceId = outfit.NamePlateSequenceId
        };
    }
}
