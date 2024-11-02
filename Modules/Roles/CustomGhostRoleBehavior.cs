using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Modules;

namespace TheBetterRoles.Roles;

public abstract class CustomGhostRoleBehavior : CustomRoleBehavior
{
    public override bool IsGhostRole => true;

    private int tempBaseOptionNum = 0;
    private int GetBaseOptionID()
    {
        var num = tempBaseOptionNum;
        tempBaseOptionNum++;
        return RoleUID + num;
    }

    protected override void SetUpSettings()
    {
        tempBaseOptionNum = 0;
        RoleOptionItem = new BetterOptionPercentItem().Create(GetBaseOptionID(), SettingsTab, Utils.GetCustomRoleNameAndColor(RoleType, true), 0f);
        AmountOptionItem = new BetterOptionIntItem().Create(GetBaseOptionID(), SettingsTab, Translator.GetString("Role.Option.Amount"), [1, 15, 1], 1, "", "", RoleOptionItem);

        OptionItems.Initialize();

        if (TaskReliantRole)
        {
            OverrideTasksOptionItem = new BetterOptionCheckboxItem().Create(GetBaseOptionID(), SettingsTab, Translator.GetString("Role.Option.OverrideTasks"), false, RoleOptionItem);
            CommonTasksOptionItem = new BetterOptionIntItem().Create(GetBaseOptionID(), SettingsTab, Translator.GetString("Role.Option.CommonTasks"), [0, 10, 1], 2, "", "", OverrideTasksOptionItem);
            LongTasksOptionItem = new BetterOptionIntItem().Create(GetBaseOptionID(), SettingsTab, Translator.GetString("Role.Option.LongTasks"), [0, 10, 1], 2, "", "", OverrideTasksOptionItem);
            ShortTasksOptionItem = new BetterOptionIntItem().Create(GetBaseOptionID(), SettingsTab, Translator.GetString("Role.Option.ShortTasks"), [0, 10, 1], 4, "", "", OverrideTasksOptionItem);
        }
    }
}
