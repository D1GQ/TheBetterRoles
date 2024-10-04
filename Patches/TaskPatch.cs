using HarmonyLib;
using System.Text;
using Il2CppSystem.Text;
namespace TheBetterRoles;

public class TaskPatch
{
    private static float Timer = 0f;
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public class HudManager_RecomputeTaskList
    {
        public static void Postfix(HudManager __instance)
        {
            try
            {
                Il2CppSystem.Text.StringBuilder sb = new();

                if (PlayerControl.LocalPlayer.BetterData()?.RoleInfo?.RoleAssigned == true)
                {
                    System.Text.StringBuilder sb2 = new();
                    var allRoles = PlayerControl.LocalPlayer.BetterData().RoleInfo.AllRoles;
                    for (int i = allRoles.Count - 1; i >= 0; i--)
                    {
                        var role = allRoles[i];
                        sb2.Append($"<color={role.RoleColor}>{role.RoleName}</color>+++");
                    }
                    sb2 = Utils.FormatStringBuilder(sb2);
                    sb.Append($"Role: {sb2}\n");
                    sb.Append($"Team: <color={PlayerControl.LocalPlayer.GetTeamHexColor()}>{PlayerControl.LocalPlayer.GetRoleTeamName()}</color>\n");
                    sb.Append($"Objective:\n<color={PlayerControl.LocalPlayer.Role().RoleColor}>{PlayerControl.LocalPlayer.GetRoleInfo()}</color>\n\n");
                }
                sb.Append(PlayerControl.LocalPlayer.Role().HasTask == true
                    || PlayerControl.LocalPlayer.Role().HasSelfTask == true
                    ? $"Tasks" + ":\n" : $"<color={PlayerControl.LocalPlayer.Role().RoleColor}>" + "Fake Tasks" + "</color>:\n");

                float num = __instance.taskDirtyTimer;
                if (!PlayerControl.LocalPlayer)
                {
                    __instance.TaskPanel.SetTaskText(string.Empty);
                    return;
                }
                NetworkedPlayerInfo data = PlayerControl.LocalPlayer.Data;
                if (data == null)
                {
                    return;
                }
                bool flag = data.Role != null && data.Role.IsImpostor;
                if (PlayerControl.LocalPlayer.myTasks == null || PlayerControl.LocalPlayer.myTasks.Count == 0)
                {
                    sb.Append("None");
                }
                else
                {
                    for (int i = 0; i < PlayerControl.LocalPlayer.myTasks.Count; i++)
                    {
                        PlayerTask playerTask = PlayerControl.LocalPlayer.myTasks[i];
                        if (playerTask)
                        {
                            if (playerTask.TaskType == TaskTypes.FixComms && !flag)
                            {
                                playerTask.AppendTaskText(sb);
                                break;
                            }
                            playerTask.AppendTaskText(sb);
                        }
                    }
                    /*
                    if (GameOptionsManager.Instance.CurrentGameOptions.GameMode == GameModes.HideNSeek && ShipStatus.Instance.HideCountdown > 0f)
                    {
                        ShipStatus.Instance.HideCountdown -= num;
                        sb.Append("\n\n" + ((int)ShipStatus.Instance.HideCountdown).ToString());
                    }
                    */
                    sb.TrimEnd();
                }
                __instance.TaskPanel.SetTaskText(sb.ToString());
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(GameData), nameof(GameData.RecomputeTaskCounts))]
    public class GameData_RecomputeTaskCounts
    {
        public static bool Prefix(GameData __instance)
        {
            __instance.TotalTasks = 0;
            __instance.CompletedTasks = 0;
            for (var i = 0; i < __instance.AllPlayers.Count; i++)
            {
                var playerInfo = __instance.AllPlayers.ToArray()[i];
                if (!playerInfo.Disconnected && playerInfo.Tasks != null && playerInfo.Object &&
                    (GameOptionsManager.Instance.currentNormalGameOptions.GhostsDoTasks || !playerInfo.IsDead) && playerInfo.Object.BetterData()?.RoleInfo?.RoleAssigned == true
                    && playerInfo.Object.BetterData().RoleInfo.Role.HasTask == true)
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
    public class Console_CanUse
    {
        public static bool Prefix(Console __instance, [HarmonyArgument(0)] NetworkedPlayerInfo playerInfo, ref float __result, ref bool canUse, ref bool couldUse)
        {
            var pc = playerInfo.Object;
            var flag = pc.BetterData().RoleInfo.Role.HasTask || pc.BetterData().RoleInfo.Role.HasSelfTask;
            if (!flag && !__instance.AllowImpostor)
            {
                couldUse = false;
                canUse = false;
                __result = float.MaxValue;
                return false;
            }

            return true;
        }
    }
}