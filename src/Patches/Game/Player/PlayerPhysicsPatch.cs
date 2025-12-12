using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using System.Collections;
using TheBetterRoles.Data;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules.CustomSystems;

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
    private static void CoEnterVent_Postfix(PlayerPhysics __instance, int id, ref Il2CppSystem.Collections.IEnumerator __result)
    {
        Logger.LogPrivate($"{__instance.myPlayer.Data.PlayerName} Has entered vent: {id}", "EventLog");
        __result = Effects.Sequence(CoWaitForVent(id).WrapToIl2Cpp(), Effects.All(__result, CoEnterVentFixCollision(__instance).WrapToIl2Cpp()));
    }

    /// <summary>
    /// Fixes the issue of colliding with objects when entering vents.
    /// </summary>
    private static IEnumerator CoEnterVentFixCollision(PlayerPhysics __instance)
    {
        var player = __instance?.myPlayer;
        if (player == null) yield break;

        bool didSet = false;
        if (player.Collider.enabled)
        {
            player.Collider.enabled = false;
            didSet = true;
        }

        while (player?.inVent == false)
        {
            if (player == null)
            {
                yield break;
            }

            yield return null;
        }

        if (didSet)
        {
            player.Collider.enabled = true;
        }

        yield break;
    }

    [HarmonyPatch(nameof(PlayerPhysics.CoExitVent))]
    [HarmonyPostfix]
    private static void CoExitVent_Postfix(PlayerPhysics __instance, int id, ref Il2CppSystem.Collections.IEnumerator __result)
    {
        Logger.LogPrivate($"{__instance.myPlayer.Data.PlayerName} Has exit vent: {id}", "EventLog");

        __result = Effects.Sequence(CoWaitForVent(id).WrapToIl2Cpp(), __result);
    }

    /// <summary>
    /// Waits until the vent with the given ID exists in the VentFactorySystem.
    /// </summary>
    private static IEnumerator CoWaitForVent(int ventId)
    {
        while (VentFactorySystem.Instance?.AllVents?.FirstOrDefault(v => v.Id == ventId) == null)
        {
            yield return null;
        }
    }
}