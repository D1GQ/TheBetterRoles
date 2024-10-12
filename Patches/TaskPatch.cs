using HarmonyLib;
using UnityEngine;
namespace TheBetterRoles;

public class TaskPatch
{
    [HarmonyPatch(typeof(ProgressTracker), nameof(ProgressTracker.FixedUpdate))]
    public class ProgressTracker_FixedUpdate
    {
        public static bool Prefix(ProgressTracker __instance)
        {
            if (PlayerTask.PlayerHasTaskOfType<IHudOverrideTask>(PlayerControl.LocalPlayer))
            {
                __instance.TileParent.enabled = false;
                return false;
            }
            if (!__instance.TileParent.enabled)
            {
                __instance.TileParent.enabled = true;
            }
            GameData instance = GameData.Instance;
            if (instance && instance.TotalTasks > 0)
            {
                int num = DestroyableSingleton<TutorialManager>.InstanceExists ? 1 :
                    instance.AllPlayers.ToArray().Count(data => data?.Disconnected == false && data?.BetterData()?.RoleInfo?.Role?.HasTask == true);
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
                float num2 = (float)instance.CompletedTasks / instance.TotalTasks * num;
                __instance.curValue = Mathf.Lerp(__instance.curValue, num2, Time.fixedDeltaTime * 2f);
                Skip_Update:
                __instance.TileParent.material.SetFloat("_Buckets", num);
                __instance.TileParent.material.SetFloat("_FullBuckets", __instance.curValue);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    public class HudManager_RecomputeTaskList
    {
        public static bool Prefix(HudManager __instance)
        {
            try
            {
                if (__instance.consoleUIRoot.transform.localPosition.x != __instance.consoleUIHorizontalShift)
                {
                    Vector3 localPosition = __instance.consoleUIRoot.transform.localPosition;
                    localPosition.x = __instance.consoleUIHorizontalShift;
                    __instance.consoleUIRoot.transform.localPosition = localPosition;
                }
                if (__instance.joystickR != null && LobbyBehaviour.Instance != null)
                {
                    __instance.joystickR.ToggleVisuals(false);
                }

                __instance.taskDirtyTimer += Time.deltaTime;
                if (__instance.taskDirtyTimer > 0.25f)
                {
                    __instance.taskDirtyTimer = 0f;
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

                    // float num = __instance.taskDirtyTimer;
                    if (!PlayerControl.LocalPlayer)
                    {
                        __instance.TaskPanel.SetTaskText(string.Empty);
                        return false;
                    }
                    NetworkedPlayerInfo data = PlayerControl.LocalPlayer.Data;
                    if (data == null)
                    {
                        return false;
                    }
                    bool flag = data.BetterData().RoleInfo.Role.IsImpostor;
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
                                    sb.Clear();
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

                return false;
            }
            catch
            {
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(GameData), nameof(GameData.RecomputeTaskCounts))]
    public class GameData_RecomputeTaskCounts
    {
        public static bool Prefix(GameData __instance)
        {
            if (GameManager.Instance == null)
            {
                return false;
            }

            __instance.TotalTasks = 0;
            __instance.CompletedTasks = 0;
            for (var i = 0; i < __instance.AllPlayers.Count; i++)
            {
                var playerInfo = __instance.AllPlayers[i];
                if (!playerInfo.Disconnected && playerInfo.Tasks != null && playerInfo.Object &&
                    !playerInfo.IsDead && playerInfo.Object.BetterData().RoleInfo.Role.HasTask == true)
                    for (var j = 0; j < playerInfo.Tasks.Count; j++)
                    {
                        __instance.TotalTasks++;
                        if (playerInfo.Tasks[j].Complete) __instance.CompletedTasks++;
                    }
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(NormalPlayerTask), nameof(NormalPlayerTask.NextStep))]
    public class NormalPlayerTask_NextStep
    {
        public static void Postfix(NormalPlayerTask __instance)
        {
            if (__instance.taskStep >= __instance.MaxStep)
            {
                if (PlayerControl.LocalPlayer)
                {
                    CustomRoleManager.RoleListener(__instance.Owner, role => role.OnTaskComplete(__instance.Owner, __instance.Id));
                }
            }
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