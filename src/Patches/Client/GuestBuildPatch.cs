using HarmonyLib;
using TheBetterRoles.Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheBetterRoles.Patches.Client;

internal class GuestBuildPatch
{
    internal static TextMeshPro? GuestBuildWatermark;

    [HarmonyPatch(typeof(JoinGameButton))]
    internal class GuestBuildJoinGameButtonPatch
    {
        private static JoinGameButton? instance;

        [HarmonyPatch(nameof(JoinGameButton.Start))]
        [HarmonyPostfix]
        private static void Start_Postfix(JoinGameButton __instance)
        {
            if (instance != null) return;

            Scene currentScene = SceneManager.GetActiveScene();
            string sceneName = currentScene.name;

            if (ModInfo.IsGuestBuild && sceneName == "MMOnline")
            {
                var JoinGameButton = UnityEngine.Object.Instantiate(__instance);
                instance = JoinGameButton;
                __instance.transform.parent.gameObject.DestroyObj();
                JoinGameButton.transform.localPosition += new Vector3(0f, 2f, 0f);
                JoinGameButton.transform.Find("JoinGameMenu").localPosition -= new Vector3(0f, 2f, 0f); ;
            }
        }
    }

    [HarmonyPatch(typeof(MainMenuManager))]
    internal class GuestBuildMainMenuManagerPatch
    {
        [HarmonyPatch(nameof(MainMenuManager.Start))]
        [HarmonyPostfix]
        private static void Start_Postfix(MainMenuManager __instance)
        {
            if (ModInfo.IsGuestBuild)
            {
                __instance.playLocalButton.DestroyObj();
                __instance.freePlayButton.DestroyObj();

                __instance.PlayOnlineButton.transform.localPosition -= new Vector3(1.15f, 0f, 0f);
                __instance.howToPlayButton.transform.localPosition += new Vector3(1.15f, 0f, 0f);
            }
        }
    }
}