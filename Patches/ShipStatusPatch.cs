using AmongUs.GameOptions;
using HarmonyLib;
using UnityEngine;

namespace TheBetterRoles.Patches;

[HarmonyPatch(typeof(ShipStatus))]
class ShipStatusPatch
{
    // Set vision for role
    [HarmonyPatch(nameof(ShipStatus.CalculateLightRadius))]
    [HarmonyPrefix]
    public static bool CalculateLightRadius_Prefix(ShipStatus __instance, ref float __result)
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || !player.IsAlive(true))
        {
            __result = __instance.MaxLightRadius;
            return false;
        }
        if (player.Is(CustomRoleTeam.Impostor))
        {
            __result = __instance.MaxLightRadius *
                PlayerControl.LocalPlayer.BetterData()?.PlayerVisionMod * PlayerControl.LocalPlayer.BetterData()?.PlayerVisionModPlus
                ?? GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.ImpostorLightMod);
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
        __result = Mathf.Lerp(__instance.MinLightRadius, __instance.MaxLightRadius, num) *
            PlayerControl.LocalPlayer.BetterData()?.PlayerVisionMod * PlayerControl.LocalPlayer.BetterData()?.PlayerVisionModPlus ??
            GameOptionsManager.Instance.CurrentGameOptions.GetFloat(FloatOptionNames.CrewLightMod);

        return false;
    }

    [HarmonyPatch(nameof(ShipStatus.Awake))]
    [HarmonyPostfix]
    public static void Awake_Postfix(ShipStatus __instance)
    {
        SubmergedCompatibility.SetupMap(__instance);
    }

    [HarmonyPatch(nameof(ShipStatus.OnDestroy))]
    [HarmonyPostfix]
    public static void OnDestroy_Postfix(/*ShipStatus __instance*/)
    {
        SubmergedCompatibility.SetupMap(null);
    }
}
