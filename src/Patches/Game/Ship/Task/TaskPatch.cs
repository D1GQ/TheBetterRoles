using HarmonyLib;
using Il2CppSystem.Text;
using TheBetterRoles.Helpers;
using TheBetterRoles.Modules;
using TheBetterRoles.Network;
using UnityEngine;

namespace TheBetterRoles.Patches.Game.Ship.Task;

[HarmonyPatch(typeof(TaskPanelBehaviour))]
internal class TaskPatchRecomputeTaskList
{
    private static StringBuilder sbTasks;
    private static StringBuilder sbRoles;
    private static StringBuilder sbUnused;

    [HarmonyPatch(nameof(TaskPanelBehaviour.SetTaskText))]
    [HarmonyPrefix]
    private static bool SetTaskText_Prefix(TaskPanelBehaviour __instance)
    {
        sbTasks ??= new StringBuilder(512);
        sbRoles ??= new StringBuilder(256);
        sbUnused ??= new StringBuilder(128);

        sbTasks.Clear();
        sbTasks.Append("<size=75%>");

        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            __instance.taskText.SetText(string.Empty);
            return false;
        }

        var extendedData = player.ExtendedData();
        if (extendedData == null || extendedData.RoleInfo?.RoleAssigned != true)
        {
            return false;
        }

        var roleInfo = extendedData.RoleInfo;
        var allRoles = roleInfo.AllRoles;

        sbRoles.Clear();
        int roleCount = allRoles.Count;
        for (int i = 0; i < roleCount; i++)
        {
            var role = allRoles[i];
            sbRoles.Append(role.RoleNameAndColor + "---");
        }

        sbRoles = Utils.FormatStringBuilder(sbRoles);
        sbTasks.Append(Translator.GetString("Roles", [sbRoles.ToString()]));
        sbTasks.Append('\n');

        // Team information
        sbTasks.Append(Translator.GetString("Role.Team",
            [player.GetRoleTeamName().ToColor(player.GetTeamHexColor())]));
        sbTasks.Append('\n');

        // Role hint and addons
        sbTasks.Append($"{Translator.GetString(StringNames.RoleHint)}:\n");
        sbTasks.Append($"<color={player.Role().RoleColorHex}>");
        sbTasks.Append(player.GetRoleInfo());

        var addonsList = roleInfo.Addons;
        if (addonsList != null && addonsList.Count > 0)
        {
            sbTasks.Append('\n');
            sbTasks.Append(player.GetAddonInfo());
        }
        sbTasks.Append("</color>\n\n");

        // Tasks header
        var playerRole = player.Role();
        bool hasRealTasks = playerRole.HasTask || playerRole.HasSelfTask;
        sbTasks.Append(hasRealTasks
            ? Translator.GetString(StringNames.Tasks) + "\n"
            : $"<color={playerRole.RoleColorHex}>" + Translator.GetString(StringNames.FakeTasks) + "</color>\n");

        // Handle tasks
        var myTasks = player.myTasks;
        if (myTasks == null || myTasks.Count == 0)
        {
            sbTasks.Append(Translator.GetString(StringNames.None));
        }
        else
        {
            bool flag = player.Data?.Role()?.IsImpostor ?? false;
            int taskCount = myTasks.Count;

            for (int i = 0; i < taskCount; i++)
            {
                var playerTask = myTasks[i];
                if (playerTask == null) continue;

                if (playerTask.TaskType == TaskTypes.FixComms && !flag)
                {
                    // Special case for comms
                    sbTasks.Clear();
                    sbTasks.Append("<size=75%>");
                    playerTask.AppendTaskText(sbUnused);
                    playerTask.AppendTaskText(sbTasks);
                    break;
                }

                playerTask.AppendTaskText(sbUnused);
                playerTask.AppendTaskText(sbTasks);
            }

            sbUnused.Clear();
        }

