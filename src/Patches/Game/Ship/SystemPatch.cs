using HarmonyLib;
using TheBetterRoles.Data;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;
using TheBetterRoles.Roles;
using TheBetterRoles.Roles.Core.Interfaces;

namespace TheBetterRoles.Patches.Game.Ship;

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
    internal static void OnSabotage_Prefix(ISystemType __instance)
    {
        void RoleCheck()
        {
            RoleListener.InvokeRoles<IRoleSabotageAction>(role => role.OnSabotage(__instance, __instance.GetSystemTypes()));
        }

        if (__instance.TryCast<SwitchSystem>(out var switchSystem))
        {
            if (!switchSystem.IsActive)
            {
                RoleCheck();
            }
            return;
        }
        else if (__instance.TryCast<ReactorSystemType>(out var reactorSystem))
        {
            if (!reactorSystem.IsActive)
            {
                RoleCheck();
            }
            return;
        }
        else if (__instance.TryCast<LifeSuppSystemType>(out var lifeSuppSystem))
        {
            if (!lifeSuppSystem.IsActive)
            {
                RoleCheck();
            }
            return;
        }
        else if (__instance.TryCast<HeliSabotageSystem>(out var heliSabotageSystem))
        {
            if (!heliSabotageSystem.IsActive)
            {
                RoleCheck();
            }
            return;
        }
        else if (__instance.TryCast<HqHudSystemType>(out var hqHudSystem))
        {
            if (!hqHudSystem.IsActive)
            {
                RoleCheck();
            }
            CamouflageComms(__instance, hqHudSystem.IsActive);
            return;
        }
        else if (__instance.TryCast<HudOverrideSystemType>(out var hudOverrideSystem))
        {
            if (!hudOverrideSystem.IsActive)
            {
                RoleCheck();
            }
            CamouflageComms(__instance, hudOverrideSystem.IsActive);
            return;
        }
        else if (CastHelper.TryCast<MushroomMixupSabotageSystem>(__instance))
        {
            RoleCheck();
        }
    }

    internal static bool LastActive = false;
    internal static bool IsCamouflageActive = false;
    internal static void CamouflageComms(ISystemType __instance, bool active)
    {
        if (TBRGameSettings.CamouflageComms.GetBool() && GameManager.Instance.GameHasStarted)
        {
            active = !active;
            _ = new LateTask(() =>
            {
                if (LastActive != active)
                {
                    if (!IsCamouflageActive)
                    {
                        IsCamouflageActive = true;
                        foreach (var player in Main.AllPlayerControls)
                        {
                            if (player == null) continue;

                            player.SetCamouflage(IsCamouflageActive);
                        }
                    }
                    else
                    {
                        IsCamouflageActive = false;
                        foreach (var player in Main.AllPlayerControls)
                        {
                            if (player == null) continue;

                            player.SetCamouflage(IsCamouflageActive);
                        }
                    }
                }
                LastActive = active;
            }, 0.005f, shouldLog: false);
        }
    }
}
