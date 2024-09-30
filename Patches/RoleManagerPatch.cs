using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;

namespace TheBetterRoles.Patches;

[HarmonyPatch(typeof(RoleManager))]
public class RoleManagerPatch
{
    public static Dictionary<PlayerControl, RoleTypes> SetPlayerRole = []; // Player, Role
    public static Dictionary<string, int> ImpostorMultiplier = []; // HashPuid, Multiplier
    private static Random random = new Random();

    // Better role algorithm
    [HarmonyPatch(nameof(RoleManager.SelectRoles))]
    [HarmonyPrefix]
    public static bool RoleManager_Prefix(/*RoleManager __instance*/)
    {
        CustomRoleManager.AssignRoles();

        return false;
    }

    public static void RegularBetterRoleAssignment()
    {
    }

    public static void HideAndSeekBetterRoleAssignment()
    {
    }

    [HarmonyPatch(nameof(RoleManager.AssignRoleOnDeath))]
    [HarmonyPrefix]
    public static bool AssignRoleOnDeath_Prefix(/*RoleManager __instance*/ [HarmonyArgument(0)] PlayerControl player)
    {
        player.RawSetRole(RoleTypes.CrewmateGhost);

        return false;
    }
}