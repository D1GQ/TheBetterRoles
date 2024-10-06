
namespace TheBetterRoles;

public abstract class CustomAddonBehavior : CustomRoleBehavior
{
    public virtual Func<CustomRoles, bool> AssignmentCondition => (CustomRoles role) => true;
    public override bool IsAddon => true;
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
        OptionItems.Initialize();
        OnSetUpRole();
    }

    protected override void SetUpSettings()
    {
        RoleOptionItem = new BetterOptionPercentItem().Create(RoleId, SettingsTab, Utils.GetCustomRoleNameAndColor(RoleType, true), 0f);
        AmountOptionItem = new BetterOptionIntItem().Create(RoleId + 1, SettingsTab, Translator.GetString("Role.Option.Amount"), [1, 15, 1], 1, "", "", RoleOptionItem);

        if (RoleCategory is not CustomRoleCategory.GoodAddon or CustomRoleCategory.EvilAddon)
        {
            AssignToCrewmate = new BetterOptionCheckboxItem().Create(RoleId + 2, SettingsTab,
                string.Format(Translator.GetString("Role.Option.AssignToCrewmate"), $"<{Utils.GetCustomRoleTeamColor(CustomRoleTeam.Crewmate)}>", "</color>"), true, RoleOptionItem);
            AssignToImpostor = new BetterOptionCheckboxItem().Create(RoleId + 3, SettingsTab,
                string.Format(Translator.GetString("Role.Option.AssignToImpostor"), $"<{Utils.GetCustomRoleTeamColor(CustomRoleTeam.Impostor)}>", "</color>"), true, RoleOptionItem);
            AssignToNeutral = new BetterOptionCheckboxItem().Create(RoleId + 4, SettingsTab,
                string.Format(Translator.GetString("Role.Option.AssignToNeutral"), $"<{Utils.GetCustomRoleTeamColor(CustomRoleTeam.Neutral)}>", "</color>"), true, RoleOptionItem);
        }

        OptionItems.Initialize();
    }
}
