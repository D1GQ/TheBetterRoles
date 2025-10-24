using HarmonyLib;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;
using TheBetterRoles.Network;
using UnityEngine;

namespace TheBetterRoles.Patches.Game;

[HarmonyPatch(typeof(LobbyBehaviour))]
internal class LobbyBehaviourPatch
{
    private static GameObject? logoSpray;

    [HarmonyPatch(nameof(LobbyBehaviour.Start))]
    [HarmonyPostfix]
    private static void Start_Postfix(LobbyBehaviour __instance)
    {
        _ = new LateTask(() =>
        {
            GameOptionsManager.Instance?.Initialize();
        }, 1f, shouldLog: false);

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
    private static void Update_Postfix(/*LobbyBehaviour __instance*/)
    {
        if (Main.DisableLobbyTheme.Value)
            SoundManager.instance.StopSound(LobbyBehaviour.Instance.MapTheme);

    }

    [HarmonyPatch(nameof(LobbyBehaviour.RpcExtendLobbyTimer))]
    [HarmonyPostfix]
    private static void RpcExtendLobbyTimer_Postfix(/*LobbyBehaviour __instance*/)
    {
        CatchedGameData.lobbyTimer += 30f;
    }
}
