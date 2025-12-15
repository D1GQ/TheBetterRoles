namespace TheBetterRoles.Roles.Core.Interfaces;

internal interface IRoleDisguiseAction : IRoleAction
{
    /// <summary>
    /// Called when a player disguises, handling the logic of what happens when the player changes their appearance.
    /// </summary>
    void Disguise(PlayerControl player) { }

    /// <summary>
    /// Called when a player removes their disguise, handling the logic of what happens when they return to their original form.
    /// </summary>
    void Undisguise(PlayerControl player) { }
}
