namespace TheBetterRoles.Roles.Interfaces;

internal interface IRoleSabotageAction : IRoleAction
{
    /// <summary>
    /// Called when a Sabotage is called.
    /// </summary>
    void OnSabotage(ISystemType system, SystemTypes? systemType);
}
