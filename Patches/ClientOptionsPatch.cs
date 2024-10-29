using HarmonyLib;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using UnityEngine;

namespace TheBetterRoles.Patches;

[HarmonyPatch(typeof(OptionsMenuBehaviour))]
public static class OptionsMenuBehaviourPatch
{
    private static ClientOptionItem? ForceOwnLanguage;
    private static ClientOptionItem? ChatDarkMode;
    private static ClientOptionItem? DisableLobbyTheme;
    private static ClientOptionItem? UnlockFPS;
    private static ClientOptionItem? ShowFPS;
    private static ClientOptionItem? OpenSaveData;
    private static ClientOptionItem? SwitchToVanilla;

    [HarmonyPatch(nameof(OptionsMenuBehaviour.Start))]
    [HarmonyPrefix]
    public static void Start_Postfix(OptionsMenuBehaviour __instance)
    {
        static bool toggleCheckInGamePlay(string buttonName)
        {
            bool flag = GameState.IsInGame && !GameState.IsLobby || GameState.IsFreePlay;
            if (flag)
                BetterNotificationManager.Notify($"Unable to toggle '{buttonName}' while in gameplay!", 2.5f);

            return flag;
        }
        static bool toggleCheckInGame(string buttonName)
        {
            bool flag = GameState.IsInGame;
            if (flag)
                BetterNotificationManager.Notify($"Unable to toggle '{buttonName}' while in game!", 2.5f);

            return flag;
        }

        if (__instance.DisableMouseMovement == null) return;

        if (ForceOwnLanguage == null || ForceOwnLanguage.ToggleButton == null)
        {
            string title = Translator.GetString("BetterOption.ForceOwnLanguage");
            ForceOwnLanguage = ClientOptionItem.Create(title, Main.ForceOwnLanguage, __instance);
        }

        if (ChatDarkMode == null || ChatDarkMode.ToggleButton == null)
        {
            string title = Translator.GetString("BetterOption.ChatDarkMode");
            ChatDarkMode = ClientOptionItem.Create(title, Main.ChatDarkMode, __instance, ChatDarkModeToggle);

            static void ChatDarkModeToggle()
            {
                ChatPatch.ChatControllerPatch.SetChatTheme();
            }
        }

        if (DisableLobbyTheme == null || DisableLobbyTheme.ToggleButton == null)
        {
            string title = Translator.GetString("BetterOption.LobbyTheme");
            DisableLobbyTheme = ClientOptionItem.Create(title, Main.DisableLobbyTheme, __instance, DisableLobbyThemeButtonToggle);
            static void DisableLobbyThemeButtonToggle()
            {
                if (GameState.IsLobby && !Main.DisableLobbyTheme.Value)
                {
                    SoundManager.instance.CrossFadeSound("MapTheme", LobbyBehaviour.Instance.MapTheme, 0.5f, 1.5f);
                }
            }
        }

        if (UnlockFPS == null || UnlockFPS.ToggleButton == null)
        {
            string title = Translator.GetString("BetterOption.UnlockFPS");
            UnlockFPS = ClientOptionItem.Create(title, Main.UnlockFPS, __instance, UnlockFPSButtonToggle);
            static void UnlockFPSButtonToggle()
            {
                Application.targetFrameRate = Main.UnlockFPS.Value ? 165 : 60;
            }
        }

        if (ShowFPS == null || ShowFPS.ToggleButton == null)
        {
            string title = Translator.GetString("BetterOption.ShowFPS");
            ShowFPS = ClientOptionItem.Create(title, Main.ShowFPS, __instance);
        }

        if (OpenSaveData == null || OpenSaveData.ToggleButton == null)
        {
            string title = Translator.GetString("BetterOption.SaveData");
            OpenSaveData = ClientOptionItem.Create(title, null, __instance, OpenSaveDataButtonToggle, IsToggle: false);
            static void OpenSaveDataButtonToggle()
            {
                if (File.Exists(BetterDataManager.GetFilePath("BetterData")))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = BetterDataManager.GetFilePath("BetterRoleData"),
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
            }
        }

        if (SwitchToVanilla == null || SwitchToVanilla.ToggleButton == null)
        {
            string title = Translator.GetString("BetterOption.ToVanilla");
            SwitchToVanilla = ClientOptionItem.Create(title, null, __instance, SwitchToVanillaButtonToggle, IsToggle: false, toggleCheck: () => !toggleCheckInGame(title));
            static void SwitchToVanillaButtonToggle()
            {
                BetterNotificationManager.BAUNotificationManagerObj.DestroyObj();
                Harmony.UnpatchAll();
            }
        }
    }

    [HarmonyPatch(nameof(OptionsMenuBehaviour.Close))]
    [HarmonyPrefix]
    public static void Close_Postfix()
    {
        ClientOptionItem.CustomBackground?.gameObject.SetActive(false);
    }
}