using HarmonyLib;
using System.Text;
using TMPro;
using UnityEngine;

namespace TheBetterRoles.Patches;

class GamePlayManager
{
    [HarmonyPatch(typeof(LobbyBehaviour))]
    public class LobbyBehaviourPatch
    {
        [HarmonyPatch(nameof(LobbyBehaviour.Start))]
        [HarmonyPostfix]
        private static void Start_Postfix(/*LobbyBehaviour __instance*/)
        {
        }

        // Disabled annoying music
        [HarmonyPatch(nameof(LobbyBehaviour.Update))]
        [HarmonyPostfix]
        public static void Update_Postfix(/*LobbyBehaviour __instance*/)
        {
            if (Main.DisableLobbyTheme.Value)
                SoundManager.instance.StopSound(LobbyBehaviour.Instance.MapTheme);

        }

        [HarmonyPatch(nameof(LobbyBehaviour.RpcExtendLobbyTimer))]
        [HarmonyPostfix]
        private static void RpcExtendLobbyTimer_Postfix(/*LobbyBehaviour __instance*/)
        {
            GameStartManagerPatch.lobbyTimer += 30f;
        }
    }

    [HarmonyPatch(typeof(IntroCutscene))]
    public class IntroCutscenePatch
    {
        [HarmonyPatch(nameof(IntroCutscene.ShowRole))]
        [HarmonyPostfix]
        private static void ShowRole_Postfix(IntroCutscene __instance)
        {
            try
            {
                _ = new LateTask(() =>
                {
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

    [HarmonyPatch(typeof(GameManager))]
    public class GameManagerPatch
    {
        [HarmonyPatch(nameof(GameManager.EndGame))]
        [HarmonyPostfix]
        private static void EndGame_Postfix(/*GameManager __instance*/)
        {
            if (GameStates.IsHost)
            {
                foreach (PlayerControl player in Main.AllPlayerControls)
                {
                    player.RpcSetName(player.Data.PlayerName);
                }
            }
        }
    }

    [HarmonyPatch(typeof(GameStartManager))]
    public class GameStartManagerPatch
    {
        public static float lobbyTimer = 600f;
        public static string lobbyTimerDisplay = "";
        [HarmonyPatch(nameof(GameStartManager.Start))]
        [HarmonyPostfix]
        private static void Start_Postfix(/*GameStartManager __instance*/)
        {
            lobbyTimer = 600f;
        }
        [HarmonyPatch(nameof(GameStartManager.Update))]
        [HarmonyPrefix]
        private static void Update_Prefix(GameStartManager __instance)
        {
            lobbyTimer = Mathf.Max(0f, lobbyTimer -= Time.deltaTime);
            int minutes = (int)lobbyTimer / 60;
            int seconds = (int)lobbyTimer % 60;
            lobbyTimerDisplay = $"{minutes:00}:{seconds:00}";

            __instance.MinPlayers = 1;
        }
        [HarmonyPatch(nameof(GameStartManager.Update))]
        [HarmonyPostfix]
        private static void Update_Postfix(GameStartManager __instance)
        {
            if (!GameStates.IsHost)
            {
                __instance.StartButton.gameObject.SetActive(false);
                return;

            }
            __instance.GameStartTextParent.SetActive(false);
            __instance.StartButton.gameObject.SetActive(true);
            if (__instance.startState == GameStartManager.StartingStates.Countdown)
            {
                __instance.StartButton.buttonText.text = string.Format("{0}: {1}", Translator.GetString(StringNames.Cancel), (int)__instance.countDownTimer + 1);
            }
            else
            {
                __instance.StartButton.buttonText.text = Translator.GetString(StringNames.StartLabel);
            }
        }
        [HarmonyPatch(nameof(GameStartManager.BeginGame))]
        [HarmonyPrefix]
        private static bool BeginGame_Prefix(GameStartManager __instance)
        {
            if (__instance.startState == GameStartManager.StartingStates.Countdown)
            {
                SoundManager.instance.StopSound(__instance.gameStartSound);
                __instance.ResetStartState();
                return false;
            }

            if (Input.GetKey(KeyCode.LeftShift))
            {
                __instance.startState = GameStartManager.StartingStates.Countdown;
                __instance.FinallyBegin();
                return false;
            }

            return true;
        }

        [HarmonyPatch(nameof(GameStartManager.FinallyBegin))]
        [HarmonyPrefix]
        private static void FinallyBegin_Prefix(/*GameStartManager __instance*/)
        {
            Logger.LogHeader($"Game Has Started - {Enum.GetName(typeof(MapNames), GameStates.GetActiveMapId)}/{GameStates.GetActiveMapId}", "GamePlayManager");
        }
    }
    [HarmonyPatch(typeof(EndGameManager))]
    public class EndGameManagerPatch
    {
        [HarmonyPatch(nameof(EndGameManager.SetEverythingUp))]
        [HarmonyPostfix]
        private static void SetEverythingUp_Postfix(EndGameManager __instance)
        {
            Logger.LogHeader($"Game Has Ended - {Enum.GetName(typeof(MapNames), GameStates.GetActiveMapId)}/{GameStates.GetActiveMapId}", "GamePlayManager");

            Logger.LogHeader("Game Summary Start", "GameSummary");

            GameObject SummaryObj = UnityEngine.Object.Instantiate(__instance.WinText.gameObject, __instance.WinText.transform.parent.transform);
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

                NetworkedPlayerInfo[] playersData = GameData.Instance.AllPlayers
                    .ToArray()
                    .OrderBy(pd => pd.Disconnected)  // Disconnected players last
                    .ThenBy(pd => pd.IsDead)
                    .ThenBy(pd => !pd.Role.IsImpostor)
                    .ToArray();        // Dead players after live players

                string winTeam;
                string winTag;
                string winColor;

                switch (EndGameResult.CachedGameOverReason)
                {
                    case GameOverReason.HumansByTask:
                        winTeam = Translator.GetString(StringNames.Crewmates);
                        winTag = Translator.GetString("Game.Summary.Result.TasksCompletion");
                        winColor = "#8cffff";
                        break;
                    case GameOverReason.HumansByVote:
                        winTeam = Translator.GetString(StringNames.Crewmates);
                        winTag = Translator.GetString("Game.Summary.Result.ImpostersVotedOut");
                        winColor = "#8cffff";
                        break;
                    case GameOverReason.ImpostorDisconnect:
                        winTeam = Translator.GetString(StringNames.Crewmates);
                        winTag = Translator.GetString("Game.Summary.Result.ImpostorsDisconnected");
                        winColor = "#8cffff";
                        break;
                    case GameOverReason.ImpostorByKill:
                        winTeam = Translator.GetString(StringNames.ImpostorsCategory);
                        winTag = Translator.GetString("Game.Summary.Result.CrewOutnumbered");
                        winColor = "#f00202";
                        break;
                    case GameOverReason.ImpostorBySabotage:
                        winTeam = Translator.GetString(StringNames.ImpostorsCategory);
                        winTag = Translator.GetString("Game.Summary.Result.Sabotage");
                        winColor = "#f00202";
                        break;
                    case GameOverReason.ImpostorByVote:
                        winTeam = Translator.GetString(StringNames.ImpostorsCategory);
                        winTag = Translator.GetString("Game.Summary.Result.CrewOutnumbered");
                        winColor = "#f00202";
                        break;
                    case GameOverReason.HumansDisconnect:
                        winTeam = Translator.GetString(StringNames.ImpostorsCategory);
                        winTag = Translator.GetString("Game.Summary.Result.CrematesDisconnected");
                        winColor = "#f00202";
                        break;

                    case GameOverReason.HideAndSeek_ByTimer:
                        winTeam = Translator.GetString("Game.Summary.Hiders");
                        winTag = Translator.GetString("Game.Summary.Result.TimeOut");
                        winColor = "#8cffff";
                        break;
                    case GameOverReason.HideAndSeek_ByKills:
                        winTeam = Translator.GetString("Game.Summary.Seekers");
                        winTag = Translator.GetString("Game.Summary.Result.NoSurvivors");
                        winColor = "#f00202";
                        break;

                    default:
                        winTeam = "Unknown";
                        winTag = "Unknown";
                        winColor = "#ffffff";
                        break;
                }

                Logger.Log($"{winTeam}: {winTag}", "GameSummary");

                string SummaryHeader = $"<align=\"center\"><size=150%>   {Translator.GetString("GameSummary")}</size></align>";
                SummaryHeader += $"\n\n<size=90%><color={winColor}>{winTeam} {Translator.GetString("Game.Summary.Won")}</color></size>" +
                    $"\n<size=60%>\n{Translator.GetString("Game.Summary.By")} {winTag}</size>";

                StringBuilder sb = new StringBuilder();

                foreach (var data in playersData)
                {
                    var name = $"<color={Utils.Color32ToHex(Palette.PlayerColors[data.DefaultOutfit.ColorId])}>{data.BetterData().RealName}</color>";
                    string playerTheme(string text) => $"<color={Utils.GetCustomRoleTeamColor(data.BetterData().RoleInfo.Role.RoleTeam)}>{text}</color>";

                    string roleInfo;
                    if (data.Role.IsImpostor)
                    {
                        roleInfo = $"({playerTheme(Translator.GetString(data.Role.StringName))}) → {playerTheme($"{Translator.GetString("Kills")}: {data.BetterData().RoleInfo.Kills}")}";
                    }
                    else
                    {
                        roleInfo = $"({playerTheme(Translator.GetString(data.Role.StringName))}) → {playerTheme($"{Translator.GetString("Tasks")}: {data.Tasks.ToArray().Where(task => task.Complete).Count()}/{data.Tasks.Count}")}";
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


        [HarmonyPatch(nameof(EndGameManager.ShowButtons))]
        [HarmonyPrefix]
        private static bool ShowButtons_Prefix(EndGameManager __instance)
        {
            __instance.FrontMost.gameObject.SetActive(false);
            __instance.Navigation.ShowDefaultNavigation();
            if (!GameStates.IsLocalGame)
            {
                __instance.Navigation.ShowNavigationToProgressionScreen();
                __instance.Navigation.ContinueButton.transform.Find("ContinueButton").position -= new Vector3(0.5f, 0.2f, 0f);
            }

            return false;
        }
    }
}
