// Code from: https://github.com/0xDrMoe/TownofHost-Enhanced

namespace TheBetterRoles;

class LateTask
{
    public string name;
    public float timer;
    public bool shouldLog;
    public Action action;
    public static List<LateTask> Tasks = [];
    public bool Run(float deltaTime)
    {
        timer -= deltaTime;
        if (timer <= 0)
        {
            action();

            if (shouldLog == true)
                Logger.Log($"{name} has finished", "LateTask");
            return true;
        }
        return false;
    }
    public LateTask(Action action, float time, string name = "No Name Task", bool shoudLog = true)
    {
        this.action = action;
        this.timer = time;
        this.name = name;
        this.shouldLog = shoudLog;
        Tasks.Add(this);
    }
    public static void Update(float deltaTime)
    {
        var TasksToRemove = new List<LateTask>();
        foreach (var task in Tasks.ToArray())
        {
            try
            {
                if (task.Run(deltaTime))
                {
                    TasksToRemove.Add(task);
                }
            }
            catch (Exception ex)
            {
                TasksToRemove.Add(task);
                Logger.Error(ex);
            }
        }
        TasksToRemove.ForEach(task => Tasks.Remove(task));
    }
}