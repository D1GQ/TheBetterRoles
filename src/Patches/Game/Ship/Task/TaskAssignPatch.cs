using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TheBetterRoles.Data;
using TheBetterRoles.Modules;
using TheBetterRoles.Network;

namespace TheBetterRoles.Patches.Game.Ship.Task;

[HarmonyPatch(typeof(ShipStatus))]
internal class AddTasksFromListPatch
{
    [HarmonyPatch(nameof(ShipStatus.AddTasksFromList))]
    [HarmonyPrefix]
    private static void AddTasksFromList_Prefix(ShipStatus __instance, [HarmonyArgument(4)] Il2CppSystem.Collections.Generic.List<NormalPlayerTask> unusedTasks)
    {
        if (!AmongUsClient.Instance.AmHost || __instance == null) return;

        List<NormalPlayerTask> disabledTasks = [];

        /*
        foreach (var task in unusedTasks)
        {
            switch (task.TaskType)
            {
                default:
                    disabledTasks.Add(task);
                    break;
            }
        }
        */

        foreach (var task in disabledTasks)
        {
            unusedTasks.Remove(task);
        }
    }
}

[HarmonyPatch(typeof(NetworkedPlayerInfo))]
internal class RpcSetTasksPatch
{
    // Patch to overwrite the task just before the process of allocating the task and sending the RPC is performed
    // Does not interfere with the vanilla task allocation process itself
    [HarmonyPatch(nameof(NetworkedPlayerInfo.RpcSetTasks))]
    [HarmonyPrefix]
    private static void RpcSetTasks_Prefix(NetworkedPlayerInfo __instance, [HarmonyArgument(0)] ref Il2CppStructArray<byte> taskTypeIds)
    {
        if (!GameState.IsHost) return;
        taskTypeIds = new Il2CppStructArray<byte>(0);

        // Default number of tasks
        int NumCommonTasks = VanillaGameSettings.CommonTasksNum.GetInt();
        int NumLongTasks = VanillaGameSettings.LongTasksNum.GetInt();
        int NumShortTasks = VanillaGameSettings.ShortTasksNum.GetInt();

        if (__instance?.ExtendedData()?.RoleInfo != null)
        {
            NumCommonTasks = __instance.ExtendedData().RoleInfo.OverrideCommonTasks >= 0 ? __instance.ExtendedData().RoleInfo.OverrideCommonTasks : NumCommonTasks;
            NumShortTasks = __instance.ExtendedData().RoleInfo.OverrideShortTasks >= 0 ? __instance.ExtendedData().RoleInfo.OverrideShortTasks : NumShortTasks;
            NumLongTasks = __instance.ExtendedData().RoleInfo.OverrideLongTasks >= 0 ? __instance.ExtendedData().RoleInfo.OverrideLongTasks : NumLongTasks;
        }

        // A list containing the IDs of tasks that can be assigned
        // Clone of the second argument of the original RpcSetTasks
        Il2CppSystem.Collections.Generic.List<byte> TasksList = new();
        foreach (var num in taskTypeIds)
            TasksList.Add(num);

        // A HashSet into which allocated tasks can be placed
        // Prevents multiple assignments of the same task
        Il2CppSystem.Collections.Generic.HashSet<TaskTypes> usedTaskTypes = new();
        int start2 = 0;
        int start3 = 0;
        int start4 = 0;

        // List of common tasks that can be assigned
        Il2CppSystem.Collections.Generic.List<NormalPlayerTask> CommonTasks = new();
        foreach (var task in ShipStatus.Instance.CommonTasks.ToArray())
            CommonTasks.Add(task);
        Shuffle(CommonTasks);

        // List of long tasks that can be assigned
        Il2CppSystem.Collections.Generic.List<NormalPlayerTask> LongTasks = new();
        foreach (var task in ShipStatus.Instance.LongTasks.ToArray())
            LongTasks.Add(task);
        Shuffle(LongTasks);

        // List of short tasks that can be assigned
        Il2CppSystem.Collections.Generic.List<NormalPlayerTask> ShortTasks = new();
        foreach (var task in ShipStatus.Instance.ShortTasks.ToArray())
            ShortTasks.Add(task);
        Shuffle(ShortTasks);

        // Use the function to assign tasks that are actually used on the Among Us side
        ShipStatus.Instance.AddTasksFromList(
            ref start2,
            NumCommonTasks,
            TasksList,
            usedTaskTypes,
            CommonTasks
        );
        ShipStatus.Instance.AddTasksFromList(
            ref start3,
            NumLongTasks,
            TasksList,
            usedTaskTypes,
            LongTasks
        );
        ShipStatus.Instance.AddTasksFromList(
            ref start4,
            NumShortTasks,
            TasksList,
            usedTaskTypes,
            ShortTasks
        );

        // Converts a list of tasks into an array (Il2CppStructArray)
        taskTypeIds = new Il2CppStructArray<byte>(TasksList.Count);
        for (int i = 0; i < TasksList.Count; i++)
        {
            taskTypeIds[i] = TasksList[i];
        }
    }
    internal static void Shuffle<T>(Il2CppSystem.Collections.Generic.List<T> list)
    {
        for (int i = 0; i < list.Count - 1; i++)
        {
            T obj = list[i];
            int rand = UnityEngine.Random.Range(i, list.Count);
            list[i] = list[rand];
            list[rand] = obj;
        }
    }
}

[HarmonyPatch(typeof(NetworkedPlayerInfo))]
internal class SetTasksPatch
{
    // Patch to overwrite the task just before the process of allocating the task and sending the RPC is performed
    // Does not interfere with the vanilla task allocation process itself
    [HarmonyPatch(nameof(NetworkedPlayerInfo.SetTasks))]
    [HarmonyPostfix]
    internal static void SetTasks_Postfix(NetworkedPlayerInfo __instance)
    {
        __instance.DirtyName();
    }
}