using TheBetterRoles.Items.OptionItems;

namespace TheBetterRoles.Roles.Core.RoleBase;

internal class RoleOptions
{
    /// <summary>
    /// The role's specific chance option in the game settings. This allows the chance of this role appearing to be configured.
    /// </summary>
    internal OptionPercentItem? RoleOptionItem { get; set; }

    /// <summary>
    /// The role's amount option in the game settings. This allows setting how many players can have this role in a game.
    /// </summary>
    internal OptionIntItem? AmountOptionItem { get; set; }

    /// <summary>
    /// The option that determines whether players can use vents while playing this role. 
    /// </summary>
    internal OptionCheckboxItem? CanVentOptionItem { get; set; }

    /// <summary>
    /// The option that sets the cooldown time between vent uses for players in this role.
    /// </summary>
    internal OptionFloatItem? VentCooldownOptionItem { get; set; }

    /// <summary>
    /// The option that sets the duration a player can stay in a vent while playing this role.
    /// </summary>
    internal OptionFloatItem? VentDurationOptionItem { get; set; }

    /// <summary>
    /// The option that determines whether players can use call an emergency meeting.
    /// </summary>
    internal OptionCheckboxItem? CanCallMeetingOptionItem { get; set; }

    /// <summary>
    /// The option that determines if the normal task amount is overrated.
    /// </summary>
    internal OptionCheckboxItem? OverrideTasksOptionItem { get; set; }

    /// <summary>
    /// The option that determines the amount of common tasks assigned to this role.
    /// </summary>
    internal OptionIntItem? CommonTasksOptionItem { get; set; }

    /// <summary>
    /// The option that determines the amount of long tasks assigned to this role when using vents.
    /// </summary>
    internal OptionIntItem? LongTasksOptionItem { get; set; }

    /// <summary>
    /// The option that determines the amount of short tasks assigned to this role.
    /// </summary>
    internal OptionIntItem? ShortTasksOptionItem { get; set; }

    /// <summary>
    /// The option that determines whether players has Impostor vision.
    /// </summary>
    internal OptionCheckboxItem? HasImpostorVisionOption { get; set; }

    // Addons
    internal OptionCheckboxItem? AssignToCrewmate { get; set; }
    internal OptionCheckboxItem? AssignToImpostor { get; set; }
    internal OptionCheckboxItem? AssignToNeutral { get; set; }
}