namespace TheBetterRoles.Modules;

internal class LateTask
{
    private readonly string name;
    private float timer;
    private readonly bool shouldLog;
    private readonly Action action;
    private static readonly List<LateTask> tasks = [];

    internal LateTask(Action action, float time, string name = "No Name Task", bool shouldLog = true)
    {
        this.action = action;
        timer = time;
        this.name = name;
        this.shouldLog = shouldLog;
        tasks.Add(this);
    }

    internal bool Run(float deltaTime)
    {
        timer -= deltaTime;
        if (timer <= 0)
        {
            action();
            if (shouldLog == true)
            {
                Logger.Log($"{name} has finished", "LateTask");
            }

            return true;
        }

        return false;
    }

    internal static void Update(float deltaTime)
    {
        List<LateTask> TasksToRemove = [];

        foreach (var task in tasks)
        {
            try
            {
                if (task.Run(deltaTime))
                {
                    TasksToRemove.Add(task);
                }
            }
            catch
            {
                TasksToRemove.Add(task);
                // Logger.Error(ex);
            }
        }

        TasksToRemove.ForEach(task => tasks.Remove(task));
    }
}