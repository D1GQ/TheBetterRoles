using System.Text;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Modules;
using TheBetterRoles.Network;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Managers;

internal class GameSummaryManager
{
    internal static void SetupGameSummary(EndGameManager endGameManager)
    {
        if (CatchedGameData.Instance.CatchedGameEndReason == EndGameReason.CriticalError) return;

        List<NetworkedPlayerInfo> players = CatchedGameData.CatchedPlayerData.ToList();
        if (!players.Any()) return;

        var firstPlayer = players.First();
        var role = firstPlayer?.ExtendedData()?.RoleInfo?.Role;
        if (role == null) return;

        LogGameSummaryHeader();

        var summaryObj = CreateSummaryObject(endGameManager);
        var summaryText = summaryObj.GetComponent<TextMeshPro>();

        if (summaryText != null)
        {
            ConfigureSummaryText(summaryText);
            var sortedPlayers = SortPlayersData(players);

            string winTeam, winColor, winTag;
            GetWinningTeamInfo(out winTeam, out winColor, out winTag);

            LogWinningTeamInfo(winTeam, winTag);

            string summaryHeader = GenerateSummaryHeader(winTeam, winColor, winTag);

            string playerSummary = GeneratePlayerSummary(sortedPlayers);

            summaryText.text = $"{summaryHeader}\n\n{playerSummary.Size(58f)}";

            Logger.LogHeader("Game Summary End", "GameSummary");
        }
    }

    private static void LogGameSummaryHeader()
    {
        Logger.LogHeader($"Game Has Ended - {Enum.GetName(typeof(MapNames), GameState.GetActiveMapId)}/{GameState.GetActiveMapId}", "GamePlayManager");
        Logger.LogHeader("Game Summary Start", "GameSummary");
    }

    private static GameObject CreateSummaryObject(EndGameManager endGameManager)
    {
        GameObject summaryObj = UnityEngine.Object.Instantiate(endGameManager.WinText.gameObject, endGameManager.WinText.transform.parent);
        summaryObj.name = "SummaryObj (TMP)";
        summaryObj.transform.SetSiblingIndex(0);

        Camera localCamera = HudManager.InstanceExists
            ? HudManager.Instance.GetComponentInChildren<Camera>()
            : Camera.main;

        summaryObj.transform.position = AspectPosition.ComputeWorldPosition(localCamera, AspectPosition.EdgeAlignments.LeftTop, new Vector3(1f, 0.2f, -5f));
        summaryObj.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);

        return summaryObj;
    }

    private static void ConfigureSummaryText(TextMeshPro summaryText)
    {
        summaryText.autoSizeTextContainer = false;
        summaryText.enableAutoSizing = false;
        summaryText.lineSpacing = -25f;
        summaryText.alignment = TextAlignmentOptions.TopLeft;
        summaryText.color = Color.white;
    }

    private static NetworkedPlayerInfo[] SortPlayersData(List<NetworkedPlayerInfo> players)
    {
        return players
            .OrderBy(pd => pd.Disconnected)
            .ThenBy(pd => pd.IsDead)
            .ThenBy(pd => CustomGameManager.CheckWinner(pd) ? 0 : 1)
            .ThenBy(pd => pd.Role()?.RoleTeam == RoleClassTeam.Crewmate)
            .ThenBy(pd => pd.Role()?.RoleTeam == RoleClassTeam.Impostor)
            .ThenBy(pd => pd.Role()?.RoleTeam == RoleClassTeam.Neutral)
            .ToArray();
    }

    private static void GetWinningTeamInfo(out string winTeam, out string winColor, out string winTag)
    {
        var role = CatchedGameData.CatchedPlayerData.First().Role();
        winTeam = role.RoleName;
        winColor = role.RoleColorHex;

        switch (CatchedGameData.Instance.CatchedWinTeam)
        {
            case RoleClassTeam.Impostor:
                winTeam = Translator.GetString(StringNames.ImpostorsCategory);
                winColor = Utils.GetCustomRoleTeamColorHex(RoleClassTeam.Impostor);
                break;
            case RoleClassTeam.Crewmate:
                winTeam = Translator.GetString(StringNames.Crewmates);
                winColor = Utils.GetCustomRoleTeamColorHex(RoleClassTeam.Crewmate);
                break;
            case RoleClassTeam.Neutral:
                winTeam = Translator.GetString("Neutrals");
                winColor = Utils.GetCustomRoleTeamColorHex(RoleClassTeam.Neutral);
                break;
        }

        switch (CatchedGameData.Instance.CatchedGameEndReason)
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
                winTag = string.Empty;
                break;
        }
    }

    private static void LogWinningTeamInfo(string winTeam, string winTag)
    {
        Logger.Log($"{winTeam}: {winTag}", "GameSummary");
    }

    private static string GenerateSummaryHeader(string winTeam, string winColor, string winTag)
    {
        string summaryHeader = $"<align=\"center\"><size=150%>   {Translator.GetString("GameSummary")}</size></align>";

        if (CatchedGameData.Instance.CatchedWinTeam != RoleClassTeam.None)
        {
            summaryHeader += $"\n\n<size=90%><color={winColor}>{winTeam} {Translator.GetString("Game.Summary.Won")}</color></size>" +
                             $"\n<size=60%>\n{Translator.GetString("Game.Summary.By")} {winTag}</size>";
        }

        return summaryHeader;
    }

    private static string GeneratePlayerSummary(NetworkedPlayerInfo[] sortedPlayers)
    {
        StringBuilder sb = new StringBuilder();

        foreach (var player in sortedPlayers)
        {
            var extendedData = player.ExtendedData();
            string name = $"<color={Colors.Color32ToHex(Palette.PlayerColors[player.DefaultOutfit.ColorId])}>{extendedData.RealName}</color>";
            string playerTheme(string text) => $"<color={Utils.GetCustomRoleTeamColorHex(extendedData.RoleInfo.Role.RoleTeam)}>{text}</color>";

            var roleInfo = $"({string.Join("<#A3A3A3> > </color>", extendedData.RoleInfo.RoleHistory.Select(r => Utils.GetCustomRoleNameAndColor(r)))})";

            if (extendedData.RoleInfo.Role.HasTask || extendedData.RoleInfo.Role.HasSelfTask)
            {
                roleInfo += $" → {playerTheme($"{Translator.GetString("Tasks")}: {player.Tasks.CountIl2Cpp(task => task.Complete)}/{player.Tasks.Count}")}";
            }
            if (extendedData.RoleInfo.Role.CanKill)
            {
                roleInfo += $" → {playerTheme($"{Translator.GetString("Kills")}: {extendedData.RoleInfo.Kills}")}";
            }

            string deathReason = player.Disconnected
                ? $"『<color=#838383><b>{Translator.GetString("DC")}</b></color>』"
                : player.IsDead
                    ? $"『<color=#ff0600><b>{Utils.FormatDeathReason(extendedData.DeathReason, extendedData.DeathReasonColor)}</b></color>』"
                    : $"『<color=#80ff00><b>{Translator.GetString("Alive")}</b></color>』";

            Logger.Log($"{name} {roleInfo} {deathReason}", "GameSummary");

            sb.AppendLine($"- {name} {roleInfo} {deathReason}\n");
        }

        return sb.ToString();
    }
}
