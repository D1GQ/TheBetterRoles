using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using System.Collections;
using TheBetterRoles.Data;
using TheBetterRoles.Helpers;
using UnityEngine;

namespace TheBetterRoles.Patches.Game.Player;

[HarmonyPatch(typeof(PlayerPhysics))]
internal class PlayerPhysicsPatch
{
    [HarmonyPatch("get_SpeedMod")]
    [HarmonyPrefix]
    private static bool SpeedMod_Prefix(PlayerPhysics __instance, ref float __result)
    {
        if (GameManager.Instance == null)
        {
            __result = 1f;
            return false;
        }

        float playerSpeedMod = VanillaGameSettings.PlayerSpeed.GetFloat();

        if (__instance.myPlayer != null && __instance.myPlayer.Data != null && !__instance.myPlayer.IsAlive())
        {
            __result = playerSpeedMod * __instance.GhostSpeed / __instance.Speed;
            return false;
        }

        __result = playerSpeedMod;
        return false;
    }

    [HarmonyPatch(nameof(PlayerPhysics.BootFromVent))]
    [HarmonyPostfix]
    private static void BootFromVent_Postfix(PlayerPhysics __instance, [HarmonyArgument(0)] int ventId)
    {
        Logger.LogPrivate($"{__instance.myPlayer.Data.PlayerName} Has been booted from vent: {ventId}", "EventLog");
    }

    [HarmonyPatch(nameof(PlayerPhysics.CoEnterVent))]
    [HarmonyPostfix]
    private static void CoEnterVent_Postfix(PlayerPhysics __instance, [HarmonyArgument(0)] int ventId)
    {
        Logger.LogPrivate($"{__instance.myPlayer.Data.PlayerName} Has entered vent: {ventId}", "EventLog");
        __instance.StartCoroutine(CoEnterVentFixCollision(__instance));
    }

    private static IEnumerator CoEnterVentFixCollision(PlayerPhysics __instance)
    {
        yield return new WaitForSeconds(1f);

        var player = __instance?.myPlayer;
        if (player == null) yield break;

        Vector2 lastPos = player.GetCustomPosition();
        while (player?.inVent == false)
        {
            if (lastPos == player.GetCustomPosition())
            {
                player.Collider.enabled = false;
            }

            lastPos = player.GetCustomPosition();

            yield return null;
        }

        if (player == null) yield break;

        player.Collider.enabled = true;
    }

    [HarmonyPatch(nameof(PlayerPhysics.CoExitVent))]
    [HarmonyPostfix]
    private static void CoExitVent_Postfix(PlayerPhysics __instance, [HarmonyArgument(0)] int ventId)
    {

        Logger.LogPrivate($"{__instance.myPlayer.Data.PlayerName} Has exit vent: {ventId}", "EventLog");
    }
}