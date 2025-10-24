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
    private static StringBuilder sbTasks = new();
    private static System.Text.StringBuilder sbRoles = new();
    private static StringBuilder sbUnused = new();
    [HarmonyPatch(nameof(TaskPanelBehaviour.SetTaskText))]
    [HarmonyPrefix]
    private static bool SetTaskText_Prefix(TaskPanelBehaviour __instance)
    {
        sbTasks.Clear();
        sbTasks.Append("<size=75%>");

        var extendedData = PlayerControl.LocalPlayer.ExtendedData();
        if (extendedData != null)
        {
            if (extendedData?.RoleInfo?.RoleAssigned == true)
            {
                sbRoles.Clear();
                var allRoles = extendedData.RoleInfo.AllRoles;
                for (int i = 0; i < allRoles.Count; i++)
                {
                    var role = allRoles[i];
                    sbRoles.Append($"{role.RoleNameAndColor}---");
                }
                sbRoles = Utils.FormatStringBuilder(sbRoles);
                sbTasks.Append(Translator.GetString("Roles", [$"{sbRoles}\n"]));

                sbTasks.Append(Translator.GetString("Role.Team", [$"<color={PlayerControl.LocalPlayer.GetTeamHexColor()}>{PlayerControl.LocalPlayer.GetRoleTeamName()}</color>\n"]));

                string addons = "";
                if (extendedData.RoleInfo.Addons.Any()) addons = "\n" + PlayerControl.LocalPlayer.GetAddonInfo();
                sbTasks.Append($"{Translator.GetString(StringNames.RoleHint)}:\n<color={PlayerControl.LocalPlayer.Role().RoleColorHex}>{PlayerControl.LocalPlayer.GetRoleInfo()}{addons}</color>\n\n");

                sbTasks.Append(PlayerControl.LocalPlayer.Role().HasTask == true
                    || PlayerControl.LocalPlayer.Role().HasSelfTask == true
                    ? Translator.GetString(StringNames.Tasks) + "\n" : $"<color={PlayerControl.LocalPlayer.Role().RoleColorHex}>" + Translator.GetString(StringNames.FakeTasks) + "</color>\n");

                if (!PlayerControl.LocalPlayer)
                {
                    __instance.taskText.SetText(string.Empty);
                    return false;
                }
                NetworkedPlayerInfo data = PlayerControl.LocalPlayer.Data;
                if (data == null)
                {
                    return false;
                }
                bool flag = data.Role().IsImpostor;
                if (PlayerControl.LocalPlayer.myTasks == null || PlayerControl.LocalPlayer.myTasks.Count == 0)
                {
                    sbTasks.Append(Translator.GetString(StringNames.None));
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
                                sbTasks.Clear();
                                sbTasks.Append("<size=75%>");
                                playerTask.AppendTaskText(sbUnused);
                                playerTask.AppendTaskText(sbTasks);
                                break;
                            }
                            playerTask.AppendTaskText(sbUnused);
                            playerTask.AppendTaskText(sbTasks);
                        }
                    }
                    sbUnused.Clear();
                    sbTasks.Append("</size>");
                    sbTasks.TrimEnd();
                }
            }

            __instance.taskText.SetText(sbTasks);
        }

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
                instance.AllPlayers.ToArray().Count(data => data?.Disconnected == false && data?.ExtendedData()?.RoleInfo?.Role?.HasTask == true);

            float totalProgress = 0f;
            int taskPlayers = 0;

            foreach (var player in instance.AllPlayers)
            {
                if (player?.Disconnected == false && player?.ExtendedData()?.RoleInfo?.Role?.HasTask == true)
                {
                    var tasks = player?.Tasks;
                    if (tasks != null)
                    {
                        int playerCompletedTasks = tasks.ToArray().Count(task => task.Complete);
                        int playerTotalTasks = tasks.ToArray().Length;

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

        var allPlayers = __instance.AllPlayers?.ToArray();
        if (allPlayers == null) return false;

        for (var i = 0; i < allPlayers.Length; i++)
        {
            var playerInfo = allPlayers[i];
            if (playerInfo == null || playerInfo.Disconnected || playerInfo.Tasks == null || playerInfo.Object == null)
                continue;

            var extendedData = playerInfo.ExtendedData();
            if (extendedData == null || extendedData.RoleInfo == null || !extendedData.RoleInfo.RoleAssigned || !extendedData.RoleInfo.Role.HasTask)
                continue;

            if (!playerInfo.IsDead)
            {
                var tasks = playerInfo.Tasks?.ToArray();
                if (tasks == null) continue;

                for (var j = 0; j < tasks.Length; j++)
                {
                    __instance.TotalTasks++;
                    if (tasks[j].Complete) __instance.CompletedTasks++;
                }
            }
        }

        return false;
    }
}