using TheBetterRoles.Helpers;
using TheBetterRoles.Items.Enums;
using TheBetterRoles.Items.OptionItems;
using TheBetterRoles.Modules;
using TheBetterRoles.Network;

namespace TheBetterRoles.Roles.Core;

internal abstract class AddonClass : RoleClass
{
    private static readonly List<List<RoleClassTypes>> incompatibleAddons =
    [
        [RoleClassTypes.Rebound, RoleClassTypes.Onbound],
    ];

    internal override bool ShowRoleInOutro => false;
    internal override bool CanMoveInVents => true;

    internal virtual Func<RoleClass, bool> AssignmentConditionWithRole => (role) => true;

    /// <summary>
    /// Checks if the addon can be assigned based on the team.
    /// </summary>
    /// <param name="team">The team to check against.</param>
    /// <returns>True if any of the role checks pass; otherwise, false.</returns>
    internal bool CanBeAssignedWithTeam(RoleClassTeam team)
    {
        return CanBeCrewmateCheck(team) || CanBeImpostorCheck(team) || CanBeNeutralCheck(team);
    }

    private bool CanBeCrewmateCheck(RoleClassTeam team)
        => RoleOptions.AssignToCrewmate != null && RoleCategory != RoleClassCategory.EvilAddon && RoleOptions.AssignToCrewmate.GetBool() && team == RoleClassTeam.Crewmate
        || RoleOptions.AssignToCrewmate == null && team == RoleClassTeam.Crewmate && RoleCategory == RoleClassCategory.GoodAddon;

    private bool CanBeImpostorCheck(RoleClassTeam team)
        => RoleOptions.AssignToImpostor != null && RoleCategory != RoleClassCategory.GoodAddon && RoleOptions.AssignToImpostor.GetBool() && team == RoleClassTeam.Impostor
        || RoleOptions.AssignToImpostor == null && team == RoleClassTeam.Impostor && RoleCategory == RoleClassCategory.EvilAddon;

    private bool CanBeNeutralCheck(RoleClassTeam team)
        => RoleOptions.AssignToNeutral != null && RoleCategory != RoleClassCategory.GoodAddon && RoleOptions.AssignToNeutral.GetBool() && team == RoleClassTeam.Neutral
        || RoleOptions.AssignToNeutral == null && team == RoleClassTeam.Neutral && RoleCategory == RoleClassCategory.EvilAddon;

    internal bool AddonCompatibilityCheck(List<RoleClassTypes> currentAddons)
    {
        var assignment = RoleType;
        foreach (var addon in currentAddons)
        {
            foreach (var list in incompatibleAddons)
            {
                if (list.Contains(assignment) && list.Contains(addon))
                {
                    return false;
                }
            }
        }

        return true;
    }

    protected override void SetUpRole()
    {
        if (HasSetup) return;
        HasSetup = true;

        Logger.LogMethodPrivate("Setting up Role Base!", GetType());
        SetUpSettings();

        Logger.LogPrivate($"Finished setting up Role Base, now setting up Role({RoleName})!");
        OnSetUpRole();
        Logger.LogPrivate($"Finished setting up Role({RoleName})!");

        _player.DirtyName();
    }


    protected override void SetUpSettings()
    {
        ResetOptionIDs();
        RoleOptions.RoleOptionItem = OptionPercentItem.Create(GetBaseOptionID(), SettingsTab, Utils.GetCustomRoleNameAndColor(RoleType, true), 0f);
        RoleOptions.RoleOptionItem.CreateDescriptionButton(Utils.GetCustomRoleInfo(RoleType, true));
        RoleOptions.AmountOptionItem = OptionIntItem.Create(GetBaseOptionID(), SettingsTab, "Role.Option.Amount", (AmountSize, 15 / AmountSize * AmountSize, AmountSize), AmountSize, ("", ""), RoleOptions.RoleOptionItem);

        OptionItems.Initialize();

        if (RoleCategory is not RoleClassCategory.GoodAddon or RoleClassCategory.EvilAddon)
        {
            RoleOptions.AssignToCrewmate = OptionCheckboxItem.Create(GetBaseOptionID(), SettingsTab,
                Translator.GetString("Role.Option.AssignToCrewmate", [$"<{Utils.GetCustomRoleTeamColorHex(RoleClassTeam.Crewmate)}>", "</color>"]), true, RoleOptions.RoleOptionItem);
            RoleOptions.AssignToImpostor = OptionCheckboxItem.Create(GetBaseOptionID(), SettingsTab,
                Translator.GetString("Role.Option.AssignToImpostor", [$"<{Utils.GetCustomRoleTeamColorHex(RoleClassTeam.Impostor)}>", "</color>"]), true, RoleOptions.RoleOptionItem);
            RoleOptions.AssignToNeutral = OptionCheckboxItem.Create(GetBaseOptionID(), SettingsTab,
                Translator.GetString("Role.Option.AssignToNeutral", [$"<{Utils.GetCustomRoleTeamColorHex(RoleClassTeam.Neutral)}>", "</color>"]), true, RoleOptions.RoleOptionItem);
        }

        if (AdditionalVentOptions != null)
        {
            RoleOptions.VentCooldownOptionItem = OptionFloatItem.Create(GetBaseOptionID(), SettingsTab, "Role.Ability.VentCooldown", (0f, 180f, 2.5f), AdditionalVentOptions.Cooldown, ("", "s"), RoleOptions.RoleOptionItem);
            RoleOptions.VentDurationOptionItem = OptionFloatItem.Create(GetBaseOptionID(), SettingsTab, "Role.Ability.VentDuration", (0f, 180f, 2.5f), AdditionalVentOptions.Duration, ("", "s"), RoleOptions.RoleOptionItem, canBeInfinite: true);
        }
    }
}
