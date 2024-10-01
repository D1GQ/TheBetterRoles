using HarmonyLib;

namespace TheBetterRoles;

internal static class TaskPatches
{
    [HarmonyPatch(typeof(GameData), nameof(GameData.RecomputeTaskCounts))]
    private class GameData_RecomputeTaskCounts
    {
        private static bool Prefix(GameData __instance)
        {
            __instance.TotalTasks = 0;
            __instance.CompletedTasks = 0;
            for (var i = 0; i < __instance.AllPlayers.Count; i++)
            {
                var playerInfo = __instance.AllPlayers.ToArray()[i];
                if (!playerInfo.Disconnected && playerInfo.Tasks != null && playerInfo.Object &&
                    (GameOptionsManager.Instance.currentNormalGameOptions.GhostsDoTasks || !playerInfo.IsDead) && playerInfo.Object.BetterData().RoleInfo.Role.HasTask)
                    for (var j = 0; j < playerInfo.Tasks.Count; j++)
                    {
                        __instance.TotalTasks++;
                        if (playerInfo.Tasks.ToArray()[j].Complete) __instance.CompletedTasks++;
                    }
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(Console), nameof(Console.CanUse))]
    private class Console_CanUse
    {
        private static bool Prefix(Console __instance, [HarmonyArgument(0)] NetworkedPlayerInfo playerInfo, ref float __result)
        {
            var playerControl = playerInfo.Object;
            var flag = playerControl.BetterData().RoleInfo.Role.HasTask;
            if (!flag && !__instance.AllowImpostor)
            {
                __result = float.MaxValue;
                return false;
            }

            return true;
        }
    }
}