        sbTasks.Append("</size>");
        sbTasks.TrimEnd();

        __instance.taskText.SetText(sbTasks);
        return false;
    }
}

[HarmonyPatch(typeof(ProgressTracker))]
internal class TaskPatchProgressTracker
{
    [HarmonyPatch(nameof(ProgressTracker.FixedUpdate))]
    [HarmonyPrefix]
    private static bool FixedUpdate_Prefix(ProgressTracker __instance)
    {
        if (PlayerTask.PlayerHasTaskOfType<IHudOverrideTask>(PlayerControl.LocalPlayer))
        {
            __instance.TileParent.enabled = false;
            return false;
        }
        else if (!__instance.TileParent.enabled)
        {
            __instance.TileParent.enabled = true;
        }
        GameData instance = GameData.Instance;
        if (instance && instance.TotalTasks > 0)
        {
            int num = TutorialManager.InstanceExists ? 1 :
                instance.AllPlayers.CountIl2Cpp(data => data?.Disconnected == false && data?.ExtendedData()?.RoleInfo?.Role?.HasTask == true);

            float totalProgress = 0f;
            int taskPlayers = 0;

            foreach (var player in instance.AllPlayers)
            {
                if (player?.Disconnected == false && player?.ExtendedData()?.RoleInfo?.Role?.HasTask == true)
                {
                    var tasks = player?.Tasks;
                    if (tasks != null)
                    {
                        int playerCompletedTasks = tasks.CountIl2Cpp(task => task.Complete);
                        int playerTotalTasks = tasks.Count;

                        if (playerTotalTasks > 0)
                        {
                            totalProgress += (float)playerCompletedTasks / playerTotalTasks;
                            taskPlayers++;
                        }
                    }
                }
            }
            switch (GameManager.Instance.LogicOptions.GetTaskBarMode())
            {
                case TaskBarMode.Normal:
                    break;
                case TaskBarMode.MeetingOnly:
                    if (!MeetingHud.Instance)
                    {
                        goto Skip_Update;
                    }
                    break;
                case TaskBarMode.Invisible:
                    __instance.gameObject.SetActive(false);
                    goto Skip_Update;
                default:
                    goto Skip_Update;
            }
            float num2 = taskPlayers > 0 ? totalProgress / taskPlayers : 0f;
            __instance.curValue = Mathf.Lerp(__instance.curValue, num2, Time.fixedDeltaTime * 2f);
        Skip_Update:
            __instance.TileParent.material.SetFloat("_Buckets", num);
            __instance.TileParent.material.SetFloat("_FullBuckets", __instance.curValue);
        }

        return false;
    }
}

[HarmonyPatch(typeof(GameData))]
internal class TaskPatchRecomputeTaskCounts
{
    [HarmonyPatch(nameof(GameData.RecomputeTaskCounts))]
    [HarmonyPrefix]
    private static bool RecomputeTaskCounts_Prefix(GameData __instance)
    {
        __instance.TotalTasks = 0;
        __instance.CompletedTasks = 0;

        var allPlayers = __instance.AllPlayers;
        if (allPlayers == null) return false;

        for (var i = 0; i < allPlayers.Count; i++)
        {
            var playerInfo = allPlayers[i];
            if (playerInfo == null || playerInfo.Disconnected || playerInfo.Tasks == null || playerInfo.Object == null)
                continue;

            var extendedData = playerInfo.ExtendedData();
            if (extendedData == null || extendedData.RoleInfo == null || !extendedData.RoleInfo.RoleAssigned || !extendedData.RoleInfo.Role.HasTask)
                continue;

            if (!playerInfo.IsDead)
            {
                var tasks = playerInfo.Tasks;
                if (tasks == null) continue;

                for (var j = 0; j < tasks.Count; j++)
                {
                    __instance.TotalTasks++;
                    if (tasks[j].Complete) __instance.CompletedTasks++;
                }
            }
        }

        return false;
    }
}