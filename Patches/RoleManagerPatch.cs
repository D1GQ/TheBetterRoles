using AmongUs.GameOptions;
using HarmonyLib;

namespace TheBetterRoles.Patches;

[HarmonyPatch(typeof(RoleManager))]
public class RoleManagerPatch
{
    // role algorithm
    [HarmonyPatch(nameof(RoleManager.SelectRoles))]
    [HarmonyPrefix]
    public static bool RoleManager_Prefix(/*RoleManager __instance*/)
    {
        CustomRoleManager.AssignRoles();

        return false;
    }

    [HarmonyPatch(nameof(RoleManager.AssignRoleOnDeath))]
    [HarmonyPrefix]
    public static bool AssignRoleOnDeath_Prefix(/*RoleManager __instance*/ [HarmonyArgument(0)] PlayerControl player)
    {
        player.RawSetRole(RoleTypes.CrewmateGhost);

        CustomRoleManager.AssignGhostRoleOnDeath(player);

        return false;
    }
}