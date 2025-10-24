using HarmonyLib;
using TheBetterRoles.Items;
using TheBetterRoles.Modules;
using TheBetterRoles.Patches.UI.Chat;
using UnityEngine;

namespace TheBetterRoles.Patches.UI;

[HarmonyPatch(typeof(OptionsMenuBehaviour))]
internal static class OptionsMenuBehaviourPatch
{
    private static ClientOptionItem? ForceOwnLanguage;
    private static ClientOptionItem? ChatDarkMode;
    private static ClientOptionItem? DisableLobbyTheme;
    private static ClientOptionItem? UnlockFPS;
    private static ClientOptionItem? ShowFPS;

    [HarmonyPatch(nameof(OptionsMenuBehaviour.Start))]
    [HarmonyPrefix]
    private static void Start_Postfix(OptionsMenuBehaviour __instance)
    {
        /*
        static bool toggleCheckInGamePlay(string buttonName)
        {
            bool flag = GameState.IsInGame && !GameState.IsLobby || GameState.IsFreePlay;
            if (flag)
                TBRNotificationManager.Notify($"Unable to toggle '{buttonName}' while in gameplay!", 2.5f);

            return flag;
        }

        static bool toggleCheckInGame(string buttonName)
        {
            bool flag = GameState.IsInGame;
            if (flag)
                TBRNotificationManager.Notify($"Unable to toggle '{buttonName}' while in game!", 2.5f);

            return flag;
        }
        */

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
                ChatControllerPatch.SetChatTheme();
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
    }

    [HarmonyPatch(nameof(OptionsMenuBehaviour.Close))]
    [HarmonyPrefix]
    private static void Close_Postfix()
    {
        ClientOptionItem.CustomBackground?.gameObject.SetActive(false);
    }
}