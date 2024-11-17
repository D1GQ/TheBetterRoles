using HarmonyLib;
using TheBetterRoles.Helpers;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.RPCs;
using UnityEngine;

namespace TheBetterRoles.Patches;

class GamePlayManager
{
    [HarmonyPatch(typeof(LobbyBehaviour))]
    public class LobbyBehaviourPatch
    {
        private static GameObject? logoSpray;
        [HarmonyPatch(nameof(LobbyBehaviour.Start))]
        [HarmonyPostfix]
        private static void Start_Postfix(LobbyBehaviour __instance)
        {
            _ = new LateTask(() =>
            {
                PlayerControl.LocalPlayer?.SendVersionRequest(Main.GetVersionText());
            }, 3f, shouldLog: false);

            _ = new LateTask(() =>
            {
                GameOptionsManager.Instance?.Initialize();
            }, 2f, shouldLog: false);

            if (logoSpray == null)
            {
                logoSpray = new GameObject("TheBetterRoles_Spray");
                logoSpray.transform.SetParent(__instance.transform, true);
                logoSpray.transform.position = new Vector3(0f, -2.35f, -1f);
                var sprite = logoSpray.AddComponent<SpriteRenderer>();
                sprite.sprite = Utils.LoadSprite("TheBetterRoles.Resources.Images.TBR_Spray.png", 170);
            }
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

    [HarmonyPatch(typeof(GameManager))]
    public class GameManagerPatch
    {
        [HarmonyPatch(nameof(GameManager.EndGame))]
        [HarmonyPostfix]
        private static void EndGame_Postfix(/*GameManager __instance*/)
        {
            if (GameState.IsHost)
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
            __instance.MinPlayers = 0;

            lobbyTimer = Mathf.Max(0f, lobbyTimer -= Time.deltaTime);
            int minutes = (int)lobbyTimer / 60;
            int seconds = (int)lobbyTimer % 60;
            lobbyTimerDisplay = $"{minutes:00}:{seconds:00}";
        }
        [HarmonyPatch(nameof(GameStartManager.Update))]
        [HarmonyPostfix]
        private static void Update_Postfix(GameStartManager __instance)
        {
            if (GameState.IsLobby)
            {
                if (!GameState.IsHost)
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
        }
        [HarmonyPatch(nameof(GameStartManager.BeginGame))]
        [HarmonyPrefix]
        private static bool BeginGame_Prefix(GameStartManager __instance)
        {
            if (!Main.AllPlayerControls.Where(pc => !pc.IsHost()).All(pc => pc.BetterData().HasMod)) return false;

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
            Logger.LogHeader($"Game Has Started - {Enum.GetName(typeof(MapNames), GameState.GetActiveMapId)}/{GameState.GetActiveMapId}", "GamePlayManager");
            CustomGameManager.GameStart();
        }

        [HarmonyPatch(nameof(GameStartManager.UpdateMapImage))]
        [HarmonyPrefix]
        private static bool UpdateMapImage_Prefix(GameStartManager __instance, [HarmonyArgument(0)] MapNames map)
        {
            if (__instance.AllMapIcons.ToArray().FirstOrDefault(m => m.Name == map).MapIcon == null)
            {
                __instance.MapImage.sprite = __instance.AllMapIcons.ToArray().First().MapIcon;
                return false;
            }
            return true;
        }

        [HarmonyPatch(nameof(GameStartManager.CheckSettingsDiffs))]
        [HarmonyPrefix]
        private static bool CheckSettingsDiffs_Prefix(GameStartManager __instance)
        {
            return false;
        }
    }
    [HarmonyPatch(typeof(EndGameManager))]
    public class EndGameManagerPatch
    {
        [HarmonyPatch(nameof(EndGameManager.ShowButtons))]
        [HarmonyPrefix]
        private static bool ShowButtons_Prefix(EndGameManager __instance)
        {
            __instance.FrontMost.gameObject.SetActive(false);
            __instance.Navigation.ShowDefaultNavigation();

            return false;
        }
    }
}
