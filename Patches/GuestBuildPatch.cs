using AmongUs.GameOptions;
using HarmonyLib;
using TheBetterRoles.Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheBetterRoles.Patches;

[HarmonyPatch(typeof(JoinGameButton))]
public class GuestBuildPatch
{
    private static GameObject? instance;
    [HarmonyPatch(nameof(JoinGameButton.Start))]
    [HarmonyPostfix]
    public static void Start_Postfix(JoinGameButton __instance)
    {
        if (instance != null) return;

        Scene currentScene = SceneManager.GetActiveScene();
        string sceneName = currentScene.name;

        if (Main.IsGuestBuild && sceneName == "MMOnline")
        {
            var JoinGameMenu = UnityEngine.Object.Instantiate(__instance.gameObject);
            instance = JoinGameMenu;
            __instance.transform.parent.gameObject.DestroyObj();
        }
    }
}
