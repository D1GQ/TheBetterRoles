using AmongUs.GameOptions;
using HarmonyLib;
using TheBetterRoles.Helpers;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;
using UnityEngine;

namespace TheBetterRoles.Patches;

[HarmonyPatch(typeof(ShipStatus))]
class ShipStatusPatch
{
    private static GameObject? settingsComputer;
    [HarmonyPatch(nameof(ShipStatus.Start))]
    [HarmonyPostfix]
    public static void Start_Postfix(ShipStatus __instance)
    {
        if (GameState.IsFreePlay)
        {
            if (settingsComputer == null)
            {
                settingsComputer = ObjectHelper.FindObjectByName("CustomizeComputer");
                if (settingsComputer != null)
                {
                    settingsComputer.SetActive(true);
                    var button = settingsComputer.GetComponent<ButtonBehavior>();
                    button?.DestroyMono();

                    Vector3 pos = Vector3.zeroVector;
                    float Z = settingsComputer.transform.position.z;
                    bool invert = false;
                    if (__instance.TryCast<SkeldShipStatus>())
                    {
                        pos = new Vector3(0.7127f, 3.3518f, Z);
                    }
                    else if (__instance.TryCast<MiraShipStatus>())
                    {
                        pos = new Vector3(24.7253f, 3.7456f, Z);
                        invert = true;
                    }
                    else if (__instance.TryCast<PolusShipStatus>())
                    {
                        pos = new Vector3(18.3167f, -16.9478f, -1f);
                    }
                    else if (__instance.TryCast<AirshipStatus>())
                    {
                        pos = new Vector3(-1.5738f, -0.3927f, Z);
                        invert = true;
                    }
                    else if (__instance.TryCast<FungleShipStatus>())
                    {
                        pos = new Vector3(-3.8168f, -1.5715f, Z);
                    }

                    settingsComputer.transform.position = pos;
                    if (invert) settingsComputer.transform.localScale = new Vector3(0f - settingsComputer.transform.localScale.x, settingsComputer.transform.localScale.y, settingsComputer.transform.localScale.z);
                }
            }
        }
    }

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
        if (CustomRoleManager.RoleChecksAny(player, role => role.HasImpostorVision, log: false))
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
}

[HarmonyPatch]
class SystemPatch
{
    [HarmonyPatch(typeof(SwitchSystem), nameof(SwitchSystem.UpdateSystem))] // Lights
    [HarmonyPatch(typeof(SwitchSystem), nameof(SwitchSystem.Deserialize))] // Lights
    [HarmonyPatch(typeof(ReactorSystemType), nameof(ReactorSystemType.UpdateSystem))] // Reactor
    [HarmonyPatch(typeof(ReactorSystemType), nameof(ReactorSystemType.Deserialize))] // Reactor
    [HarmonyPatch(typeof(LifeSuppSystemType), nameof(LifeSuppSystemType.UpdateSystem))]// O2
    [HarmonyPatch(typeof(LifeSuppSystemType), nameof(LifeSuppSystemType.Deserialize))] // O2
    [HarmonyPatch(typeof(HeliSabotageSystem), nameof(HeliSabotageSystem.UpdateSystem))] // Airship meltdown
    [HarmonyPatch(typeof(HeliSabotageSystem), nameof(HeliSabotageSystem.Deserialize))] // Airship meltdown
    [HarmonyPatch(typeof(HqHudSystemType), nameof(HqHudSystemType.UpdateSystem))] // Comms
    [HarmonyPatch(typeof(HqHudSystemType), nameof(HqHudSystemType.Deserialize))] // Comms
    [HarmonyPatch(typeof(HudOverrideSystemType), nameof(HudOverrideSystemType.UpdateSystem))] // Comms
    [HarmonyPatch(typeof(HudOverrideSystemType), nameof(HudOverrideSystemType.Deserialize))] // Comms
    [HarmonyPatch(typeof(MushroomMixupSabotageSystem), nameof(MushroomMixupSabotageSystem.MushroomMixUp))] // MushroomMixup
    [HarmonyPrefix]
    public static void OnSabotage_Prefix(ISystemType __instance)
    {
        void RoleCheck()
        {
            CustomRoleManager.RoleListenerOther(role => role.OnSabotage(__instance, __instance.GetSystemTypes()));
        }

        if (CastHelper.TryCast<SwitchSystem>(__instance, out var switchSystem))
        {
            if (!switchSystem.IsActive)
            {
                RoleCheck();
            }
            return;
        }
        else if (CastHelper.TryCast<ReactorSystemType>(__instance, out var reactorSystem))
        {
            if (!reactorSystem.IsActive)
            {
                RoleCheck();
            }
            return;
        }
        else if (CastHelper.TryCast<LifeSuppSystemType>(__instance, out var lifeSuppSystem))
        {
            if (!lifeSuppSystem.IsActive)
            {
                RoleCheck();
            }
            return;
        }
        else if (CastHelper.TryCast<HeliSabotageSystem>(__instance, out var heliSabotageSystem))
        {
            if (!heliSabotageSystem.IsActive)
            {
                RoleCheck();
            }
            return;
        }
        else if (CastHelper.TryCast<HqHudSystemType>(__instance, out var hqHudSystem))
        {
            if (!hqHudSystem.IsActive)
            {
                RoleCheck();
            }
            return;
        }
        else if (CastHelper.TryCast<HudOverrideSystemType>(__instance, out var hudOverrideSystem))
        {
            if (!hudOverrideSystem.IsActive)
            {
                RoleCheck();
            }
            return;
        }
        else if (CastHelper.TryCast<MushroomMixupSabotageSystem>(__instance))
        {
            RoleCheck();
        }
    }
}
