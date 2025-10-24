using HarmonyLib;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;
using TheBetterRoles.Network;
using UnityEngine;

namespace TheBetterRoles.Patches.Manager.GameStart;

[HarmonyPatch(typeof(GameStartManager))]
internal class StartGamePatch
{
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
        if (!Main.AllPlayerControls.Where(pc => !pc.IsHost()).All(pc => pc.ExtendedData().HasMod) &&
            (!Input.GetKey(KeyCode.LeftShift) || !Input.GetKey(KeyCode.LeftControl))) return false;

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
}
