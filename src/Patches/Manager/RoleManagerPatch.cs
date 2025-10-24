using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using TheBetterRoles.Managers;
using TheBetterRoles.Network;

namespace TheBetterRoles.Patches.Manager;

[HarmonyPatch(typeof(RoleManager))]
internal class RoleManagerPatch
{
    [HarmonyPatch(nameof(RoleManager.SelectRoles))]
    [HarmonyPrefix]
    private static bool RoleManager_Prefix(RoleManager __instance)
    {
        __instance.StartCoroutine(CatchedGameData.Instance?.CurrentGameMode.CoAssignRoles());

        return false;
    }

    [HarmonyPatch(nameof(RoleManager.AssignRoleOnDeath))]
    [HarmonyPrefix]
    private static bool AssignRoleOnDeath_Prefix(/*RoleManager __instance*/ [HarmonyArgument(0)] PlayerControl player)
    {
        CustomRoleManager.AssignGhostRoleOnDeath(player);

        return false;
    }
}