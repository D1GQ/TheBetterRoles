using AmongUs.GameOptions;
using HarmonyLib;
using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using TheBetterRoles.Network;
using TheBetterRoles.Roles;
using UnityEngine;

namespace TheBetterRoles.Patches.Game.Ship;

[HarmonyPatch(typeof(ShipStatus))]
internal class ShipStatusPatch
{
    internal static Sprite? CatchedMeetingButtonSprite;

    [HarmonyPatch(nameof(ShipStatus.Start))]
    [HarmonyPostfix]
    private static void Start_Postfix(ShipStatus __instance)
    {
        if (__instance == null) return;

        CustomRoleManager.CleanUpRoles();
        ShipStatusExtension.TrySetShipExtension(__instance);
        BaseSystem.AddSystems();

        CatchedMeetingButtonSprite = __instance.EmergencyButton?.Image?.sprite;

        _ = new LateTask(() =>
        {
            GameOptionsManager.Instance?.Initialize();
        }, 0.5f, shouldLog: false);

        SystemPatch.LastActive = false;
        SystemPatch.IsCamouflageActive = false;
    }

    [HarmonyPatch(nameof(ShipStatus.CalculateLightRadius))]
    [HarmonyPrefix]
    private static bool CalculateLightRadius_Prefix(ShipStatus __instance, ref float __result)
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || !player.IsAlive(true))
        {
            __result = __instance.MaxLightRadius;
            return false;
        }
        float blackOutNum = BlackoutSabotageSystem.Instance?.VisionSize ?? 1f;
        float blackOut = !player.Is(RoleClassTeam.Impostor) ? blackOutNum : Mathf.Lerp(0.5f, 1f, blackOutNum);
        float radius = __result;
        if (player.CheckAnyRoles(role => role.HasImpostorVision))
        {
            __result = (__instance.MaxLightRadius *
                PlayerControl.LocalPlayer.ExtendedData()?.PlayerVisionMod * PlayerControl.LocalPlayer.ExtendedData()?.PlayerVisionModPlus
                ?? GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.ImpostorLightMod)) * blackOut;
            return false;
        }
        float num = 1f;
        if (__instance.Systems.TryGetValue(SystemTypes.Electrical, out var systemType))
        {
            var switchSystem = systemType.Cast<SwitchSystem>();
            if (switchSystem != null)
            {
                num = switchSystem.Value / 255f;
            }
        }
        __result = (Mathf.Lerp(__instance.MinLightRadius, __instance.MaxLightRadius, num) *
            PlayerControl.LocalPlayer.ExtendedData()?.PlayerVisionMod * PlayerControl.LocalPlayer.ExtendedData()?.PlayerVisionModPlus ??
            GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.CrewLightMod)) * blackOut;

        return false;
    }
}

[HarmonyPatch]
class ShipStatusSpawnPlayerPatch
{
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.SpawnPlayer))]
    [HarmonyPatch(typeof(PolusShipStatus), nameof(PolusShipStatus.SpawnPlayer))]
    [HarmonyPatch(typeof(AirshipStatus), nameof(AirshipStatus.SpawnPlayer))]
    [HarmonyPrefix]
    internal static bool SpawnPlayer_Prefix([HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] int numPlayers, [HarmonyArgument(2)] bool initialSpawn)
    {
        if (ShipStatusExtension.Instance != null && ShipStatusExtension.Instance.SpawnPlayer(player, numPlayers, initialSpawn) == false) return false;
        return true;
    }
}