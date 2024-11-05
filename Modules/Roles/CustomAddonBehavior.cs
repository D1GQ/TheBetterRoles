using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Managers;
using TheBetterRoles.Modules;

namespace TheBetterRoles.Roles;

public abstract class CustomAddonBehavior : CustomRoleBehavior
{
    public override bool IsAddon => true;
    public virtual Func<CustomRoleBehavior, bool> AssignmentCondition => (CustomRoleBehavior role) => true;
    public override bool CanMoveInVents => true;

    private BetterOptionItem? AssignToCrewmate { get; set; }
    private BetterOptionItem? AssignToImpostor { get; set; }
    private BetterOptionItem? AssignToNeutral { get; set; }

    /// <summary>
    /// Checks if the addon can be assigned based on the team.
    /// </summary>
    /// <param name="team">The team to check against.</param>
    /// <returns>True if any of the role checks pass; otherwise, false.</returns>
    public bool CanBeAssignedWithTeam(CustomRoleTeam team)
    {
        return CanBeCrewmateCheck(team) || CanBeImpostorCheck(team) || CanBeNeutralCheck(team);
    }

    private bool CanBeCrewmateCheck(CustomRoleTeam team)
        => (AssignToCrewmate != null && (RoleCategory != CustomRoleCategory.EvilAddon && AssignToCrewmate.GetBool()) && team == CustomRoleTeam.Crewmate)
        || (AssignToCrewmate == null && team == CustomRoleTeam.Crewmate && RoleCategory == CustomRoleCategory.GoodAddon);

    private bool CanBeImpostorCheck(CustomRoleTeam team)
        => (AssignToImpostor != null && (RoleCategory != CustomRoleCategory.GoodAddon && AssignToImpostor.GetBool()) && team == CustomRoleTeam.Impostor)
        || (AssignToImpostor == null && team == CustomRoleTeam.Impostor && RoleCategory == CustomRoleCategory.EvilAddon);

    private bool CanBeNeutralCheck(CustomRoleTeam team)
        => (AssignToNeutral != null && (RoleCategory != CustomRoleCategory.GoodAddon && AssignToNeutral.GetBool()) && team == CustomRoleTeam.Neutral)
        || (AssignToNeutral == null && team == CustomRoleTeam.Neutral && RoleCategory == CustomRoleCategory.EvilAddon);


    protected override void SetUpRole()
    {
        if (hasSetup) return;
        hasSetup = true;

        Logger.LogMethodPrivate("Setting up Role Base!", GetType());
        SetUpSettings();

        Logger.LogPrivate($"Finished setting up Role Base, now setting up Role({RoleName})!");
        OnSetUpRole();
        Logger.LogPrivate($"Finished setting up Role({RoleName})!");

        _player.DirtyName();
    }

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

        if (RoleCategory is not CustomRoleCategory.GoodAddon or CustomRoleCategory.EvilAddon)
        {
            AssignToCrewmate = new BetterOptionCheckboxItem().Create(GetBaseOptionID(), SettingsTab,
                string.Format(Translator.GetString("Role.Option.AssignToCrewmate"), $"<{Utils.GetCustomRoleTeamColor(CustomRoleTeam.Crewmate)}>", "</color>"), true, RoleOptionItem);
            AssignToImpostor = new BetterOptionCheckboxItem().Create(GetBaseOptionID(), SettingsTab,
                string.Format(Translator.GetString("Role.Option.AssignToImpostor"), $"<{Utils.GetCustomRoleTeamColor(CustomRoleTeam.Impostor)}>", "</color>"), true, RoleOptionItem);
            AssignToNeutral = new BetterOptionCheckboxItem().Create(GetBaseOptionID(), SettingsTab,
                string.Format(Translator.GetString("Role.Option.AssignToNeutral"), $"<{Utils.GetCustomRoleTeamColor(CustomRoleTeam.Neutral)}>", "</color>"), true, RoleOptionItem);
        }
    }
}
