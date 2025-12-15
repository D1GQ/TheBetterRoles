namespace TheBetterRoles.Roles.Core.Interfaces;

internal interface IRoleTaskAction : IRoleAction
{
    /// <summary>
    /// Called when another player completes a task.
    /// </summary>
    void TaskCompleteOther(PlayerControl player, uint taskId) { }

    /// <summary>
    /// Called when the local player completes a task.
    /// </summary>
    void TaskComplete(PlayerControl player, uint taskId) { }
}
